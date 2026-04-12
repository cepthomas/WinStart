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
using System.Drawing;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;
using System.Text.Json;

//#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

//TODO options for:
//  - browser shortcuts
//  - 



namespace WinStart
{

    ///// <summary>Internal exception.</summary>
    //class WinStartException(string msg, bool isError = true) : Exception(msg)
    //{
    //    public bool IsError { get; } = isError;
    //}


    public class Utils
    {
        public static long GetId()
        {
            var id = DateTime.UtcNow.Ticks;

            //DateTime EPOCH_DT = new(1900, 1, 1, 0, 0, 0, 0);
            //var now = DateTime.Now;
            //TimeSpan ts = now - EPOCH_DT;
            //double seconds = Math.Truncate(ts.TotalSeconds);
            //double fraction = ts.Milliseconds / 1000.0 * 0xFFFFFFFF;
            //ulong raw = ((ulong)seconds << 32) + (ulong)fraction;
            //var id = now.Millisecond.ToString();

            return id;
        }
    }
}
