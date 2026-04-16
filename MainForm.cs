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
// using W32 = Ephemera.Win32.Internals;
// using WM = Ephemera.Win32.WindowManagement;


// TODO? entry.Pinned
// TODO? entry group


// programs:
// ====== All programs available in start menu => %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs  +++  subdirs
// ====== Win-X / main start context menu => %LOCALAPPDATA%\Microsoft\Windows\WinX\Group1/2/3
// ====== taskbar User Pinned => %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
// ====== user main start menu => %APPDATA%\Microsoft\Windows\Start Menu\Programs  +++  subdirs
// files:
// ====== all recent files => %APPDATA%\Microsoft\Windows\Recent
// ====== maybe %APPDATA%\Microsoft\Office\Recent

// TODO need delete entry from ui - context menu, delete key, ???  start_context_win11_2.png




// Now you can use the CommonOpenFileDialog or CommonSaveFileDialog components to display a file or folder selection dialog.
// This example uses the following code to let the user select a folder.
// using Microsoft.WindowsAPICodePack.Dialogs;
// // Let the user select a folder.
// private void btnSelect_Click(object sender, EventArgs e)
// {
//     CommonOpenFileDialog dialog = new CommonOpenFileDialog();
//     dialog.InitialDirectory = txtFolder.Text;
//     dialog.IsFolderPicker = true;
//     if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
//     {
//         txtFolder.Text = dialog.FileName;
//     }
// }

// I once hoped that Micosoft would eventually include these dialogs with Visual Studio, or at least update the
// FolderBrowserDialog to something more usable. It's been so long, however, that I think I'm going to have to let
// that dream die. It seems likely that we'll be stuck using this old saved version of the Code Pack or some other
// dialogs written by other people for the foreseeable future.



namespace WinStart
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>The jumplist. TODO probably remove</summary>
        JumpList? _jl;

        /// <summary>Filter recents.</summary>
        readonly string _filters = "bat cmd config css csv json log md txt xml";

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        Icon? _folderIcon;

        Icon? _urlIcon;
        Icon? _unknownIcon;


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

            Text = "WinStart";

            InitSelector_fake();

            // Hook selector events.
            selector.Selection += Selector_Selection;
            selector.DroppedTarget += Selector_DroppedTarget;
            selector.Trace += Selector_Trace;

            var menu = selector.ContextMenuStrip = new();
            menu.Opening += (_, _) =>
            {
                menu.Items.Clear();
                menu.Items.Add("Add Link");
                menu.Items.Add("Add File");
                menu.Items.Add("Remove");
            };
            menu.ItemClicked += Menu_ItemClicked;

            // Grab some system icons.
            _folderIcon = Utils.ExtractIcon("shell32.dll", 3, true);
            _urlIcon = Utils.ExtractIcon("shell32.dll", 13, true);
            _unknownIcon = Utils.ExtractIcon("shell32.dll", 23, true);

            selector.AddImage("SYS_folder", _folderIcon);
            selector.AddImage("SYS_url", _urlIcon);
            selector.AddImage("SYS_unknown", _unknownIcon);

            //// Process any args: *.exe id context target.
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem!.Text)
            {
                case "Add Link":
                    //FolderBrowserDialog dlg = new()
                    //{
                    //    Description = "Select the folder to add.",
                    //    ShowNewFolderButton = false
                    //    //SelectedPath = init?
                    //};

                    //if (dlg.ShowDialog() == DialogResult.OK) // UI locks here!!
                    //{
                    //    var f = dlg.SelectedPath;
                    //    Items.Insert(SelectedIndex, f);
                    //}
                    break;

                case "Add File":
                    break;

                case "Remove":
                    //int index = SelectedIndex;
                    //selector.RemoveEntry(index);
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
                _folderIcon?.Dispose();
                _urlIcon?.Dispose();
                _unknownIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Selector interaction

        ////  - symlink: `mklink /d <current_folder>\LBOT <lbot_source_folder>\LuaBagOfTricks`
        //// Creates a symbolic link located in FullName that points to the specified pathToTarget.
        //fi.CreateAsSymbolicLink(file);

        /// <summary>
        /// Init the selector from settings.
        /// </summary>
        void InitSelector()
        {
            selector.ImageSize = _settings.ImageSize;
            selector.Style = _settings.Style;
            selector.AllowExternalDrop = true;

            foreach (var entry in _settings.Entries)
            {
                AddEntry(entry.Target);


                // switch (entry.EntryType)
                // {
                //     case EntryType.File:
                //         // Process icon.
                //         var iconSpec = GetIconForFile(entry.Target);
                //         if (iconSpec is not null)
                //         {
                //             selector.AddImage(iconSpec.Value.name, iconSpec.Value.icon);
                //             var finfo = new FileInfo(entry.Target);
                //             selector.AddEntry(entry.Target, finfo.Name, iconSpec.Value.name);
                //         }
                //         else
                //         {
                //             throw new InvalidOperationException("TODO ??");
                //         }
                //         break;

                //     case EntryType.Folder:
                //         {
                //             var finfo = new FileInfo(entry.Target);
                //             selector.AddEntry(entry.Target, finfo.Name, "SYS_folder");
                //         }
                //         break;

                //     case EntryType.Link:
                //         break;

                //     case EntryType.Exe:
                //         break;

                //     default:
                //         break;
                // }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        void AddEntry(string target)
        {
            Icon? icon = null;
            string name = "?";
            string fullname = "?";

            //// Determine target type.
            //var parts = target.ToLower().Split(".");
            //string ext = parts.Length >= 2 ? parts.Last() : "";

            //string[] protocols = ["http", "https", "file"];
            //bool isUrl = protocols.Contains(protocol);
            //parts = target.ToLower().Split("://");
            //string protocol = parts.Length >= 2 ? parts[0] : "";

            //bool isFile = File.Exists(target);
            //bool isDir = Directory.Exists(target);

            string tgtlc = target.ToLower();


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
                    name = finfo.Name;
                    fullname = ft;
                    icon = Icon.ExtractAssociatedIcon(ft);

                }
                // Directory?
                else if (Directory.Exists(ft))
                {
                    DirectoryInfo dinfo = new(ft);
                    name = dinfo.Name;
                    fullname = ft;
                    icon = _folderIcon;
                }
                else
                {
                    //TODO ???
                }
            }
            // File?
            else if (File.Exists(target))
            {
                FileInfo finfo = new(target);
                name = finfo.Name;
                fullname = target;
                icon = Icon.ExtractAssociatedIcon(fullname);
            }
            // Directory?
            else if (Directory.Exists(target))
            {
                DirectoryInfo dinfo = new(target);
                name = dinfo.Name;
                fullname = target;
                icon = _folderIcon;
            }
            // URL?
            else if (tgtlc.StartsWith("http://") || tgtlc.StartsWith("https://") || tgtlc.StartsWith("file://"))
            {
                var parts = target.Split("://");
                name = parts[1];
                // could be huge...
                //http://open.juilliard.edu/courses?utm_source=hardlaunch&utm_medium=facebook&utm_campaign=online_courses&utm_term=general&utm_content=tofd
                //https://www.cnn.com/travel/article/what-to-do-houston-texas/index.htmld
                //https://www.wayfair.com/outdoor/pdp/latitude-run-2-ft-h-x-4-ft-w-plastic-privacy-screen-w004637769.html?piid=498400715&cjevent=b73e751c94a411eb808801880a1c0e14&refid=CJ687298-CJ2975314&pid=CJ4441350d
                //https://www.facebook.com/TFLDAustin/
                fullname = target;
                icon = _urlIcon;
            }

            if (icon is not null)
            {
                selector.AddImage(name, icon);
                selector.AddEntry(name, name, target);
            }
            else
            {
                //    throw new InvalidOperationException("TODO ??");
            }
        }

        /// <summary>
        /// User made a selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Selection(object? sender, Selector.SelectionEventArgs e)
        {
            Tell($"Selection -> [{e.Text}] [{e.ImageName}] [{e.Tag}]");

            // TODO click/run/open it
            //   ExecResult ExecuteCommand(List<string> args, bool cmd = false)

            //var fi_y = fi.ResolveLinkTarget(true);
            // Creates a symbolic link located in FullName that points to the specified pathToTarget.
            //fi_y.CreateAsSymbolicLink(file);

            //// Get file info.
            //var fi = new FileInfo(file);
            //var fname = fi.FullName;
            //var icon = GetIconForFile(fname);
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

            // File list?
            var targets = e.Data.GetData(DataFormats.FileDrop);
            if (targets is not null)
            {
                foreach (string target in (string[])targets)
                {
                    Tell($"Dropped file -> [{target}]");
                    AddEntry(target);
                }
                return;
            }

            var html = e.Data.GetData(DataFormats.Html);
            if (html is not null)
            {
                // Parse out the url
                var s = html as string;


                //<!--StartFragment--><A HREF="https://www.youtube.com/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1">King Crimson</A>


                //<!--StartFragment--><A HREF="https://www.google.com/search?client=firefox-b-1-d&q=o+and+m+plumbing">o and m plumbing - Google Search</A>

                // if contains "=\"http"   slice incl from "\"http" to \"   
                //="https://www.youtube.com/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1">King Crimson</A>
                //="https://www.google.com/search?client=firefox-b-1-d&q=o+and+m+plumbing">o and m plumbing - Google Search</A>


                //Version:0.9
                //StartHTML:00000147
                //EndHTML:00000322
                //StartFragment:00000181
                //EndFragment:00000286
                //SourceURL:chrome://browser/content/browser.xhtml
                //<html><body>
                //<!--StartFragment--><A HREF="https://www.youtube.com/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1">King Crimson</A>
                //<!--EndFragment-->
                //</body>
                //</html>


                //Version:0.9
                //StartHTML:00000147
                //EndHTML:00000335
                //StartFragment:00000181
                //EndFragment:00000299
                //SourceURL:chrome://browser/content/browser.xhtml
                //<html><body>
                //<!--StartFragment--><A HREF="https://www.google.com/search?client=firefox-b-1-d&q=o+and+m+plumbing">o and m plumbing - Google Search</A>
                //<!--EndFragment-->
                //</body>
                //</html>


                // spec:::
                //Version:0.9
                //StartHTML:71
                //EndHTML:170
                //StartFragment:140
                //EndFragment:160
                //StartSelection:140
                //EndSelection:160
                //<!DOCTYPE>
                //<HTML>
                //<HEAD>
                //<TITLE>The HTML Clipboard</TITLE>
                //<BASE HREF="http://sample/specs"> 
                //</HEAD>
                //<BODY>
                //<!--StartFragment -->
                //<P>The Fragment</P>
                //<!--EndFragment -->
                //</BODY>
                //</HTML>


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
        ///// <summary>
        ///// Gets the icon associated with a file, link, uri,....
        ///// </summary>
        ///// <param name="target"></param>
        ///// <returns>Icon and name or null if none</returns>
        //(Icon icon, string name)? GetIconForTarget(string target)
        //{
        //    (Icon, string)? res = null;

        //    // Default, normal file.
        //    FileInfo finfo = new(target);
        //    string name = finfo.Name;
        //    //string ext = finfo.Extension.ToLower();
        //    string fullname = finfo.FullName;

        //    // Check for dir.
        //    if (finfo.Attributes.HasFlag(FileAttributes.Directory))
        //    {



        //    }
        //    // Process if a link
        //    else if (finfo.Extension.ToLower() == ".lnk")
        //    {
        //        var sl = ShellObject.FromParsingName(finfo.FullName);
        //        var ft = ((ShellLink)sl).TargetLocation;
        //        //var fi1 = new FileInfo(ft);

        //        FileInfo finfo2 = new(ft);
        //        name = finfo2.Name;
        //        fullname = finfo2.FullName;
        //    }
        //    // else default of normal file

        //    var icon = Icon.ExtractAssociatedIcon(fullname);
        //    res = (icon, name);

        //    return res;
        //}

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
            this.InvokeIfRequired(_ => { Tell($"{e.Message}"); });
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

            string[] images = ["canard", "heart", "anguilla", "SYS_folder"];
            var rand = new Random();

            // Add entries to selector
            for (int i = 0; i < 15; i++)
            {
                //var img = images[rand.Next(0, images.Count())];
                selector.AddEntry($"<Item {i} ABCD>", images[rand.Next(0, images.Length)], $"tag{i}");
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
