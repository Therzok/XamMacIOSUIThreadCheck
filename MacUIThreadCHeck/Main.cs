using AppKit;

namespace MacUIThreadCheck
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            //typeof(ObjCRuntime.Runtime)
            //    .GetField("DisposeOnlyUIObjectsOnUIThread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            //    .SetValue(null, true);

            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}
