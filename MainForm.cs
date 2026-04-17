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
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// TODOX? entry.Pinned

// TODO Support entry groups

// TODOX entry from clipboard - file/dir name or url - uses win32 clipboard

// https://github.com/oozcitak/imagelistview

namespace WinStart
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>The jumplist. TODO remove when finished => test form</summary>
        JumpList? _jl;

        /// <summary>Filter recents.</summary>
        readonly string _filters = "bat cmd config css csv json log md txt xml";

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

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

            // Load settings first before initializing.
            string appDir = MiscUtils.GetAppDataDir("WinStart", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
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

            InitSelector_fake();

            // Hook selector events.
            selector.Selection += Selector_Selection;
            selector.DroppedTarget += Selector_DroppedTarget;
            selector.Trace += Selector_Trace;

            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Paste");
            selector.ContextMenuStrip.Items.Add("Remove");
            selector.ContextMenuStrip.ItemClicked += Menu_ItemClicked;

            // Grab some system icons. Selector takes ownership of lifetime.
            selector.AddImage(FOLDER_IMAGE, Utils.ExtractIcon("shell32.dll", 3, true)!);
            selector.AddImage(URL_IMAGE, Utils.ExtractIcon("shell32.dll", 13, true)!);
            selector.AddImage(DEFAULT_IMAGE, Utils.ExtractIcon("shell32.dll", 23, true)!);
        }

        /// <summary>
        /// User wants something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();
            int index = selector.SelectedIndexes.Count > 0 ? selector.SelectedIndexes[0] : -1;

            switch (e.ClickedItem!.Text)
            {
                case "Add File":
                case "Add Folder":
                    CommonOpenFileDialog dialog = new()
                    {
                        InitialDirectory = @"%APPDATA%\Microsoft\Windows\Start Menu\Programs",
                        IsFolderPicker = e.ClickedItem!.Text == "Add Folder"
                    };
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        AddEntry(dialog.FileName, index);
                    }
                    break;

                case "Paste":
                    AddEntry(Clipboard.GetText(), index);
                    break;

                case "Remove":
                    selector.SelectedItems.ForEach(i => 
                    {
                        _logger.Info($"Removing item [{1}]");
                        selector.RemoveEntry(i);
                    });
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Tell("OnLoad");
            base.OnLoad(e);
        }

        /// <summary>
        /// Apparently you need to create the jumplist after the window is shown.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            BuildMyJumpList();

            base.OnShown(e);
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

            // TODO Collect the current entries.



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
                //_folderIcon?.Dispose();
                //_urlIcon?.Dispose();
                //_unknownIcon?.Dispose();
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
            _settings.Entries.ForEach(e => AddEntry(e.Target));
        }

        /// <summary>
        /// Add an entry.
        /// </summary>
        /// <param name="target">Resource full name</param>
        /// <param name="index">Where to insert or append if -1</param>
        void AddEntry(string target, int index = -1)
        {
            string text;
            string fulltarget;
            string imagename;
            string tgtlc = target.ToLower();

            // Determine target type.

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

                    var icon = Icon.ExtractAssociatedIcon(ft);
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

                var icon = Icon.ExtractAssociatedIcon(fulltarget);
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
                //text = parts[1].Left(20); // could be large...
                fulltarget = target;
                imagename = URL_IMAGE;
            }
            else
            {
                _logger.Error($"Invalid target [{target}]");
                return;
            }

            selector.InsertNewEntry(index, $"", text, imagename, fulltarget);
        }

        /// <summary>
        /// User made a selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Selection(object? sender, Selector.SelectionEventArgs e)
        {
            Tell($"Selection -> [{e.Text}] [{e.ImageName}] [{e.Tag}]");

            if (e.DoubleClick)
            {
                var cmd = $"cmd /C {e.Tag}";

                ProcessStartInfo pinfo = new(cmd)
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
            else
            {
            }
        }

        /// <summary>
        /// Something external was dropped onto the control. We only care about a few.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_DroppedTarget(object? sender, Selector.DroppedTargetEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            // var formats = e.Data.GetFormats(false);
            // foreach (var sf in formats) Debug.WriteLine(sf);

            // File list?
            var targets = e.Data.GetData(DataFormats.FileDrop);
            if (targets is not null)
            {
                foreach (string target in (string[])targets)
                {
                    Tell($"Dropped file -> [{target}]");
                    AddEntry(target, e.Index);
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
                var s = html! as string;
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
                        AddEntry(url, e.Index);
                        break;
                    }
                }

                return;
            }
        }

        /// <summary>
        /// Debugging help.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        void Selector_Trace(object? sender, string s)
        {
            txtTrace.Text = s;
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
            //string s = $">{DateTime.Now:hh\\:mm\\:ss\\.fff} {s}{Environment.NewLine}";
            rtbTell.AppendText(s);
            rtbTell.AppendText(Environment.NewLine);
            rtbTell.ScrollToCaret();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "DrawColor":
                    case "SelectedColor":
                        restart = true;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            _settings.Save();
        }
        #endregion



        /////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////// debug/dev stuff  - put in test /////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Dummy data.
        /// </summary>
        void InitSelector_fake()
        {
            selector.ImageSize = _settings.ImageSize;
            selector.Style = _settings.Style;
            selector.AllowExternalDrop = true;

            // Init the image list.
            selector.AddImage("canard", new Icon(@"C:\Dev\Apps\WinStart\_Resources\canard.ico"));
            selector.AddImage("heart", new Bitmap(@"C:\Dev\Apps\WinStart\_Resources\fav32.png"));
            selector.AddImage("anguilla", new Icon(@"C:\Dev\Apps\WinStart\_Resources\anguilla.ico"));

            string[] images = ["canard", "heart", "anguilla", FOLDER_IMAGE];
            var rand = new Random();

            // Add entries to selector
            for (int i = 0; i < 15; i++)
            {
                //var img = images[rand.Next(0, images.Count())];
                selector.AddNewEntry($"name{i}", $"<Item {i} ABCD>", images[rand.Next(0, images.Length)], $"fullname{i}");
                //selector.AddEntry($"name{i}", $"<Item {i} ABCD>", i % 2 == 0 ? "canard" : "heart");

                // var lvItem = Items.Add($"name{i}", $"Item {i} ABCD", i % 2 == 0 ? "canard" : "anguilla");
                // lvItem.SubItems.Add("hi");
                // lvItem.SubItems.Add("there");
                // lvItem.Tag = $"tag{i}";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnGo_Click(object sender, EventArgs e)
        {
            selector.Dump().ForEach(s => Tell(s));
            return;


            DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
            // Key is target, value is shortcut.
            //Dictionary<FileInfo, FileInfo> finfos = [];
            //foreach (var fs in diRecent.GetFiles($"*.{f}.lnk"))
            foreach (var fs in diRecent.GetFiles())
            {
                var sl = ShellObject.FromParsingName(fs.FullName);
                Tell($"[{sl}]");

                var ft = ((ShellLink)sl).TargetLocation;
                var fi1 = new FileInfo(ft);
                Tell($"[{sl}] [{ft}]");
            }

            // Process the file path here
            var file = @"C:\Dev\Apps\WinStart\Pico.ico";
            var fi = new FileInfo(file);

            //  - symlink: `mklink /d <current_folder>\LBOT <lbot_source_folder>\LuaBagOfTricks`
            // Creates a symbolic link located in FullName that points to the specified pathToTarget.
            //fi.CreateAsSymbolicLink(file);

            if (fi.Extension.Equals(".lnk", StringComparison.CurrentCultureIgnoreCase))
            {
                // <returns>A <see cref="FileSystemInfo"/> instance if the link exists, independently if the target
                // exists or not; <see langword="null"/> if this file or directory is not a link.</returns>
                var fi_y = fi.ResolveLinkTarget(true);
            }
        }

        /// <summary>
        /// Build the actual list.
        /// </summary>
        void BuildMyJumpList()
        {
            _jl = JumpList.CreateJumpList();
            _jl.ClearAllUserTasks();
            _jl.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent; // Frequent

            ///// ---> fake pinned files.
            List<JumpListLink> pinnedItems = [];
            DirectoryInfo diPinned = new(@"C:\Dev\Misc\NLab\TestFiles");
            foreach (var fs in diPinned.GetFiles())
            {
                JumpListLink jlink = new(fs.FullName, fs.Name);
                pinnedItems.Add(jlink);
            }
            JumpListCustomCategory catPinned = new("Pinned");
            catPinned.AddJumpListItems([.. pinnedItems]);
            _jl.AddCustomCategories(catPinned);

            ///// ---> recent files.
            DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            // Key is target, value is shortcut.
            Dictionary<FileInfo, FileInfo> finfos = [];

            // Get the links.
            var filters = _filters.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
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
            List<JumpListLink> recentItems = [];
            foreach (KeyValuePair<FileInfo, FileInfo> scut in finfos.OrderBy(key => key.Key.LastAccessTime).Reverse())
            {
                JumpListLink jlink = new(scut.Value.FullName, scut.Key.Name);
                recentItems.Add(jlink);
            }

            JumpListCustomCategory catRecent = new("Recent");
            catRecent.AddJumpListItems([.. recentItems]);
            _jl.AddCustomCategories(catRecent);

            ///// ---> fake user tasks aka exes.
            var stPath = @"C:\Program Files\Sublime Text\sublime_text.exe";
            _jl.AddUserTasks(new JumpListLink(stPath, "Open ST")
            {
                IconReference = new IconReference(stPath, 0) // 0 is default icon
            });

            ///// ---> Separator.
            _jl.AddUserTasks(new JumpListSeparator());

            ///// ---> Call to myself for e.g. configuration
            var assy = Assembly.GetEntryAssembly();
            var loc = assy!.Location.Replace(".dll", ".exe");
            _jl.AddUserTasks(new JumpListLink(loc, "Configure")
            {
                IconReference = new IconReference(loc, 0),
                Arguments = "config_taskbar"
            });

            ///// ---> Separator.
            _jl.AddUserTasks(new JumpListSeparator());

            ///// ---> End of my stuff. Followed by builtin.
            _jl.Refresh();
        }
    }
}
