using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace WinStart
{
    /// <summary>
    /// 
    /// </summary>
    public class Selector : ListView
    {
        #region Properties
        /// <summary>Large image size.</summary>
        public int LargeSize
        {
            get { return LargeImageList!.ImageSize.Width; }
            set { LargeImageList!.ImageSize = new(value, value); }
        }

        /// <summary>Small image size.</summary>
        public int SmallSize
        {
            get { return SmallImageList!.ImageSize.Width; }
            set { SmallImageList!.ImageSize = new(value, value); }
        }

        /// <summary>Allow drag and drop from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;
        #endregion

        #region Fields
        /// <summary></summary>
        readonly Dictionary<string, Icon> _cache = new();
        #endregion

        #region Events
        /// <summary>User made a selection.</summary>
        public class SelectionEventArgs : EventArgs
        {
            /// <summary></summary>
            public string Name = "";
            /// <summary></summary>
            public string Text = "";
            /// <summary></summary>
            public object? Tag = null;
        }

        /// <summary></summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary></summary>
        public event EventHandler<string>? Report;
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        Icon? GetIconForFile(string fn)
        {
            Icon? icon = null;

            Icon defaultIcon = SystemIcons.Question;
            string ext = Path.GetExtension(fn);

            switch (ext, _cache.ContainsKey(ext))
            {
                case ("", _):
                case (".", _):
                case ("..", _):
                    break;

                case (_, false):
                    icon = Icon.ExtractAssociatedIcon(Path.GetFileName(fn));
                    if (icon != null)
                    {
                        _cache[ext] = icon;
                    }
                    else
                    {
                        icon = defaultIcon;
                    }
                    break;

                case (_, true):
                    icon = _cache[ext];
                    break;
            }

            return icon;
        }



        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// For some reason ListView doesn't have an OnLoad() event so do everything here.
        /// </summary>
        public Selector()
        {
            GridLines = true;
            DoubleBuffered = true;
            AllowDrop = true;
            View = View.Tile; // List  Details  LargeIcon  SmallIcon  Tile
            MultiSelect = false;
            InsertionMark.Color = Color.Red;

            OwnerDraw = true;

            Columns.Clear();
            Columns.Add("Details view is probably stupid", 200);

            ListViewItemSorter = new ListViewIndexComparer();

            LargeImageList = new();
            SmallImageList = new();

            Click += (object? sender, EventArgs e) =>
            {
                foreach (var item in SelectedItems) // should only be one...
                {
                    var lvi = (ListViewItem)item;
                    Selection?.Invoke(this, new SelectionEventArgs()
                    {
                        Name = lvi.Name,
                        Text = lvi.Text,
                        Tag = lvi.Tag
                    });
                }
            };
        }
        #endregion

        #region Public API
        /// <summary>Add a named icon to large and small images.</summary>
        /// <param name="name">Reference name</param>
        /// <param name="icon"></param>
        public void AddImage(string name, Icon icon)
        {
            LargeImageList!.Images.Add(name, icon);
            SmallImageList!.Images.Add(name, icon);
        }

        /// <summary>Add a named bitmap to large and small images.</summary>
        /// <param name="name">Reference name</param>
        /// <param name="bmp"></param>
        public void AddImage(string name, Bitmap bmp)
        {
            LargeImageList!.Images.Add(name, bmp);
            SmallImageList!.Images.Add(name, bmp);
        }

        /// <summary>
        /// Add a line item.
        /// </summary>
        /// <param name="name">Reference name</param>
        /// <param name="text">For display below image</param>
        /// <param name="image">Image name</param>
        public void AddEntry(string name, string text, string image)
        {
            Items.Add(name, text, image);
            //var lvItem = Items.Add(name, text, image);
            // lvItem.SubItems.Add("hi");
            // lvItem.SubItems.Add("there");
            // lvItem.Tag = $"tag{i}";
        }

        /// <summary>
        /// Remove item from list. Useful??
        /// </summary>
        /// <param name="name"></param>
        public void RemoveEntry(string name)
        {
            Items.RemoveByKey(name);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Custom draw the entries.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            // Use something like this:
            //e.DrawBackground();
            //if (e.Item.Selected)
            //{
            //    e.Graphics.FillRectangle(Brushes.LightYellow, e.Bounds);
            //}
            //Image img = LargeImageList.Images[e.Item.ImageKey];
            //e.Graphics.DrawImage(img, e.Bounds.Location);
            //e.Graphics.DrawString(e.Item.Text, e.Item.Font, Brushes.Black, e.Bounds.Left + img.Width + 2, e.Bounds.Top + e.Bounds.Height / 2);

            // or....
            //e.DrawBackground();
            //e.DrawText();
            e.DrawDefault = true;
            if (e.Item.Selected)
            {
                Rectangle R = e.Bounds;
                R.Inflate(-1, -1);
                using Pen pen = new(Color.Red, 1.5f);
                e.Graphics.DrawRectangle(pen, R);
            }

            base.OnDrawItem(e);
        }
        #endregion

        #region Drag and drop
        // From https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-display-an-insertion-mark-in-a-windows-forms-listview-control?view=netframeworkdesktop-4.8

        /// <summary>
        /// Starts the drag-and-drop operation when an item is dragged.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemDrag(ItemDragEventArgs e)
        {
            if (e.Item is not null)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
            base.OnItemDrag(e);
        }

        /// <summary>
        /// Sets the target drop effect.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragEnter(DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
            base.OnDragEnter(e);
        }

        /// <summary>
        /// Moves the insertion mark as the item is dragged. TODO there's a bug with drawing insertion mark in Tile view.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragOver(DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse pointer.
            Point targetPoint = PointToClient(new Point(e.X, e.Y));

            // Retrieve the index of the item closest to the mouse pointer. -1 means over drag item.
            int closestItem = InsertionMark.NearestIndex(targetPoint);
            Report?.Invoke(this, $"closestItem:{closestItem}");
            if (closestItem > -1)
            {
                // Determine whether the mouse pointer is to the left or the right of the midpoint of
                // the closest item and set the InsertionMark.AppearsAfterItem property accordingly.
                Rectangle itemBounds = GetItemRect(closestItem);
                InsertionMark.AppearsAfterItem = targetPoint.X > itemBounds.Left + (itemBounds.Width / 2);
            }

            // Set the location of the insertion mark. -1 makes the insertion mark disappear.
            InsertionMark.Index = closestItem;

            base.OnDragOver(e);
        }

        /// <summary>
        /// Removes the insertion mark when the mouse leaves the control.
        /// </summary>
        /// <param name="e"></param>d
        protected override void OnDragLeave(EventArgs e)
        {
            InsertionMark.Index = -1;
            base.OnDragLeave(e);
        }

        /// <summary>
        /// Moves the item to the location of the insertion mark.
        /// Handles drag sources of listview entries and external files.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragDrop(DragEventArgs e)
        {
            // Determine if the source is internal or external.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files is null) // internal
            {
                // Retrieve the index of the insertion mark.
                int targetIndex = InsertionMark.Index;

                // If the insertion mark is not visible, exit the method.
                if (targetIndex == -1)
                {
                    return;
                }

                // If the insertion mark is to the right of the item with the corresponding index,
                // increment the target index.
                if (InsertionMark.AppearsAfterItem)
                {
                    targetIndex++;
                }

                // Retrieve the dragged item.
                ListViewItem draggedItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));

                // Insert a copy of the dragged item at the target index.
                // A copy must be inserted before the original item is removed to preserve item index values.
                Items.Insert(targetIndex, (ListViewItem)draggedItem.Clone());

                // Remove the original copy of the dragged item.
                Items.Remove(draggedItem);
            }
            else if (AllowExternalDrop) // external
            {
                foreach (string file in files)
                {
                    // Process the file(s).
                    //Tell($"OnDragDrop [{file}]");
                    //OnDragEnter [C:\Users\cepth\Desktop\anole.jpg]
                    //OnDragEnter [C:\Users\cepth\AppData\Roaming\Microsoft\Windows\Recent\3dlink1.gif.lnk]

                    //var fi_y = fi.ResolveLinkTarget(true);
                    // Creates a symbolic link located in FullName that points to the specified pathToTarget.
                    //fi_y.CreateAsSymbolicLink(file);

                    // Get file info.
                    var fi = new FileInfo(file);
                    var fname = fi.FullName;
                    var icon = GetIconForFile(fname);

                    if (icon is not null)
                    {
                        var id = DateTime.Now.Millisecond.ToString();
                        AddImage(id, icon);
                        AddEntry($"f{id}", file.Right(12), id);
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }

                Invalidate();
            }

            base.OnDragDrop(e);
        }
        #endregion

        /// <summary>
        /// Sorts ListViewItem objects by index.
        /// </summary>
        class ListViewIndexComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                return x is null || y is null ?
                    throw new ArgumentException("You can't do that") :
                    ((ListViewItem)x).Index - ((ListViewItem)y).Index;
            }
        }
    }
}
