using UIKit;

namespace IOSUIThreadCheck
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            typeof(ObjCRuntime.Runtime)
                .GetField("DisposeOnlyUIObjectsOnUIThread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .SetValue(null, true);

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}