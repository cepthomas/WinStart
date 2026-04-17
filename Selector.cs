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
using System.Diagnostics;
using Ephemera.NBagOfTricks;


namespace WinStart // Ephemera.NBagOfUis
{
    /// <summary>
    /// The control.
    /// </summary>
    public class Selector : UserControl
    {
        #region Types
        /// <summary>Supported styles.</summary>
        public enum SelectorStyle
        {
            /// <summary>Tile</summary>
            Tile,
            /// <summary>Large icon</summary>
            Icon
        }

        /// <summary>How to draw the entry.</summary>
        enum DrawStyle { Default, Fill, Box }
        #endregion

        #region Properties
        /// <summary>Image size.</summary>
        public int ImageSize
        {
            get { return _lv.LargeImageList!.ImageSize.Width; }
            set { _lv.LargeImageList!.ImageSize = new(value, value); _lv.TileSize = new(TileSize, value + 4); }
        }

        /// <summary>Select style.</summary>
        public SelectorStyle Style
        {
            get {  return _lv.View == View.Tile ? SelectorStyle.Tile : SelectorStyle.Icon; }
            set { _lv.View = value == SelectorStyle.Tile ? View.Tile : View.LargeIcon; }
        }

        /// <summary>Allow multiple item selection.</summary>
        public bool MultiSelect
        {
            get { return _lv.MultiSelect; }
            set { _lv.MultiSelect = value; }
        }

        /// <summary>Tile size -> width.</summary>
        public int TileSize { get; set; } = 160;

        /// <summary>Allow drag and drop (files) from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;

        /// <summary>Allow internal drag and drop.</summary>
        public new bool AllowDrop
        {
            get { return _lv.AllowDrop; }
            set { _lv.AllowDrop = value; base.AllowDrop = value; }
        }

        /// <summary>The view selections.</summary>
        public List<int> SelectedIndexes
        {
            get
            {
                List<int> inds = [];
                foreach (var ind in _lv.SelectedIndices) inds.Add((int)ind);
                return inds;
            }
        }

        /// <summary>The view selections.</summary>
        public List<ListViewItem> SelectedItems
        {
            get
            {
                List<ListViewItem> items = [];
                foreach (var itm in _lv.SelectedItems) items.Add((ListViewItem)itm);
                return items;
            }
        }

        /// <summary>Cosmetics.</summary>
        public Color MarkerColor { get; set; } = Color.Orange;
        #endregion

        #region Fields
        /// <summary>Contained list view.</summary>
        readonly ListView _lv = new();

        /// <summary>Text formatting.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

        /// <summary>Default image.</summary>
        Image _defaultImage;
        #endregion

        #region Events
        /// <summary>User made a selection.</summary>
        public class SelectionEventArgs : EventArgs
        {
            /// <summary>Optional name - not unique</summary>
            public string Name { get; set; } = "";

            /// <summary>Displayed text</summary>
            public string Text { get; set; } = "";

            /// <summary>Id for associated image</summary>
            public string ImageName { get; set; } = "";

            /// <summary>Optional client use</summary>
            public object? Tag { get; set; } = null;

            /// <summary>Single or double click</summary>
            public bool DoubleClick { get; set; } = false;
        }
        /// <summary></summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary>User drag-dropped something from elsewhere.</summary>
        public class DroppedTargetEventArgs : EventArgs
        {
            /// <summary>What was dragged</summary>
            public IDataObject? Data { get; set; } = null;
            
            /// <summary>Target location in list</summary>
            public int Index { get; set; } = -1;
        }
        /// <summary></summary>
        public event EventHandler<DroppedTargetEventArgs>? DroppedTarget;

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
            ///// Init the ListView defaults.
            _lv.LargeImageList = new() { ImageSize = new(32, 32) };
            _lv.SmallImageList = new() { ImageSize = new(16, 16) };
            _lv.GridLines = false;
            _lv.AllowDrop = true;
            _lv.InsertionMark.Color = MarkerColor;
            _lv.Dock = DockStyle.Fill;
            _lv.LabelWrap = true;
            _lv.LabelEdit = false;
            _lv.ListViewItemSorter = new ListViewIndexComparer();
            _lv.OwnerDraw = true;

            ///// ListView events.
            _lv.DrawItem += Lv_DrawItem;
            _lv.ItemDrag += Lv_ItemDrag;
            _lv.DragEnter += Lv_DragEnter;
            _lv.DragOver += Lv_DragOver;
            _lv.DragLeave += Lv_DragLeave;
            _lv.DragDrop += Lv_DragDrop;
            _lv.Click += Lv_Click;
            _lv.DoubleClick += Lv_DoubleClick;
            Controls.Add(_lv);

            ///// Init myself.
            AllowDrop = true;
            MultiSelect = false;
            Style = SelectorStyle.Icon;
            ImageSize = 32;

            ///// Make a default image.
            // Rainbow.
            using PixelBitmap pbmp = new(ImageSize, ImageSize);
            int incr = 256 / ImageSize;
            for (int y = 0; y < ImageSize; y++)
            {
                for (int x = 0; x < ImageSize; x++)
                {
                    pbmp.SetPixel(x, y, Color.FromArgb(255, x * incr % 256, y * incr % 256, 150));
                }
            }
            _defaultImage = (Bitmap)pbmp.ClientBitmap.Clone();

            // Big X.
            // Bitmap bmp = new(ImageSize, ImageSize);
            // using (Graphics gr = Graphics.FromImage(bmp))
            // {
            //    Pen pen = new(Color.Purple, 4);
            //    int pad = 2;
            //    int sz = ImageSize - 2*pad;
            //    gr.DrawLine(pen, pad, pad, sz, sz);
            //    gr.DrawLine(pen, pad, sz, sz, pad);
            // }
            // _defaultImage = bmp;
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _format.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Public API
        /// <summary>
        /// Add a named icon to images if not added already. Selector takes ownership of lifetime.
        /// </summary>
        /// <param name="imgName">Reference name - usually file extension</param>
        /// <param name="icon">The icon</param>
        public void AddImage(string imgName, Icon icon)
        {
            if (!_lv.LargeImageList!.Images.ContainsKey(imgName))
            {
                _lv.LargeImageList!.Images.Add(imgName, icon);
                _lv.SmallImageList!.Images.Add(imgName, icon);
            }
        }

        /// <summary>
        /// Add a named bitmap to images if not added already.
        /// </summary>
        /// <param name="imgName">Reference name - usually file extension</param>
        /// <param name="bmp"></param>
        public void AddImage(string imgName, Bitmap bmp)
        {
            if (!_lv.LargeImageList!.Images.ContainsKey(imgName))
            {
                _lv.LargeImageList!.Images.Add(imgName, bmp);
                _lv.SmallImageList!.Images.Add(imgName, bmp);
            }
        }

        /// <summary>
        /// Insert a new entry.
        /// </summary>
        /// <param name="index">Where to</param>
        /// <param name="name">Optional name</param>
        /// <param name="text">For display below/next to image</param>
        /// <param name="imgName">Image name</param>
        /// <param name="tag">Optional for client use</param>
        public void InsertNewEntry(int index, string name, string text, string imgName, object? tag = null)
        {
            ListViewItem lvi = new()
            {
                ImageKey = imgName,
                Name = name,
                Tag = tag,
            };

            if (text.Length > 20) // could be large...
            {
                lvi.ToolTipText = text;
                lvi.Text = text.Left(20);
            }
            else
            {
                lvi.Text = text;
            }

            if (index >= 0 && index < _lv.Items.Count)
            {
                _lv.Items.Insert(index, lvi);
            }
            else
            {
                _lv.Items.Add(lvi);
            }
        }

        /// <summary>
        /// Add a new entry.
        /// </summary>
        /// <param name="name">Optional name</param>
        /// <param name="text">For display below/next to image</param>
        /// <param name="imgName">Image name</param>
        /// <param name="tag">Optional for client use</param>
        public void AddNewEntry(string name, string text, string imgName, object? tag = null)
        {
            InsertNewEntry(-1, name, text, imgName, tag);
        }

        /// <summary>
        /// Remove item from list.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveEntry(int index)
        {
            if (index >= 0 && index < _lv.Items.Count)
            {
                _lv.Items.RemoveAt(index);
            }
        }

        /// <summary>
        /// Remove item from list.
        /// </summary>
        /// <param name="item"></param>
        public void RemoveEntry(ListViewItem item)
        {
            if (_lv.Items.Contains(item))
            {
                _lv.Items.Remove(item);
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Custom draw the entries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            // Indicate selected entry.
            if (e.Item.Selected)
            {
                DrawEntry(e, DrawStyle.Box, MarkerColor);
                //DrawEntry(e, DrawStyle.Fill, MarkerColor);
            }
            else
            {
                DrawEntry(e, DrawStyle.Default);
            }
        }

        /// <summary>
        /// Custom draw the entry.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="style"></param>
        /// <param name="color"></param>
        void DrawEntry(DrawListViewItemEventArgs e, DrawStyle style, Color? color = null)
        {
            //e.DrawText();
            //e.DrawDefault = true;
            e.DrawBackground();

            // Custom.
            switch (style)
            {
                case DrawStyle.Fill:
                    {
                        using var br = new SolidBrush(color ?? BackColor);
                        e.Graphics.FillRectangle(br, e.Bounds);
                    }
                    break;

                case DrawStyle.Box:
                    {
                        Rectangle rect = e.Bounds;
                        rect.Inflate(-1, -1);
                        using Pen pen = new(color ?? BackColor, 2);
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                    break;

                case DrawStyle.Default:
                    // Nothing special.
                    break;
            }

            // Main content.
            Image img = _lv.LargeImageList!.Images.ContainsKey(e.Item.ImageKey) ?
                _lv.LargeImageList!.Images[e.Item.ImageKey]! :
                _defaultImage;
            Point imgLoc = new();
            Rectangle txtRect = new();

            switch (Style)
            {
                case SelectorStyle.Tile:
                    {
                        imgLoc = new(
                            e.Bounds.Left,
                            e.Bounds.Top + (e.Bounds.Height - img.Height) / 2);
                        txtRect = new(
                            e.Bounds.Left + img.Width,
                            e.Bounds.Top,
                            e.Bounds.Width - img.Width,
                            e.Bounds.Height);
                    }
                    break;

                case SelectorStyle.Icon:
                    {
                        imgLoc = new(
                            e.Bounds.Left + (e.Bounds.Width - img.Width) / 2,
                            e.Bounds.Top);
                        txtRect = new(
                            e.Bounds.Left,
                            e.Bounds.Bottom - img.Height,
                            e.Bounds.Width,
                            e.Bounds.Height - img.Height);
                    }
                    break;
            }

            //e.Graphics.DrawRectangle(Pens.Green, txtRect);
            e.Graphics.DrawImage(img, imgLoc);
            e.Graphics.DrawString(e.Item.Text, e.Item.Font, Brushes.Black, txtRect, _format);
        }
        #endregion

        #region Standard events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doubleClick"></param>
        void NotifySelection(bool doubleClick)
        {
            foreach (var item in _lv.SelectedItems)
            {
                var lvi = (ListViewItem)item;
                Selection?.Invoke(this, new SelectionEventArgs()
                {
                    Text = lvi.Text,
                    ImageName = lvi.ImageKey,
                    Name = lvi.Name,
                    Tag = lvi.Tag,DoubleClick = doubleClick
                });
            }
        }

        /// <summary>
        /// User clicked an entry. Pass to client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Click(object? sender, EventArgs e)
        {
            NotifySelection(false);
        }

        /// <summary>
        /// User double clicked an entry. Pass to client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        void Lv_DoubleClick(object? sender, EventArgs e)
        {
            NotifySelection(true);
        }
        #endregion

        #region Drag and drop
        // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-display-an-insertion-mark-in-a-windows-forms-listview-control?view=netframeworkdesktop-4.8

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
        /// Moves the insertion mark as the item is dragged.
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
        void Lv_DragDrop(object? sender, DragEventArgs e) // also support paste filename or url
        {
            // Sanity checks.
            if (e.Data is null) return;

            // Retrieve the index of the insertion mark.
            int targetIndex = _lv.InsertionMark.Index;
            // If the insertion mark is not visible, done.
            if (targetIndex == -1) return;

            // Determine the source - internal or external.
            var formats = e.Data.GetFormats(false);

            if (formats.Contains("System.Windows.Forms.ListViewItem")) // internal
            {
                var draggedItem = (ListViewItem)e.Data.GetData("System.Windows.Forms.ListViewItem")!;

                // If the insertion mark is to the right of the item with the corresponding index, increment the target index.
                if (_lv.InsertionMark.AppearsAfterItem)
                {
                    targetIndex++;
                }
                // Trace?.Invoke(this, $"item:{draggedItem.Index}  targetIndex:{targetIndex}");

                // Insert a copy of the dragged item at the target index. To preserve item index values.
                var copy = (ListViewItem)draggedItem.Clone();
                copy.Name = (string)draggedItem.Name.Clone(); // oddly clone doesn't copy Name...
                _lv.Items.Insert(targetIndex, copy);
                // Remove the original dragged item.
                _lv.Items.Remove(draggedItem);
            }
            else if (AllowExternalDrop) // external
            {
                _lv.InsertionMark.Index = -1;
                // Hand back to the client to deal with.
                DroppedTarget?.Invoke(this, new() { Data = e.Data, Index = targetIndex });
            }
        }
        #endregion

        /// <summary>
        /// Diagnostic.
        /// </summary>
        /// <returns></returns>
        public List<string> Dump()
        {
            List<string> res = [];
            foreach (var o in _lv.Items)
            {
                var lvi = (ListViewItem)o;
                var s = $"index:[{lvi.Index}] name:[{lvi.Name}] image:[{lvi.ImageKey}] text:[{lvi.Text}] tag:[{lvi.Tag}]";
                res.Add(s);
            }
            return res;
        }

        #region Sorting
        /// <summary>
        /// Sorts ListViewItem objects by index.
        /// </summary>
        class ListViewIndexComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                return x is null || x is not ListViewItem || y is null || y is not ListViewItem ?
                    throw new ArgumentException("You can't do that") :
                    ((ListViewItem)x).Index - ((ListViewItem)y).Index;
            }
        }
        #endregion
    }
}
