using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WinStart
{
    // /// <summary>xxx</summary>
    // public enum EntryType { Empty, Exe, File, Folder, Link };

    [Serializable]
    public class Entry
    {
        // /// <summary>xxx</summary>
        // [Browsable(false)]
        // [JsonConverter(typeof(JsonStringEnumConverter))]
        // public EntryType? EntryType { get; set; } = null;

        /// <summary>file path, directory, folder, uri, etc</summary>
        [Browsable(false)]
        public string Target { get; set; } = "";

        /// <summary>xxx</summary>
        [Browsable(false)]
        public bool Pinned { get; set; } = false;

        public string? Group { get; set; } = null;
    };

    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Display Style")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Selector.SelectorStyle Style { get; set; } = Selector.SelectorStyle.Icon;
        
        [DisplayName("Image Size")]
        [Browsable(true)]
        public int ImageSize { get; set; } = 32;

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

        //[DisplayName("Selected Color")]
        //[Description("The color used for control selections.")]
        //[Browsable(true)]
        //[JsonConverter(typeof(JsonColorConverter))]
        //public Color SelectedColor { get; set; } = Color.Blue;

        //[DisplayName("Root Paths")]
        //[Description("Your favorite places.")]
        //[Browsable(true)]
        //[Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        //public List<string> RootDirs { get; set; } = [];

        //[DisplayName("Single Click Select")]
        //[Description("Generate event with single or double click.")]
        //[Browsable(true)]
        //public bool SingleClickSelect { get; set; } = false;
        #endregion

        #region Persisted Non-editable Properties
        /// <summary>Users selections.</summary>
        [Browsable(false)]
        public List<Entry> Entries { get; set; } = [];

        // [Browsable(false)]
        // public int SplitterPosition { get; set; } = 30;
        #endregion
    }
}
