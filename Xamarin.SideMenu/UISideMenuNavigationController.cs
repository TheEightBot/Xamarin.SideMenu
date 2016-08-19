using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Foundation;
using UIKit;
using CoreAnimation;

namespace EightBot.BigBang.iOS.SideMenu
{
    public class UISideMenuNavigationController : UINavigationController
    {
        public UISideMenuNavigationController(UIViewController rootViewController) : base (rootViewController)
        {

        }

        public UIColor OriginalMenuBackgroundColor { get; set; }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            // if this isn't set here, segues cause viewWillAppear and viewDidAppear to be called twice
            // likely because the transition completes and the presentingViewController is added back
            // into view for the default transition style.
            this.ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen;
        }

        /// Whether the menu appears on the right or left side of the screen. Right is the default.
        private bool leftSide = false;
        public bool LeftSide
        {
            get { return leftSide; }
            set
            {
                if (this.IsViewLoaded && value != leftSide)
                {
                    DidSetSide();
                }
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            DidSetSide();
        }

        private void DidSetSide()
        {
            if (leftSide)
            {
                SideMenuManager.menuLeftNavigationController = this;
            }
            else
            {
                SideMenuManager.menuRightNavigationController = this;
            }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            // we had presented a view before, so lets dismiss ourselves as already acted upon
            if (this.View.Hidden)
            {
                SideMenuTransition.Current.hideMenuComplete();
                DismissViewController(false, () => this.View.Hidden = false);
            }
        }


        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            // when presenting a view controller from the menu, the menu view gets moved into another transition view above our transition container
            // which can break the visual layout we had before. So, we move the menu view back to its original transition view to preserve it.
            if (!IsBeingDismissed)
            {
                var mainView = PresentingViewController?.View;
                if (mainView != null)
                {
                    switch (SideMenuManager.menuPresentMode)
                    {
                        case SideMenuManager.MenuPresentMode.ViewSlideOut:
                        case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                            mainView.Superview?.InsertSubviewBelow(View, mainView);
                            break;
                        case SideMenuManager.MenuPresentMode.MenuSlideIn:
                        case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                            mainView.Superview?.InsertSubviewAbove(View, SideMenuTransition.tapView);
                            break;
                    }
                }
            }
        }

        public override void ViewDidDisappear(bool animated)
        {

            // we're presenting a view controller from the menu, so we need to hide the menu so it isn't  g when the presented view is dismissed.
            if (!IsBeingDismissed)
            {
                View.Hidden = true;
                SideMenuTransition.Current.hideMenuStart();
            }
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);

            // don't bother resizing if the view isn't visible
            if (View.Hidden)
            {
                return;
            }

            if (SideMenuTransition.statusBarView != null)
            {
                SideMenuTransition.statusBarView.Hidden = true;
                coordinator.AnimateAlongsideTransition(
                    (_) => SideMenuTransition.Current.presentMenuStart(toSize),
                    (_) => SideMenuTransition.statusBarView.Hidden = false);
            }
        }

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            var menuViewController = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController : SideMenuManager.menuRightNavigationController;
            if (menuViewController != null)
            {
                var presentingViewController = menuViewController.PresentingViewController as UINavigationController;
                if (presentingViewController != null)
                    presentingViewController.PrepareForSegue(segue, sender: sender);
            }
        }

        public override bool ShouldPerformSegue(string segueIdentifier, NSObject sender)
        {
            var menuViewController = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController : SideMenuManager.menuRightNavigationController;
            if (menuViewController != null)
            {
                var presentingViewController = menuViewController.PresentingViewController as UINavigationController;
                if (presentingViewController != null)
                    return presentingViewController.ShouldPerformSegue(segueIdentifier, sender: sender);
            }

            return base.ShouldPerformSegue(segueIdentifier, sender: sender);
        }

        public override void PushViewController(UIViewController viewController, bool animated)
        {
            if (ViewControllers.Length <= 0)
            {
                // NOTE: pushViewController is called by init(rootViewController: UIViewController)
                // so we must perform the normal super method in this case.
                base.PushViewController(viewController, animated: true);
                return;
            }

            var presentingViewController = PresentingViewController as UINavigationController;
            if (presentingViewController == null)
            {
                PresentViewController(viewController, animated, null);
                System.Diagnostics.Debug.WriteLine("SideMenu Warning: cannot push a ViewController from a ViewController without a NavigationController. It will be presented it instead.");
                return;
            }

            // to avoid overlapping dismiss & pop/push calls, create a transaction block where the menu
            // is dismissed after showing the appropriate screen
            CATransaction.Begin();
            CATransaction.CompletionBlock = () =>
            {
                this.DismissViewController(true, null);
                this.VisibleViewController?.ViewWillAppear(false); // Hack: force selection to get cleared on UITableViewControllers when reappearing using custom transitions
            };

            UIView.Animate(SideMenuManager.menuAnimationDismissDuration, animation: () => SideMenuTransition.Current.hideMenuStart());

            if (SideMenuManager.menuAllowPopIfPossible)
            {
                foreach (var subViewController in presentingViewController.ViewControllers)
                {
                    //TODO: Review this
                    if (subViewController.GetType() == viewController.GetType()) // if subViewController.dynamicType == viewController.dynamicType {
                    {
                        presentingViewController.PopToViewController(subViewController, animated: animated);
                        CATransaction.Commit();
                        return;
                    }
                }
            }

            if (!SideMenuManager.menuAllowPushOfSameClassTwice)
            {
                //TODO: Review this
                if (presentingViewController.ViewControllers[presentingViewController.ViewControllers.Length - 1].GetType() == viewController.GetType()) //if presentingViewController.viewControllers.last?.dynamicType == viewController.dynamicType {
                {
                    CATransaction.Commit();
                    return;
                }
            }

            presentingViewController.PushViewController(viewController, animated: animated);
            CATransaction.Commit();
        }
    }
}