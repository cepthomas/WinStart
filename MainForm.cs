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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
//using W32 = Ephemera.Win32.Internals;
//using WM = Ephemera.Win32.WindowManagement;


namespace WinStart
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>The jumplist.</summary>
        JumpList? _jl;

        /// <summary>Filter recents.</summary>
        readonly string _filters = "bat cmd config css csv json log md txt xml";
        // bat cmd c cpp h cc config cs csproj css csv cxx dot js json log lua md map neb np py settings txt xaml xml

        /// <summary>Icons cached by file extension.</summary>
        readonly Dictionary<string, Icon> _cache = new();

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;
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
            //DoubleBuffered = true;

            Text = "WinStart";

            InitSelector_fake();

            // Hook selector events.
            selector.Selection += Selector_Selection;
            selector.DroppedResource += Selector_DroppedResource;
            selector.Trace += Selector_Trace;

            //// Process the args: *.exe id context target.
            //string id = args.Length > 0 ? args[0].ToLower() : "No args!";
            //switch (args.Length, id)
            //{
            //    case (1, "xxx"):
            //        break;
            //    default:
            //        break;
            //        //throw new Exception($"Invalid command line args: [{string.Join(" ", args)}]");
            //}
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

            // TODO Collect the current icon list and save.
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

        ////  - symlink: `mklink /d <current_folder>\LBOT <lbot_source_folder>\LuaBagOfTricks`
        //// Creates a symbolic link located in FullName that points to the specified pathToTarget.
        //fi.CreateAsSymbolicLink(file);

        //if (fi.Extension.ToLower() == ".lnk")
        //{
        //    var fi_y = fi.ResolveLinkTarget(true);

        //    // <returns>A <see cref="FileSystemInfo"/> instance if the link exists, independently if the target
        //    // exists or not; <see langword="null"/> if this file or directory is not a link.</returns>

        //    if (fi_y is not null)
        //    {
        //        // Creates a symbolic link located in FullName that points to the specified pathToTarget.
        //        fi_y.CreateAsSymbolicLink(file);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="WinStartException"></exception>
        void InitSelector()
        {

            foreach (var entry in _settings.Entries)
            {
                // TODO? entry.Pinned(group?)

                switch (entry.EntryType)
                {
                    case EntryType.File:
                        // Process icon.
                        var iconSpec = GetIconForFile(entry.Resource);
                        if (iconSpec is not null)
                        {
                            selector.AddImage(iconSpec.Value.ext, iconSpec.Value.icon);
                            var finfo = new FileInfo(entry.Resource);
                            selector.AddEntry(entry.Resource, finfo.Name, iconSpec.Value.ext);
                        }
                        else
                        {
                            throw new WinStartException("TODO ??");
                        }
                        break;

                    case EntryType.Folder:
                        // Get folder icon from 


                        break;

                    case EntryType.Link:


                        break;

                    case EntryType.Exe:


                        break;

                    default:


                        break;
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Selection(object? sender, Selector.SelectionEventArgs e)
        {
            Tell($"Selection -> [{e.Name}] [{e.Text}]");

            // TODO click/run/open it

            //var fi_y = fi.ResolveLinkTarget(true);
            // Creates a symbolic link located in FullName that points to the specified pathToTarget.
            //fi_y.CreateAsSymbolicLink(file);

            //// Get file info.
            //var fi = new FileInfo(file);
            //var fname = fi.FullName;
            //var icon = GetIconForFile(fname);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_DroppedResource(object? sender, Selector.DroppedResourceEventArgs e)
        {
            // We only care about a few.

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files is not null)
            {
                foreach (string file in files)
                {
                    //DroppedResource?.Invoke(this, file);

                }
            }
            else
            {
                // other flavor
            }

            //var formats = e.Data.GetFormats(false);
            //if (formats.Contains("System.Windows.Forms.ListViewItem"))
            //var draggedItem = (ListViewItem)e.Data.GetData("System.Windows.Forms.ListViewItem");
            //var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //    foreach (string file in files)
            //    {
            //        DroppedResource?.Invoke(this, file);
            //    }

            //// Get file info.
            //var finfo = new FileInfo(res);
            //var fname = finfo.FullName;
            //var icon = GetIconForFile(fname);

            //if (icon is not null)
            //{
            //    var id = $"drop_{DateTime.UtcNow.Ticks}";
            //    AddImage(id, icon);
            //    AddEntry($"{id}", file.Right(12), id);
            //}
            //else
            //{
            //    e.Effect = DragDropEffects.None;
            //}
        }

        /// <summary>
        /// 
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
        /// Gets the icon associated with a file or link.
        /// </summary>
        /// <param name="fn"></param>
        /// <returns>Icon and name or null if none</returns>
        (Icon icon, string ext)? GetIconForFile(string fn)
        {
            (Icon, string)? res = null;

            Icon defaultIcon = SystemIcons.Question;
            string ext = Path.GetExtension(fn);

            switch (ext, _cache.ContainsKey(ext))
            {
                case ("", _):
                case (".", _):
                case ("..", _):
                    break;

                case (_, false):
                    var icon = Icon.ExtractAssociatedIcon(Path.GetFileName(fn));
                    if (icon != null)
                    {
                        _cache[ext] = icon;
                    }
                    else
                    {
                        icon = defaultIcon;
                    }
                    res = (icon, ext);
                    break;

                case (_, true):
                    res = (_cache[ext], ext);
                    break;
            }

            return res;
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

        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            // Usually come from a different thread.
            if (IsHandleCreated)
            {
                // this.InvokeIfRequired(_ => { tvInfo.Append($"{e.Message}"); });
            }
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



        /// <summary>
        /// Dummy data.
        /// </summary>
        void InitSelector_fake()
        {
            selector.ImageSize = 64;
            //selector1.LargeSize = 64;
            //selector1.SmallSize = 16;

            // Init the image list.
            selector.AddImage("canard", new Icon(@"C:\Dev\Apps\WinStart\_Resources\canard.ico"));
            selector.AddImage("heart", new Bitmap(@"C:\Dev\Apps\WinStart\_Resources\fav32.png"));
            selector.AddImage("anguilla", new Icon(@"C:\Dev\Apps\WinStart\_Resources\anguilla.ico"));
            // selector1.AddImage("anguilla", Properties.Resources.anguilla);

            // Add entries to selector
            for (int i = 0; i < 15; i++)
            {
                selector.AddEntry($"name{i}", $"<Item {i} ABCD>", i % 2 == 0 ? "canard" : "heart");
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
            return;

            // 
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

            if (fi.Extension.ToLower() == ".lnk")
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
