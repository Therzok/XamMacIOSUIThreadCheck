using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
#if __IOS__
using View = UIKit.UIView;
#else
using View = AppKit.NSView;
#endif

namespace UIThreadCheck
{
    public class AllocatorBenchmark : NSObject
    {
        readonly int countString, countView, totalBenchCount;
        readonly System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();

        public AllocatorBenchmark(int benchCount, int stringAllocCount, int viewAllocCount)
        {
            totalBenchCount = benchCount;
            countString = stringAllocCount;
            countView = viewAllocCount;
        }

        public void Run()
        {
            for (int i = 0; i < totalBenchCount; ++i)
            {
                // Run a UI loop callback
                double delayInSeconds = i;
                PerformSelector(new Selector("runBenchmark:"), this, delayInSeconds);
            }
        }

        int benchCount = 0;
        [Export("runBenchmark:")]
        public void RunBenchmark(NSObject sender)
        {
            // Warmup, allocate a few times so we get the GC in shape
            for (int i = 0; i < 5; ++i)
            {
                RunDispose();
            }

            var beforeDispose = process.PrivateMemorySize64;
            var beforeCount = ObjCRuntimeExtensions.GetNSObjectCount();

            RunDispose();

            var afterDispose = process.PrivateMemorySize64;
            var afterCount = ObjCRuntimeExtensions.GetNSObjectCount();

            RunFinalizer();

            var finalizable = process.PrivateMemorySize64;
            var finalizableCount = ObjCRuntimeExtensions.GetNSObjectCount();

            Console.WriteLine("{0}: Statistics for bench run ({1} strings, {2} views:", benchCount++, countString, countView);
            Console.WriteLine("Private memory: {0} -> {1} -> {2}", beforeDispose, afterDispose, finalizable);
            Console.WriteLine("NSObjects: {0} -> {1} -> {2}", beforeCount, afterCount, finalizableCount);
            Console.WriteLine();

            if (benchCount >= totalBenchCount)
                MonoCounters.Dump();
        }

        void RunDispose()
        {
            using var pool = new NSAutoreleasePool();

            for (int i = 0; i < countString; ++i)
            {
                using var _ = new NSString(i.ToString());
            }

            for (int i = 0; i < countView; ++i)
            {
                using var _ = new View();
            }
        }

        void RunFinalizer()
        {
            using var pool = new NSAutoreleasePool();

            for (int i = 0; i < countString; ++i)
            {
                _ = new NSString(i.ToString());
            }

            for (int i = 0; i < countView; ++i)
            {
                _ = new View();
            }
        }
    }
}
