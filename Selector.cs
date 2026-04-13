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
using System.Text.Json.Serialization;
using System.Text.Json;
using Ephemera.NBagOfTricks;


namespace Ephemera.NBagOfUis
{

    /// <summary>
    /// 
    /// </summary>
    public class Selector : UserControl
    {
        #region Types
        /// <summary>Supported styles.</summary>
        public enum SelectorStyle { Tile, Icon }
        #endregion

        #region Properties
        /// <summary>Large image size.</summary>
        public int ImageSize
        {
            get { return _lv.LargeImageList!.ImageSize.Width; }
            set { _lv.LargeImageList!.ImageSize = new(value, value); }
        }

        ///// <summary>Small image size.</summary>
        //public int SmallSize
        //{
        //    get { return _lv.SmallImageList!.ImageSize.Width; }
        //    set { _lv.SmallImageList!.ImageSize = new(value, value); }
        //}

        public SelectorStyle Style
        {
            get {  return _lv.View == View.Tile ? SelectorStyle.Tile : SelectorStyle.Icon; }
            set { _lv.View = value == SelectorStyle.Tile ? View.Tile : View.LargeIcon; }
        }
            //= SelectorStyle.Tile;
        // _lv.View = View.Tile; // List  Details  LargeIcon  SmallIcon  Tile

        /// <summary>Allow multiple item selection.</summary>
        public bool MultiSelect
        {
            get { return _lv.MultiSelect; }
            set { _lv.MultiSelect = value; }
        }

        /// <summary>Allow drag and drop from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;
        //{
        //    get { return _allowExternalDrop; }
        //    set { _allowExternalDrop = value; _lv.AllowDrop = _allowExternalDrop; }
        //}
        //bool _allowExternalDrop = false;

        /// <summary>Allow internal drag and drop.</summary>
        public new bool AllowDrop
        {
            get { return _lv.AllowDrop; }
            set { _lv.AllowDrop = value; base.AllowDrop = value; }
        }
        #endregion

        #region Fields
        /// <summary>Contained list view.</summary>
        ListView _lv = new();

        /// <summary></summary>
        readonly bool _debug = true;
        #endregion

        #region Events
        /// <summary>User made a selection.</summary>
        public class SelectionEventArgs : EventArgs
        {
            /// <summary>As supplied to AddEntry()</summary>
            public string Name = "";
            /// <summary>As supplied to AddEntry()</summary>
            public string Text = "";
            /// <summary>Optional</summary>
            public object? Tag = null;
        }
        /// <summary></summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary>User drag-dropped something from elsewhere.</summary>
        public class DroppedResourceEventArgs : EventArgs
        {
            /// <summary>What was dragged</summary>
            public IDataObject Data;
            /// <summary>Target location</summary>
            public int Index;
        }
        /// <summary></summary>
        public event EventHandler<DroppedResourceEventArgs>? DroppedResource;

        /// <summary></summary>
        public event EventHandler<string>? Trace;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// For some reason ListView doesn't have an OnLoad() event so do everything here.
        /// </summary>
        public Selector()
        {
            ///// Init the list view.
            _lv.LargeImageList = new() { ImageSize = new(32, 32) };
            _lv.SmallImageList = new() { ImageSize = new(16, 16) };
            _lv.GridLines = false;
            _lv.AllowDrop = true;
            _lv.InsertionMark.Color = Color.Red;
            _lv.Dock = DockStyle.Fill;
            _lv.LabelWrap = true;
            _lv.LabelEdit = false;
            _lv.ListViewItemSorter = new ListViewIndexComparer();
            // TODO these??
            _lv.TileSize = new(160, 80);
            _lv.OwnerDraw = true;
            ///// ListView events.
            _lv.DrawItem += Lv_DrawItem;
            _lv.ItemDrag += Lv_ItemDrag;
            _lv.DragEnter += Lv_DragEnter;
            _lv.DragOver += Lv_DragOver;
            _lv.DragLeave += Lv_DragLeave;
            _lv.DragDrop += Lv_DragDrop;
            _lv.Click += Lv_Click;
            Controls.Add(_lv);

            ///// Init myself.
            AllowDrop = true;
            MultiSelect = false;
            Style = SelectorStyle.Icon;
        }
        #endregion

        #region Public API
        /// <summary>Add a named icon to large and small images if not added already.</summary>
        /// <param name="imgName">Reference name - usually file extension</param>
        /// <param name="icon">The icon</param>
        public void AddImage(string imgName, Icon icon)
        {
            if (!_lv.LargeImageList.Images.ContainsKey(imgName))
            {
                _lv.LargeImageList!.Images.Add(imgName, icon);
                _lv.SmallImageList!.Images.Add(imgName, icon);
            }
        }

        /// <summary>Add a named bitmap to large and small images if not added already.</summary>
        /// <param name="imgName">Reference name - usually file extension</param>
        /// <param name="bmp"></param>
        public void AddImage(string imgName, Bitmap bmp)
        {
            if (!_lv.LargeImageList.Images.ContainsKey(imgName))
            {
                _lv.LargeImageList!.Images.Add(imgName, bmp);
                _lv.SmallImageList!.Images.Add(imgName, bmp);
            }
        }

        /// <summary>
        /// Add a line item.
        /// </summary>
        /// <param name="name">Reference name aka key/id</param>
        /// <param name="text">For display below image</param>
        /// <param name="imgName">Image name</param>
        public void AddEntry(string name, string text, string imgName)
        {
            // check unique name?
            if (_lv.Items.ContainsKey(name))
            {
                throw new InvalidOperationException($"Already has item with this name [{name}]");
            }

            _lv.Items.Add(name, text, imgName);
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
            _lv.Items.RemoveByKey(name);
        }
        #endregion

        #region Standard events
        /// <summary>
        /// Custom draw the entries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DrawItem(object? sender, DrawListViewItemEventArgs e) //TODO?
        {
            //// Use something like this:
            //e.DrawBackground();
            //if (e.Item.Selected)
            //{
            //    e.Graphics.FillRectangle(Brushes.LightYellow, e.Bounds);
            //}
            //Image img = _lv.LargeImageList.Images[e.Item.ImageKey];
            //e.Graphics.DrawImage(img, e.Bounds.Location);
            //e.Graphics.DrawString(e.Item.Text, e.Item.Font, Brushes.Black, e.Bounds.Left + img.Width + 2, e.Bounds.Top + e.Bounds.Height / 2);

            //// or....
            ////e.DrawBackground();
            ////e.DrawText();
            //e.DrawDefault = true;
            //if (e.Item.Selected)
            //{
            //    Rectangle rect = e.Bounds;
            //    rect.Inflate(-1, -1);
            //    using Pen pen = new(Color.Red, 1.5f);
            //    e.Graphics.DrawRectangle(pen, rect);
            //}
        }


        /// <summary>
        /// User clicked an entry. Pass to client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Click(object? sender, EventArgs e)
        {
            foreach (var item in _lv.SelectedItems)
            {
                var lvi = (ListViewItem)item;
                Selection?.Invoke(this, new SelectionEventArgs()
                {
                    Name = lvi.Name,
                    Text = lvi.Text,
                    Tag = lvi.Tag
                });
            }
        }
        #endregion

        #region Drag and drop
        // From https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-display-an-insertion-mark-in-a-windows-forms-listview-control?view=netframeworkdesktop-4.8

        /// <summary>
        /// Starts the drag-and-drop operation when an item is dragged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is not null)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// Sets the target drop effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        /// <summary>
        /// Moves the insertion mark as the item is dragged. TODO there's a bug with drawing insertion mark in Tile view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DragOver(object? sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse pointer.
            Point targetPoint = PointToClient(new Point(e.X, e.Y));

            // Retrieve the index of the item closest to the mouse pointer. -1 means over drag item.
            int closestItem = _lv.InsertionMark.NearestIndex(targetPoint);
            Trace?.Invoke(this, $"closestItem:{closestItem}");

            if (closestItem > -1)
            {
                // Determine whether the mouse pointer is to the left or the right of the midpoint of
                // the closest item and set the InsertionMark.AppearsAfterItem property accordingly.
                Rectangle itemBounds = _lv.GetItemRect(closestItem);
                _lv.InsertionMark.AppearsAfterItem = targetPoint.X > itemBounds.Left + (itemBounds.Width / 2);
            }

            // Set the location of the insertion mark. -1 makes the insertion mark disappear.
            _lv.InsertionMark.Index = closestItem;
        }

        /// <summary>
        /// Removes the insertion mark when the mouse leaves the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>d
        void Lv_DragLeave(object? sender, EventArgs e)
        {
            _lv.InsertionMark.Index = -1;
        }

        /// <summary>
        /// Moves the item to the location of the insertion mark.
        /// Handles drag sources of listview entries and external files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DragDrop(object? sender, DragEventArgs e)
        {
            // Sanity checks.
            if (e.Data is null) return;

            // Retrieve the index of the insertion mark.
            int targetIndex = _lv.InsertionMark.Index;
            // If the insertion mark is not visible, done.
            if (targetIndex == -1) return;


            // Determine the source - internal or external.
            var formats = e.Data.GetFormats(false);
            if (formats.Contains("System.Windows.Forms.ListViewItem"))
            {
                var draggedItem = (ListViewItem)e.Data.GetData("System.Windows.Forms.ListViewItem");

                // If the insertion mark is to the right of the item with the corresponding index, increment the target index.
                if (_lv.InsertionMark.AppearsAfterItem)
                {
                    targetIndex++;
                }

                // Retrieve the dragged item.
                //ListViewItem draggedItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));

                Trace?.Invoke(this, $"item:{draggedItem.Index}  targetIndex:{targetIndex}");

// >>>>>>>>>>>>>>  Clone doesn't copy Name!!              // Insert a copy of the dragged item at the target index.
                // A copy must be inserted before the original item is removed to preserve item index values.
                var copy = (ListViewItem)draggedItem.Clone();
                _lv.Items.Insert(targetIndex, copy);

                // Remove the original dragged item.
                _lv.Items.Remove(draggedItem);
            }
            else if (AllowExternalDrop)
            {
                _lv.InsertionMark.Index = -1;
                // Hand back to the client to deal with.
                DroppedResource?.Invoke(this, new() { Data = e.Data, Index = targetIndex });
            }
        }
        #endregion

        #region Sorting
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
        #endregion
    }
}
