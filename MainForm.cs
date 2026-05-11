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
using Microsoft.WindowsAPICodePack.Dialogs;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


// TODO recent files? pin to start?

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

        /// <summary>Folder.</summary>
        readonly Bitmap _folderImage;

        /// <summary>URL.</summary>
        readonly Bitmap _urlImage;

        /// <summary>Default if not available.</summary>
        readonly Bitmap _defaultImage;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args"></param>
        public MainForm(string[] args)
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Load settings first before initializing.
            string appDir = MiscUtils.GetAppDataDir("WinStart", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 50000);

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
            selector.TargetColor = _settings.MarkerColor;
            selector.DrawFont = _settings.TileFont;
            selector.LeftMouseClick = MouseFunction.Click;
            selector.AllowExternalDrop = true;
            // Build it.
            selector.Init(_settings.Style);

            // Hook selector events.
            selector.Selection += Selector_Selection;
            selector.DroppedTarget += Selector_DroppedTarget;

            // Selector menu.
            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Paste");
            selector.ContextMenuStrip.Items.Add("Remove");
            selector.ContextMenuStrip.ItemClicked += Menu_ItemClicked;

            // Grab some system icons. Selector takes ownership of lifetime.
            _folderImage = GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 3, true)!.ToBitmap();
            _urlImage = GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 13, true)!.ToBitmap();
            _defaultImage = GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 23, true)!.ToBitmap();

            // Init the data.
            _settings.Targets.ForEach(item => AddTarget(item));
        }

        /// <summary>
        /// User wants to do something.
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
                        AddTarget(dialog.FileName);
                    }
                    break;

                case "Paste":
                    AddTarget(Clipboard.GetText());
                    break;

                case "Remove":
                    selector.RemoveSelectedItems();
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

            _settings.Targets.Clear();
            selector.GetItems().ForEach(it => _settings.Targets.Add(new() { Name = it.Value.ToString() } ));



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
                selector?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Selector interaction
        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="target"></param>
        void AddTarget(Target target)
        {
            string text = "???";
            string targetname = target.Name;
            string targetnamelc = targetname.ToLower();
            string fulltargetname = "";
            Bitmap image = _defaultImage;

            ///// Determine target type.

            // Link?
            if (targetnamelc.EndsWith(".lnk"))
            {
                // What is it pointing to?
                var sl = ShellObject.FromParsingName(targetname);
                var ft = ((ShellLink)sl).TargetLocation;

                // File?
                if (File.Exists(ft))
                {
                    FileInfo finfo = new(ft);
                    text = finfo.Name;
                    fulltargetname = ft;

                    var icon = Icon.ExtractAssociatedIcon(ft);
                    if (icon != null)
                    {
                        image = icon.ToBitmap();
                    }
                }
                // Directory?
                else if (Directory.Exists(ft))
                {
                    DirectoryInfo dinfo = new(ft);
                    text = dinfo.Name;
                    fulltargetname = ft;
                    image = _folderImage;
                }
                else
                {
                    _logger.Error($"Invalid link [{targetname}]");
                }
            }
            // File?
            else if (File.Exists(targetname))
            {
                FileInfo finfo = new(targetname);
                text = finfo.Name;
                fulltargetname = targetname;

                var icon = Icon.ExtractAssociatedIcon(fulltargetname);
                if (icon != null)
                {
                    image = icon.ToBitmap();
                }
            }
            // Directory?
            else if (Directory.Exists(targetname))
            {
                DirectoryInfo dinfo = new(targetname);
                text = dinfo.Name;
                fulltargetname = targetname;
                image = _folderImage;
            }
            // URL?
            else if (targetnamelc.StartsWith("http://") || targetnamelc.StartsWith("https://") || targetnamelc.StartsWith("file://"))
            {
                var parts = targetname.Split("://");
                text = parts[1];
                fulltargetname = targetname;
                image = _urlImage;
            }
            // Not supported.
            else
            {
                _logger.Error($"Invalid target [{targetname}]");
            }

            if (fulltargetname != "")
            {
                selector.AddItem(text, image, fulltargetname);
            }
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="group"></param>
        void AddTarget(string name, string group = "")
        {
            AddTarget(new(){ Name = name, Group = group });
        }

        /// <summary>
        /// User made a selection. Execute it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Selection(object? sender, Ephemera.IconicSelector.SelectionEventArgs e)
        {
            //_logger.Info($"Selection -> [{e.Entry.Text}] [{e.Entry.ImageName}] [{e.Entry.Tag}]");

            foreach (var sel in e.SelectedItems)
            {
                ProcessStartInfo pinfo = new("cmd", ["/C", sel.Value.ToString()!])
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
        void Selector_DroppedTarget(object? sender, Ephemera.IconicSelector.DroppedTargetEventArgs e)
        {
            _logger.Info($"Dropped item -> [{e.NewItem}]");
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
            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "Style":
                    case "ImageSize":
                        restart = true;
                        break;
                }
            }
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            selector.TargetColor = _settings.MarkerColor;
            selector.DrawFont = _settings.TileFont;

            _settings.Save();
        }

        /// <summary>
        /// Build the list of recent items.
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
    }
}
