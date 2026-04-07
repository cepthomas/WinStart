using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Linq;
using System.Collections;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;
using System.Drawing;

//#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

//TODO options for:
//  - browser shortcuts
//  - 



namespace WinStart
{
    /// <summary>Internal exception.</summary>
    class WinStartException(string msg, bool isError) : Exception(msg)
    {
        public bool IsError { get; } = isError;
    }

    class Entry
    {
        public string Path { get; set; } = "";

        public string? Link { get; set; } = null;

        public string? Icon { get; set; } = null;

        public EntryType? EntryType { get; set; } = null;

        // Item:
        //  - list<string> groups - maybe
        //  - pinned (per group?)
    };

    class Group
    {
        // the ability to GROUP APPLICATIONS BY ACTIVITY. I had application groups for photography, for programming, for office activities, it shows how to do this. Pin apps to Start and drop them together to make folders
        public string Name { get; set; } = "";

        public List<Entry> Entries { get; set; } = [];

        public List<Entry> PinnedEntries { get; set; } = [];
    }

    public enum EntryType { Empty, Exe, PlainText, RichText, FileList, Image, Other };
}
