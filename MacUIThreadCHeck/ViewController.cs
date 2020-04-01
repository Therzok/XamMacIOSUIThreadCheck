using System;

using AppKit;
using CoreGraphics;
using Foundation;
using UIThreadCheck;

namespace MacUIThreadCheck
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var stackView = new NSStackView
            {
                Orientation = NSUserInterfaceLayoutOrientation.Vertical,
                Frame = View.Frame,
            };

            var editBenchCount = AddArrangedLabelAndField(stackView, "Benchmark count:", "5");
            var editStrings = AddArrangedLabelAndField(stackView, "NSString allocations:", "10000");
            var editViews = AddArrangedLabelAndField(stackView, "NSView allocations", "0");

            stackView.AddArrangedSubview(NSButton.CreateButton("Test", () =>
            {   
                int benchCount = int.Parse(editBenchCount.StringValue);
                int nsstringCount = int.Parse(editStrings.StringValue);
                int nsviewCount = int.Parse(editViews.StringValue);

                var bench = new AllocatorBenchmark(benchCount, nsstringCount, nsviewCount);
                bench.Run();
            }));

            View.AddSubview(stackView);

            // Do any additional setup after loading the view.
        }

        NSTextField AddArrangedLabelAndField(NSStackView toView, string label, string fieldValue)
        {
            var editField = new NSTextField { StringValue = fieldValue, };

            toView.AddArrangedSubview(NSStackView.FromViews(new NSView[] {
                new NSTextField()
                {
                    StringValue = label,
                    Editable = false,
                },
                editField
            }));

            return editField;
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
