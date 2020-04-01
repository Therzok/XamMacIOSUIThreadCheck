using System;
using System.Collections.Generic;
using Foundation;

#if __IOS__
using View = UIKit.UIView;
#else
using View = AppKit.NSView;
#endif

namespace UIThreadCheck
{
    public static class ObjCRuntimeExtensions
    {
        static readonly object lock_obj = typeof(ObjCRuntime.Runtime)
            .GetField(nameof(lock_obj), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null);

        static readonly Dictionary<IntPtr, WeakReference> object_map = (Dictionary<IntPtr, WeakReference>)typeof(ObjCRuntime.Runtime)
            .GetField(nameof(object_map), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null);

        public static int GetNSObjectCount()
        {
            lock (lock_obj)
            {
                return object_map.Count;
            }
        }
    }
}
