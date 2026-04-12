using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace WinStart
{

    /// <summary>xxx</summary>
    public enum EntryType { Empty, Exe, File, Folder, Link }; // { Empty, Exe, PlainText, RichText, FileList, Image, Other };

    [Serializable]
    public class Entry
    {
        /// <summary>xxx</summary>
        [Browsable(false)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntryType? EntryType { get; set; } = null;

        /// <summary>file, dir, uri, etc</summary>
        [Browsable(false)]
        public string Resource { get; set; } = "";

        /// <summary>xxx</summary>
        [Browsable(false)]
        public bool Pinned { get; set; } = false;

        //    public string? Group(s) { get; set; } = null;
        //    public string? Link { get; set; } = null;
        //    public string? Icon { get; set; } = null;
    };



    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Draw Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.Red;

        [DisplayName("Selected Color")]
        [Description("The color used for control selections.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Blue;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Root Paths")]
        [Description("Your favorite places.")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> RootDirs { get; set; } = [];

        [DisplayName("Single Click Select")]
        [Description("Generate event with single or double click.")]
        [Browsable(true)]
        public bool SingleClickSelect { get; set; } = false;

        [DisplayName("Default Export Folder")]
        [Description("Where to put exported files.")]
        [Browsable(true)]
        public string ExportFolder { get; set; } = "";
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        public List<Entry> Entries { get; set; } = [];

        // [Browsable(false)]
        // public int SplitterPosition { get; set; } = 30;
        #endregion
    }
}
