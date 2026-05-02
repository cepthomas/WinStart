using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
//using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// TODO Support entry groups?

// TODO recent files? pin to start?

// - Windows standard locations TODO
//   -  User Start menu => %APPDATA%\Microsoft\Windows\Start Menu\Programs and subdirs
//   -  All programs available in Start menu => %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs and subdirs
//   -  Win-X/Start context menu => %LOCALAPPDATA%\Microsoft\Windows\WinX\Group1/2/3
//   -  Taskbar pinned => %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
//   -  Recent files => %APPDATA%\Microsoft\Windows\Recent and %APPDATA%\Microsoft\Office\Recent

// https://github.com/oozcitak/imagelistview

namespace WinStart
{
    /// <summary>
    /// The application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Names.</summary>
        const string FOLDER_IMAGE = "_IMAGE_FOLDER";
        const string URL_IMAGE = "_IMAGE_URL";
        const string DEFAULT_IMAGE = "_IMAGE_DEFAULT";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args"></param>
        public MainForm(string[] args)
        {
            InitializeComponent();

x            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Load settings first before initializing.
            string appDir = MiscUtils.GetAppDataDir("WinStart", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            UpdateFromSettings();

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);

            // Main form.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;

            //WindowState = FormWindowState.Minimized;
            StartPosition = FormStartPosition.Manual;
            var pos = Cursor.Position;
            Location = new Point(200, 200);

            Text = $"WinStart {MiscUtils.GetVersionString()}";

            // Init selector properties.
            selector.ImageSize = _settings.ImageSize;
            selector.Style = _settings.Style;
            selector.MarkerColor = _settings.MarkerColor;
            selector.AllowExternalDrop = true;
            selector.MultiSelect = false;
            selector.TileSize = 150;

            // Init the data.
            foreach (var tgt in _settings.Targets)
            {
                var fn = Path.GetFileName(tgt);
                selector.AddNewEntry("", fn, fn, tgt);
            }

            // Hook selector events.
            selector.Selection += Selector_Selection;
            selector.DroppedTarget += Selector_DroppedTarget;

            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Paste");
            selector.ContextMenuStrip.Items.Add("Remove");
            selector.ContextMenuStrip.ItemClicked += Menu_ItemClicked;

            // Grab some system icons. Selector takes ownership of lifetime.
            selector.AddImage(FOLDER_IMAGE, GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 3, true)!);
            selector.AddImage(URL_IMAGE, GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 13, true)!);
            selector.AddImage(DEFAULT_IMAGE, GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 23, true)!);
        }

        /// <summary>
        /// User wants something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();
            //int index = selector.SelectedIndexes.Count > 0 ? selector.SelectedIndexes[0] : -1;

            switch (e.ClickedItem!.Text)
            {
                case "Add File":
                case "Add Folder":
                    CommonOpenFileDialog dialog = new()
                    {
                        InitialDirectory = @"%APPDATA%\Microsoft\Windows\Start Menu\Programs", // TODO from where?
                        IsFolderPicker = e.ClickedItem!.Text == "Add Folder"
                    };
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        AddEntry(dialog.FileName);//, index);
                    }
                    break;

                case "Paste":
                    AddEntry(Clipboard.GetText());//, index);
                    break;

                case "Remove":
                    selector.RemoveSelectedItems();
                    //selector.SelectedItems.ForEach(i => 
                    //{
                    //    _logger.Info($"Removing item [{1}]");
                    //    selector.RemoveEntry(i);
                    //});
                    UpdateSettingsFromSelector();
                    break;
            }
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            _settings.Save();

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Selector interaction
        /// <summary>
        /// Init the selector from settings.
        /// </summary>
        void InitSelector()
        {
            selector.ImageSize = _settings.ImageSize;
            selector.Style = _settings.Style;
            selector.AllowExternalDrop = true;
            _settings.Targets.ForEach(e => AddEntry(e));
        }

        /// <summary>
        /// Add an entry.
        /// </summary>
        /// <param name="target">Resource full name</param>
        void AddEntry(string target)
        {
            string text;
            string fulltarget;
            string imagename;
            string tgtlc = target.ToLower();

            ///// Determine target type.

            // Link?
            if (tgtlc.EndsWith(".lnk"))
            {
                // What is it pointing to?
                var sl = ShellObject.FromParsingName(target);
                var ft = ((ShellLink)sl).TargetLocation;

                // File?
                if (File.Exists(ft))
                {
                    FileInfo finfo = new(ft);
                    text = finfo.Name;
                    fulltarget = ft;

x                    var icon = Icon.ExtractAssociatedIcon(ft);
                    if (icon != null)
                    {
                        imagename = finfo.Name;
                        selector.AddImage(imagename, icon);
                    }
                    else
                    {
                        imagename = DEFAULT_IMAGE;
                    }
                }
                // Directory?
                else if (Directory.Exists(ft))
                {
                    DirectoryInfo dinfo = new(ft);
                    text = dinfo.Name;
                    fulltarget = ft;
                    imagename = FOLDER_IMAGE;
                }
                else
                {
                    _logger.Error($"Invalid link [{target}]");
                    return;
                }
            }
            // File?
            else if (File.Exists(target))
            {
                FileInfo finfo = new(target);
                text = finfo.Name;
                fulltarget = target;

x                var icon = Icon.ExtractAssociatedIcon(fulltarget);
                if (icon != null)
                {
                    imagename = finfo.Name;
                    selector.AddImage(imagename, icon);
                }
                else
                {
                    imagename = DEFAULT_IMAGE;
                }
            }
            // Directory?
            else if (Directory.Exists(target))
            {
                DirectoryInfo dinfo = new(target);
                text = dinfo.Name;
                fulltarget = target;
                imagename = FOLDER_IMAGE;
            }
            // URL?
            else if (tgtlc.StartsWith("http://") || tgtlc.StartsWith("https://") || tgtlc.StartsWith("file://"))
            {
                var parts = target.Split("://");
                text = parts[1];
                fulltarget = target;
                imagename = URL_IMAGE;
            }
            else
            {
                _logger.Error($"Invalid target [{target}]");
                return;
            }

            selector.AddNewEntry($"", text, imagename, fulltarget);
            UpdateSettingsFromSelector();
        }

        /// <summary>
        /// User made a selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Selection(object? sender, IconicSelector.SelectionEventArgs e)
        {
            _logger.Info($"Selection -> [{e.Entry.Text}] [{e.Entry.ImageName}] [{e.Entry.Tag}]");

            if (e.Entry.Tag is null) return;

            if (e.Button == MouseButtons.Left) // execute
            {
                ProcessStartInfo pinfo = new("cmd", ["/C", e.Entry.Tag.ToString()!] )
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                try
                {
                    using Process proc = new() { StartInfo = pinfo };
                    proc.Start();

                    // TIL: To avoid deadlocks, always read the output stream first and then wait.
                    var stdout = proc.StandardOutput.ReadToEnd();
                    var stderr = proc.StandardError.ReadToEnd();

                    // LogInfo("Wait for process to exit...");
                    proc.WaitForExit();
                    // proc.ExitCode, stdout, stderr

                }
                catch (Exception ex)
                {
                    _logger.Error($"Execute failed [{ex.Message}]");
                }
            }
        }

        /// <summary>
        /// Something external was dropped onto the control. We only care about a few.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_DroppedTarget(object? sender, IconicSelector.DroppedTargetEventArgs e)
        {
            if (e.Data == null) return;

            // var formats = e.Data.GetFormats(false);
            // foreach (var sf in formats) Debug.WriteLine(sf);

            // File list?
            var targets = e.Data.GetData(DataFormats.FileDrop);
            if (targets is not null)
            {
                foreach (string target in (string[])targets)
                {
                    _logger.Info($"Dropped file -> [{target}]");
                    AddEntry(target);
                }
                return;
            }

            var html = e.Data.GetData(DataFormats.Html);
            if (html is not null)
            {
                // Parse out the url.
                //Version:0.9
                //StartHTML:00000147
                //EndHTML:00000322
                //StartFragment:00000181
                //EndFragment:00000286
                //SourceURL:chrome://browser/content/browser.xhtml
                //<html><body>
                //<!--StartFragment--><A HREF="https://www.youtube.com/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1">King Crimson</A>
                //<!--StartFragment--><A HREF="https://www.google.com/search?client=firefox-b-1-d&q=o+and+m+plumbing">o and m plumbing - Google Search</A>
                //<!--EndFragment-->
                //</body>
                //</html>
                var s = html as string ?? "";
                var parts = s.SplitByToken(Environment.NewLine);
                foreach (var p in parts)
                {
                    if (p.Contains("<!--StartFragment"))
                    {
                        //<!--StartFragment--><A HREF="https://www.youtube.com/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1">King Crimson</A>
                        int start = p.IndexOf("http");
                        int end = p.IndexOf("\">", start);
                        var url = p.Substring(start, end - start);
                        Tell($"Dropped url -> [{url}]");
                        AddEntry(url);//, e.Index);
                        break;
                    }
                }

                return;
            }
        }
        #endregion

        #region Privates
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ => Tell(e.Message));
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            rtbTell.AppendText(s);
            rtbTell.AppendText(Environment.NewLine);
            rtbTell.ScrollToCaret();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;
            // foreach (var (name, cat) in changes)
            // {
            //     switch (name)
            //     {
            //         case "DrawColor":
            //         case "SelectedColor":
            //             restart = true;
            //             break;
            //     }
            // }
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            UpdateFromSettings();

            _settings.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateFromSettings()
        {
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;

            selector.Style = _settings.Style;
            selector.ImageSize = _settings.ImageSize;
            selector.MarkerColor = _settings.MarkerColor;
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateSettingsFromSelector()
        {
            _settings.Targets.Clear();
            selector.GetAllItems().ForEach(it => _settings.Targets.Add(it.Tag.ToString()));
            _settings.Save();
        }

        /// <summary>
        /// Build the actual list.
        /// </summary>
        List<string> GetRecents()
        {
            List<string> recents = [];

            List<string> filters = ["bat", "cmd", "config", "css", "csv", "json", "log", "md", "txt", "xml"];

            DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            // Key is target, value is shortcut.
            Dictionary<FileInfo, FileInfo> finfos = [];
            foreach (var f in filters)
            {
                // Get the links.
                foreach (var fs in diRecent.GetFiles($"*.{f}.lnk"))
                {
                    var sl = ShellObject.FromParsingName(fs.FullName);
                    var ft = ((ShellLink)sl).TargetLocation;
                    var fi1 = new FileInfo(ft);
                    finfos.Add(fi1, fs);
                }
            }

            // Most recent first.
            finfos.OrderBy(key => key.Key.LastAccessTime).
                Reverse().
                ForEach(fi => recents.Add(fi.Key.FullName));

            return recents;
        }

        #endregion


        ///// <summary>
        ///// Dummy data.
        ///// </summary>
        //void InitSelector_fake()
        //{
        //    selector.ImageSize = _settings.ImageSize;
        //    selector.Style = _settings.Style;
        //    selector.AllowExternalDrop = true;

        //    // Init the image list.
        //    selector.AddImage("canard", new Icon(@"C:\Dev\Apps\WinStart\_Resources\canard.ico"));
        //    selector.AddImage("heart", new Bitmap(@"C:\Dev\Apps\WinStart\_Resources\fav32.png"));
        //    selector.AddImage("anguilla", new Icon(@"C:\Dev\Apps\WinStart\_Resources\anguilla.ico"));

        //    string[] images = ["canard", "heart", "anguilla", FOLDER_IMAGE];
        //    var rand = new Random();

        //    // Add entries to selector
        //    for (int i = 0; i < 15; i++)
        //    {
        //        //var img = images[rand.Next(0, images.Count())];
        //        selector.AddNewEntry($"name{i}", $"<Item {i} ABCD>", images[rand.Next(0, images.Length)], $"fullname{i}");
        //        //selector.AddEntry($"name{i}", $"<Item {i} ABCD>", i % 2 == 0 ? "canard" : "heart");

        //        // var lvItem = Items.Add($"name{i}", $"Item {i} ABCD", i % 2 == 0 ? "canard" : "anguilla");
        //        // lvItem.SubItems.Add("hi");
        //        // lvItem.SubItems.Add("there");
        //        // lvItem.Tag = $"tag{i}";
        //    }
        //}
    }
}
