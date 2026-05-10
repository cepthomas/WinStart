using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


namespace WinStart
{
    [Serializable]
    public class Target
    {
        /// <summary>File path, directory, url.</summary>
        [Browsable(false)]
        public string Name { get; set; } = "";

        /// <summary>Group. TODO</summary>
        [Browsable(false)]
        public string Group { get; set; } = "";

        ///// <summary>Pinned, Favorite, recent, ...</summary>
        //[Browsable(false)]
        //public bool Pinned { get; set; } = false;
    };

    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Display Style")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SelectorStyle Style { get; set; } = SelectorStyle.Icon;
        
        [DisplayName("Image Size")]
        [Browsable(true)]
        public int ImageSize { get; set; } = 32;

        [DisplayName("Marker Color")]
        [Description("The color used for markers.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color MarkerColor { get; set; } = Color.Blue;

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

        [JsonConverter(typeof(JsonFontConverter))]
        public Font TileFont { get; set; } = new("Calibri", 11, FontStyle.Regular, GraphicsUnit.Point, 0);

        //[DisplayName("Root Paths")]
        //[Description("Your favorite places.")]
        //[Browsable(true)]
        //[Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        //public List<string> RootDirs { get; set; } = [];
        #endregion

        #region Persisted Non-editable Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<Target> Targets { get; set; } = [];
        #endregion
    }
}
