using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UIKit;
using System.Drawing;

using Xamarin.SideMenu;

namespace Xamarin.SideMenu.Sample
{
    class SideMenuSample : UIViewController
    {
        UIGestureRecognizer _navBarGesture;
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var sideMenuManager = new SideMenuManager();

            View.BackgroundColor = UIColor.White;
            Title = "My Custom View Controller";

            var btn = new UIButton(UIButtonType.System);
            btn.Frame = new RectangleF(20, 200, 280, 44);
            btn.SetTitle("Test", UIControlState.Normal);

            btn.TouchUpInside += (sender, e) => {
                PresentViewController(sideMenuManager.LeftNavigationController, true, null);
            };

            View.AddSubview(btn);

            var tableVC = new UITableViewController();
            var table2VC = new UITableViewController();

            var menuLeftNavigationController = new UISideMenuNavigationController(sideMenuManager, tableVC);
            // UISideMenuNavigationController is a subclass of UINavigationController, so do any additional configuration of it here like setting its viewControllers.
            //sideMenuManager.LeftNavigationController = menuLeftNavigationController;

            //var menuRightNavigationController = new UISideMenuNavigationController(sideMenuManager, table2VC, false);
            // UISideMenuNavigationController is a subclass of UINavigationController, so do any additional configuration of it here like setting its viewControllers.
            //sideMenuManager.RightNavigationController = menuRightNavigationController;

            // Enable gestures. The left and/or right menus must be set up above for these to work.
            // Note that these continue to work on the Navigation Controller independent of the View Controller it displays!
            _navBarGesture = sideMenuManager.AddPanGestureToPresent(this.NavigationController.NavigationBar);
            sideMenuManager.AddScreenEdgePanGesturesToPresent(this.NavigationController.View, UIRectEdge.Left);
            sideMenuManager.PresentMode = SideMenuManager.MenuPresentMode.MenuSlideIn;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_navBarGesture != null)
                    this.NavigationController?.NavigationBar?.RemoveGestureRecognizer(_navBarGesture);

                //if (_navControllerGesture != null)
                //    this.NavigationController?.View.RemoveGestureRecognizer(_navControllerGesture);
            }

            base.Dispose(disposing);
        }
    }
}