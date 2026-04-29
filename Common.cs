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
using System.Text.Json;
using Ephemera.NBagOfTricks;


namespace WinStart
{
    /// <summary>
    /// 
    /// </summary>
    public class Utils
    {
        //public static long GetId()
        //{
        //    var id = DateTime.UtcNow.Ticks;
        //    //DateTime EPOCH_DT = new(1900, 1, 1, 0, 0, 0, 0);
        //    //var now = DateTime.Now;
        //    //TimeSpan ts = now - EPOCH_DT;
        //    //double seconds = Math.Truncate(ts.TotalSeconds);
        //    //double fraction = ts.Milliseconds / 1000.0 * 0xFFFFFFFF;
        //    //ulong raw = ((ulong)seconds << 32) + (ulong)fraction;
        //    //var id = now.Millisecond.ToString();
        //    return id;
        //}





        /// <summary>
        /// TODO put in nbui/nbot.
        /// Every icon handle (HICON) returned by ExtractIconEx must be released
        /// using the DestroyIcon function from user32.dll to prevent memory leaks.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="index"></param>
        /// <param name="largeIcon"></param>
        /// <returns></returns>
        public static Icon? ExtractIcon(string file, int index, bool largeIcon)
        {
            Icon? icon = null;

            var hres = ExtractIconEx(file, index, out nint hlarge, out nint hsmall, 1);
            if (hres != 0)
            {
                if (largeIcon && hlarge != 0)
                {
                    icon = Icon.FromHandle(hlarge);
                    if (hsmall != 0) DestroyIcon(hsmall);
                }
                else if (!largeIcon && hsmall != 0)
                {
                    icon = Icon.FromHandle(hsmall);
                    if (hlarge != 0) DestroyIcon(hlarge);
                }
            }

            return icon;
        }
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
