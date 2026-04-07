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
        #endregion

        #region Lifecycle
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public MainForm(string[] args)
        {
            InitializeComponent();

            //WindowState = FormWindowState.Minimized;
            StartPosition = FormStartPosition.Manual;
            var pos = Cursor.Position;
            Location = new Point(200, 200);
            //DoubleBuffered = true;

            Text = "XXX";

            cmbView.SelectedValueChanged += CmbView_SelectedValueChanged;
            cmbView.SelectedIndex = 0;

            InitSelector();

            AllowDrop = true;


            ///// Process the args: artificer.exe id context target.
            string id = args.Length > 0 ? args[0].ToLower() : "No args!";
            switch (args.Length, id)
            {
                case (1, "xxx"):
                    break;

                default:
                    break;
                    //throw new Exception($"Invalid command line args: [{string.Join(" ", args)}]");
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
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            Tell("OnActivated");
            Text = "ON";
            base.OnActivated(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDeactivate(EventArgs e)
        {
            Tell("OnDeactivate");
            Text = "OFF";
            base.OnDeactivate(e);
        }

        /// <summary>
        /// Apparently you need to create the jumplist after the window is shown.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            BuildMyList();

            base.OnShown(e);
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Btn_Go_Click(object sender, EventArgs e)
        {
            //// C:\Users\cepth\AppData\Roaming\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar

            //DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
            //// Key is target, value is shortcut.
            ////Dictionary<FileInfo, FileInfo> finfos = [];
            ////foreach (var fs in diRecent.GetFiles($"*.{f}.lnk"))
            //foreach (var fs in diRecent.GetFiles())
            //{
            //    var sl = ShellObject.FromParsingName(fs.FullName);
            //    Tell($"[{sl}]");

            //    //var ft = ((ShellLink)sl).TargetLocation;
            //    //var fi1 = new FileInfo(ft);
            //    //Tell($"[{sl}] [{ft}]");
            //}


            // Process the file path here

            //var file = @"C:\Users\cepth\Desktop\anole.jpg";
            //var file = @"C:\Users\cepth\AppData\Roaming\Microsoft\Windows\Recent\108-0875_IMG.JPG.lnk";
            var file = @"C:\Dev\Apps\WinStart\Pico.ico";

            var fi = new FileInfo(file);

            //  - symlink: `mklink /d <current_folder>\LBOT <lbot_source_folder>\LuaBagOfTricks`


            // Creates a symbolic link located in FullName that points to the specified pathToTarget.
            fi.CreateAsSymbolicLink(file);

            if (fi.Extension.ToLower() == ".lnk")
            {
                var fi_y = fi.ResolveLinkTarget(true);

                // <returns>A <see cref="FileSystemInfo"/> instance if the link exists, independently if the target
                // exists or not; <see langword="null"/> if this file or directory is not a link.</returns>

                if (fi_y is not null)
                {
                    // Creates a symbolic link located in FullName that points to the specified pathToTarget.
                    fi_y.CreateAsSymbolicLink(file);
                }
            }


            // Icons
            var icon = Icon.ExtractAssociatedIcon(file);

            var id = DateTime.Now.Millisecond.ToString();

            //selector1.AddImage(id, icon);
            //selector1.AddEntry($"f{id}", file.Right(12), id);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                // Process the file path here
                Tell($"OnDragEnter [{file}]");
                //OnDragEnter [C:\Users\cepth\Desktop\anole.jpg]
                //OnDragEnter [C:\Users\cepth\AppData\Roaming\Microsoft\Windows\Recent\3dlink1.gif.lnk]

                // Get file info.
                //var fs = new FileSystemInfo(file);

                continue;
                var fi = new FileInfo(file);

                var fi_y = fi.ResolveLinkTarget(true);
                // <returns>A <see cref="FileSystemInfo"/> instance if the link exists, independently if the target
                // exists or not; <see langword="null"/> if this file or directory is not a link.</returns>


                // Creates a symbolic link located in FullName that points to the specified pathToTarget.
                fi_y.CreateAsSymbolicLink(file);



                var fi_x = fi.ResolveLinkTarget(true);
                fi_x.CreateAsSymbolicLink(file);

                var name = fi_x.Name;


                // Icons
                var icon = Icon.ExtractAssociatedIcon(file);

                var id = DateTime.Now.Millisecond.ToString();

                selector1.AddImage(id, icon);
                selector1.AddEntry($"f{id}", file.Right(12), id);

                //selector1.Invalidate();

                //Set the Effect: Assign a value from the DragDropEffects Enumeration to e.Effect.
                //If the data is valid, set it to Copy, Move, or Link. If invalid, set it to None.
                e.Effect = DragDropEffects.None;
            }

            base.OnDragEnter(e);
        }

        //protected override void OnDragOver(DragEventArgs e)
        //{
        //    Tell($"OnDragOver []");
        //    base.OnDragOver(e);
        //}

        protected override void OnDragLeave(EventArgs e)
        {
            Tell($"OnDragLeave []");
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            Tell($"OnDragDrop []");
            base.OnDragDrop(e);
        }

        /// <summary>
        /// 
        /// </summary>
        void InitSelector()
        {
            selector1.LargeSize = 64;
            selector1.SmallSize = 16;

            // Init the image list.
            selector1.AddImage("canard", new Icon(@"C:\Dev\Apps\Artificer\_Resources\canard.ico"));
            selector1.AddImage("heart", new Bitmap(@"C:\Dev\Apps\Artificer\_Resources\fav32.png"));
            selector1.AddImage("anguilla", new Icon(@"C:\Dev\Apps\Artificer\_Resources\anguilla.ico"));
            // selector1.AddImage("anguilla", Properties.Resources.anguilla);

            // Add entries to selector
            for (int i = 0; i < 5; i++)
            {
                selector1.AddEntry($"name{i}", $"<Item {i} ABCD>", i % 2 == 0 ? "canard" : "anguilla");
                // var lvItem = Items.Add($"name{i}", $"Item {i} ABCD", i % 2 == 0 ? "canard" : "anguilla");
                // lvItem.SubItems.Add("hi");
                // lvItem.SubItems.Add("there");
                // lvItem.Tag = $"tag{i}";
            }

            // Hook events.
            selector1.Selection += (object? sender, Selector.SelectionEventArgs e) =>
            {
                Tell($"Click -> [{e.Name}] [{e.Text}]");
            };

            selector1.View = View.LargeIcon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CmbView_SelectedValueChanged(object? sender, EventArgs e)
        {
            if (selector1.CheckBoxes)
            {
                Tell("Microsoft says that Tile view can't have checkboxes, so CheckBoxes have been turned off on this list.");
                selector1.CheckBoxes = false;
            }

            if (selector1.VirtualMode)
            {
                Tell("Sorry, Microsoft says that virtual lists can't use Tile view.");
                return;
            }

            // Tile  List  Details  LargeIcon SmallIcon
            switch (cmbView.SelectedItem)
            {
                case "Tile": selector1.View = View.Tile; break;
                case "List": selector1.View = View.List; break;
                case "Details": selector1.View = View.Details; break;
                case "LargeIcon": selector1.View = View.LargeIcon; break;
                case "SmallIcon": selector1.View = View.SmallIcon; break;
            }
        }

        /// <summary>
        /// Build the actual list. TODO from spec
        /// </summary>
        void BuildMyList()
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
    }
}
