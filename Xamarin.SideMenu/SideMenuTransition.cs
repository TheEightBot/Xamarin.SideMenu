using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace EightBot.BigBang.iOS.SideMenu
{
    public class SideMenuTransition : UIPercentDrivenInteractiveTransition
    {
        private static SideMenuTransition sideMenu;
        public static SideMenuTransition Current
        {
            get
            {
                if (sideMenu == null)
                    sideMenu = new SideMenuTransition();

                return sideMenu;
            }
        }

        private SideMenuAnimatedTransitioning animatedTransitioning;
        public SideMenuAnimatedTransitioning AnimatedTransitioning
        {
            get
            {
                if (animatedTransitioning == null)
                    animatedTransitioning = new SideMenuAnimatedTransitioning();

                return animatedTransitioning;
            }
        }

        private SideMenuTransitioningDelegate transitioningDelegate;
        public SideMenuTransitioningDelegate TransitioningDelegate
        {
            get
            {
                if (transitioningDelegate == null)
                    transitioningDelegate = new SideMenuTransitioningDelegate();

                return transitioningDelegate;
            }
        }

        public static bool presenting = false;
        private static bool interactive = false;
        private static UIView originalSuperview;
        private static bool switchMenus = false;

        public static UIRectEdge presentDirection = UIRectEdge.Left;
        public static UIView tapView;
        public static UIView statusBarView;

        UIViewController viewControllerForPresentedMenu
        {
            get
            {
                return SideMenuManager.menuLeftNavigationController?.PresentingViewController != null
                    ? SideMenuManager.menuLeftNavigationController?.PresentingViewController
                    : SideMenuManager.menuRightNavigationController?.PresentingViewController;
            }
        }

        UIViewController visibleViewController
        {
            get
            {
                return getVisibleViewControllerFromViewController(UIApplication.SharedApplication.KeyWindow?.RootViewController);
            }
        }

        private UIViewController getVisibleViewControllerFromViewController(UIViewController viewController)
        {
            var navigationController = viewController as UINavigationController;
            if (navigationController != null)
                return getVisibleViewControllerFromViewController(navigationController.VisibleViewController);

            var tabBarController = viewController as UITabBarController;
            if (tabBarController != null)
                return getVisibleViewControllerFromViewController(tabBarController.SelectedViewController);

            var presentedViewController = viewController?.PresentedViewController;
            if (presentedViewController != null)
                return getVisibleViewControllerFromViewController(presentedViewController);

            return viewController;
        }

        public void handlePresentMenuLeftScreenEdge(UIScreenEdgePanGestureRecognizer edge)
        {
            SideMenuTransition.presentDirection = UIRectEdge.Left;
            handlePresentMenuPan(edge);
        }

        public void handlePresentMenuRightScreenEdge(UIScreenEdgePanGestureRecognizer edge)
        {
            SideMenuTransition.presentDirection = UIRectEdge.Right;
            handlePresentMenuPan(edge);
        }

        public void handlePresentMenuPan(UIPanGestureRecognizer pan)
        {
            // how much distance have we panned in reference to the parent view?
            var view = viewControllerForPresentedMenu != null ? viewControllerForPresentedMenu?.View : pan.View;
            if (view == null)
            {
                return;
            }

            var transform = view.Transform;
            view.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
            var translation = pan.TranslationInView(pan.View);
            view.Transform = transform;

            // do some math to translate this to a percentage based value
            if (!interactive) {
                if (translation.X == 0) {
                    return; // not sure which way the user is swiping yet, so do nothing
                }

                if (!(pan is UIScreenEdgePanGestureRecognizer)) {
                    SideMenuTransition.presentDirection = translation.X > 0 ? UIRectEdge.Left : UIRectEdge.Right;
                }

                var menuViewController = SideMenuTransition.presentDirection == UIRectEdge.Left
                    ? SideMenuManager.menuLeftNavigationController
                    : SideMenuManager.menuRightNavigationController;
                if (menuViewController != null && visibleViewController != null)
                {
                    interactive = true;
                    visibleViewController.PresentViewController(menuViewController, true, null);
                }
            }

            var direction = SideMenuTransition.presentDirection == UIRectEdge.Left ? 1 : -1;
            var distance = translation.X / SideMenuManager.menuWidth;
            // now lets deal with different states that the gesture recognizer sends
            switch (pan.State)
            {
                case UIGestureRecognizerState.Began:
                case UIGestureRecognizerState.Changed:
                    if (pan is UIScreenEdgePanGestureRecognizer) {
                        this.UpdateInteractiveTransition((float)Math.Min(distance * direction, 1));
                    }
                    else if (distance > 0 && SideMenuTransition.presentDirection == UIRectEdge.Right && SideMenuManager.menuLeftNavigationController != null) {
                        SideMenuTransition.presentDirection = UIRectEdge.Left;
                        switchMenus = true;
                        this.CancelInteractiveTransition();
                    }
                    else if (distance < 0 && SideMenuTransition.presentDirection == UIRectEdge.Left && SideMenuManager.menuRightNavigationController != null) {
                        SideMenuTransition.presentDirection = UIRectEdge.Right;
                        switchMenus = true;
                        this.CancelInteractiveTransition();
                    }
                    else
                    {
                        this.UpdateInteractiveTransition((float)Math.Min(distance * direction, 1));
                    }
                    break;

                default:
                    interactive = false;
                    view.Transform = CGAffineTransform.MakeIdentity();
                    var velocity = pan.VelocityInView(pan.View).X * direction;
                    view.Transform = transform;
                    if (velocity >= 100 || velocity >= -50 && Math.Abs(distance) >= 0.5)
                    {
                        //TODO: Review this... Uses FLT_EPSILON
                        //// bug workaround: animation briefly resets after call to finishInteractiveTransition() but before animateTransition completion is called.
                        //if (NSProcessInfo.ProcessInfo.OperatingSystemVersion.Major == 8 && this.percentComplete > 1f - 1.192092896e-07F) {
                        //            this.updateInteractiveTransition(0.9999);
                        //}
                        this.FinishInteractiveTransition();
                    }
                    else
                    {
                        this.CancelInteractiveTransition();
                    }
                    break;
            }
        }

        public void handleHideMenuPan(UIPanGestureRecognizer pan)
        {
            var translation = pan.TranslationInView(pan.View);
            var direction = SideMenuTransition.presentDirection == UIRectEdge.Left ? -1 : 1;
            var distance = translation.X / SideMenuManager.menuWidth * direction;
            
            switch (pan.State)
            {
                case UIGestureRecognizerState.Began:
                    interactive = true;
                    viewControllerForPresentedMenu?.DismissViewController(true, null);
                    break;
                case UIGestureRecognizerState.Changed:
                    Current.UpdateInteractiveTransition((float)Math.Max(Math.Min(distance, 1), 0));
                    break;
                default:
                    interactive = false;
                    var velocity = pan.VelocityInView(pan.View).X * direction;
                    if (velocity >= 100 || velocity >= -50 && distance >= 0.5)
                    {
                        ////TODO: Review this... Uses FLT_EPSILON
                        //// bug workaround: animation briefly resets after call to finishInteractiveTransition() but before animateTransition completion is called.
                        //if (NSProcessInfo.ProcessInfo.OperatingSystemVersion.Major == 8 && this.PercentComplete > 1 - 1.192092896e-07F)
                        //{
                        //    this.UpdateInteractiveTransition(0.9999);
                        //}
                        Current.FinishInteractiveTransition();
                    }
                    else
                    {
                        Current.CancelInteractiveTransition();
                    }
                    break;
            }
        }

        void handleHideMenuTap(UITapGestureRecognizer tap)
        {
            viewControllerForPresentedMenu?.DismissViewController(true, null);
        }

        public void hideMenuStart()
        {
            if(menuObserver != null)
                NSNotificationCenter.DefaultCenter.RemoveObserver(menuObserver);

            var mainViewController = SideMenuTransition.Current.viewControllerForPresentedMenu;
            var menuView = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController?.View : SideMenuManager.menuRightNavigationController?.View;
            if (mainViewController == null || menuView == null)
                return;

            menuView.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Alpha = 1;
            SideMenuTransition.tapView.Frame = new CGRect(0, 0, mainViewController.View.Frame.Width, mainViewController.View.Frame.Height);
            var frame = menuView.Frame;
            frame.Y = 0;
            frame.Size = new CGSize(SideMenuManager.menuWidth, mainViewController.View.Frame.Height);
            menuView.Frame = frame;
            if (SideMenuTransition.statusBarView != null)
            {
                SideMenuTransition.statusBarView.Frame = UIApplication.SharedApplication.StatusBarFrame;
                SideMenuTransition.statusBarView.Alpha = 0;
            }

            CGRect menuFrame;
            CGRect viewFrame;
            switch (SideMenuManager.menuPresentMode)
            {
                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                    menuView.Alpha = 1 - (float)SideMenuManager.menuAnimationFadeStrength;

                    menuFrame = menuView.Frame;
                    menuFrame.X = (float)(SideMenuTransition.presentDirection == UIRectEdge.Left ? 0 : mainViewController.View.Frame.Width - SideMenuManager.menuWidth);
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;

                    menuView.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.menuAnimationTransformScaleFactor, (float)SideMenuManager.menuAnimationTransformScaleFactor);
                    break;

                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    menuView.Alpha = 1;

                    menuFrame = menuView.Frame;
                    menuFrame.X = SideMenuTransition.presentDirection == UIRectEdge.Left ? -menuView.Frame.Width : mainViewController.View.Frame.Width;
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;
                    break;

                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                    menuView.Alpha = 1;

                    menuFrame = menuView.Frame;
                    menuFrame.X = SideMenuTransition.presentDirection == UIRectEdge.Left ? -menuView.Frame.Width : mainViewController.View.Frame.Width;
                    menuView.Frame = menuFrame;
                    break;

                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                    menuView.Alpha = 0;

                    menuFrame = menuView.Frame;
                    menuFrame.X = (float)(SideMenuTransition.presentDirection == UIRectEdge.Left ? 0 : mainViewController.View.Frame.Width - SideMenuManager.menuWidth);
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;
                    break;
            }
        }

        public void hideMenuComplete()
        {
            var mainViewController = SideMenuTransition.Current.viewControllerForPresentedMenu;
            var menuView = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController?.View : SideMenuManager.menuRightNavigationController?.View;
            if (mainViewController == null || menuView == null)
            {
                return;
            }

            SideMenuTransition.tapView.RemoveFromSuperview();
            SideMenuTransition.statusBarView?.RemoveFromSuperview();
            mainViewController.View.MotionEffects = new List<UIMotionEffect>().ToArray();
            mainViewController.View.Layer.ShadowOpacity = 0;
            menuView.Layer.ShadowOpacity = 0;
            var topNavigationController = mainViewController as UINavigationController;
            if (topNavigationController != null)
            {
                topNavigationController.InteractivePopGestureRecognizer.Enabled = true;
            }

            originalSuperview?.AddSubview(mainViewController.View);
        }

        public void presentMenuStart(CGSize? size = null)
        {
            if (size == null)
                size = SideMenuManager.appScreenRect.Size;

            var menuView = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController?.View : SideMenuManager.menuRightNavigationController?.View;
            var mainViewController = SideMenuTransition.Current.viewControllerForPresentedMenu;
            if (menuView == null || mainViewController == null)
                return;

            menuView.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Transform = CGAffineTransform.MakeIdentity();
            var menuFrame = menuView.Frame;
            menuFrame.Size = new CGSize(SideMenuManager.menuWidth, size.Value.Height);
            menuFrame.X = (float)(SideMenuTransition.presentDirection == UIRectEdge.Left ? 0 : size.Value.Width - SideMenuManager.menuWidth);
            menuView.Frame = menuFrame;

            if (SideMenuTransition.statusBarView != null)
            {
                SideMenuTransition.statusBarView.Frame = UIApplication.SharedApplication.StatusBarFrame;
                SideMenuTransition.statusBarView.Alpha = 1;
            }

            int direction = 0;
            CGRect frame;
            switch (SideMenuManager.menuPresentMode)
            {
                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                    menuView.Alpha = 1;
                    direction = SideMenuTransition.presentDirection == UIRectEdge.Left ? 1 : -1;
                    frame = mainViewController.View.Frame;
                    frame.X = direction * (menuView.Frame.Width);
                    mainViewController.View.Frame = frame;
                    mainViewController.View.Layer.ShadowColor = SideMenuManager.menuShadowColor.CGColor;
                    mainViewController.View.Layer.ShadowRadius = (float)SideMenuManager.menuShadowRadius;
                    mainViewController.View.Layer.ShadowOpacity = (float)SideMenuManager.menuShadowOpacity;
                    mainViewController.View.Layer.ShadowOffset = new CGSize(0, 0);
                    break;

                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    menuView.Alpha = 1;
                    menuView.Layer.ShadowColor = SideMenuManager.menuShadowColor.CGColor;
                    menuView.Layer.ShadowRadius = (float)SideMenuManager.menuShadowRadius;
                    menuView.Layer.ShadowOpacity = (float)SideMenuManager.menuShadowOpacity;
                    menuView.Layer.ShadowOffset = new CGSize(0, 0);
                    direction = SideMenuTransition.presentDirection == UIRectEdge.Left ? 1 : -1;
                    frame = mainViewController.View.Frame;
                    frame.X = direction * (menuView.Frame.Width);
                    mainViewController.View.Frame = frame;
                    mainViewController.View.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.menuAnimationTransformScaleFactor, (float)SideMenuManager.menuAnimationTransformScaleFactor);
                    mainViewController.View.Alpha = (float)(1 - SideMenuManager.menuAnimationFadeStrength);
                    break;

                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                    menuView.Alpha = 1;
                    menuView.Layer.ShadowColor = SideMenuManager.menuShadowColor.CGColor;
                    menuView.Layer.ShadowRadius = (float)SideMenuManager.menuShadowRadius;
                    menuView.Layer.ShadowOpacity = (float)SideMenuManager.menuShadowOpacity;
                    menuView.Layer.ShadowOffset = new CGSize(0, 0);
                    mainViewController.View.Frame = new CGRect(0, 0, size.Value.Width, size.Value.Height);
                    mainViewController.View.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.menuAnimationTransformScaleFactor, (float)SideMenuManager.menuAnimationTransformScaleFactor);
                    mainViewController.View.Alpha = (float)(1 - SideMenuManager.menuAnimationFadeStrength);
                    break;
            }
        }

        NSObject menuObserver;
        void presentMenuComplete()
        {
            //TODO: Review this
            menuObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, (_) => TransitioningDelegate.applicationDidEnterBackgroundNotification());

            var mainViewController = SideMenuTransition.Current.viewControllerForPresentedMenu;
            if (mainViewController == null)
                return;

            switch (SideMenuManager.menuPresentMode) {
                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    if (SideMenuManager.menuParallaxStrength != 0) {
                        var horizontal = new UIInterpolatingMotionEffect(keyPath: "center.x", type: UIInterpolatingMotionEffectType.TiltAlongHorizontalAxis);
                        horizontal.MinimumRelativeValue = NSNumber.FromInt32(-SideMenuManager.menuParallaxStrength);
                        horizontal.MinimumRelativeValue = NSNumber.FromInt32(SideMenuManager.menuParallaxStrength);

                        var vertical = new UIInterpolatingMotionEffect(keyPath: "center.y", type: UIInterpolatingMotionEffectType.TiltAlongVerticalAxis);
                        vertical.MinimumRelativeValue = NSNumber.FromInt32(- SideMenuManager.menuParallaxStrength);
                        vertical.MaximumRelativeValue = NSNumber.FromInt32(SideMenuManager.menuParallaxStrength);

                        var group = new UIMotionEffectGroup();
                        group.MotionEffects = new UIMotionEffect[] { horizontal, vertical };
                        mainViewController.View.AddMotionEffect(group);
                    }
                    break;
                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                    break;
            }

            var topNavigationController = mainViewController as UINavigationController;
            if (topNavigationController != null) {
                topNavigationController.InteractivePopGestureRecognizer.Enabled = false;
            }
        }

        // MARK: UIViewControllerAnimatedTransitioning protocol methods

        public class SideMenuAnimatedTransitioning : UIViewControllerAnimatedTransitioning
        {
            // animate a change from one viewcontroller to another
            public override void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
            {
                // get reference to our fromView, toView and the container view that we should perform the transition in
                var container = transitionContext.ContainerView;
                var menuBackgroundColor = SideMenuManager.menuAnimationBackgroundColor;
                if (menuBackgroundColor != null)
                {
                    container.BackgroundColor = menuBackgroundColor;
                }

                // create a tuple of our screens
                var screens = new
                {
                    from = transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey),
                    to = transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey)
                };

                // assign references to our menu view controller and the 'bottom' view controller from the tuple
                // remember that our menuViewController will alternate between the from and to view controller depending if we're presenting or dismissing
                var menuViewController = (!SideMenuTransition.presenting ? screens.from : screens.to);
                var topViewController = !presenting ? screens.to : screens.from;

                var menuView = menuViewController.View;
                var topView = topViewController.View;

                // prepare menu items to slide in
                if (presenting)
                {
                    var tapView = new UIView();
                    tapView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
                    var exitPanGesture = new UIPanGestureRecognizer();
                    exitPanGesture.AddTarget(/*SideMenuTransition.Current, */() => SideMenuTransition.Current.handleHideMenuPan(exitPanGesture));
                    var exitTapGesture = new UITapGestureRecognizer();
                    exitTapGesture.AddTarget(/*SideMenuTransition.Current, */() => SideMenuTransition.Current.handleHideMenuTap(exitTapGesture));
                    tapView.AddGestureRecognizer(exitPanGesture);
                    tapView.AddGestureRecognizer(exitTapGesture);
                    SideMenuTransition.tapView = tapView;

                    SideMenuTransition.originalSuperview = topView.Superview;

                    // add the both views to our view controller
                    switch (SideMenuManager.menuPresentMode)
                    {
                        case SideMenuManager.MenuPresentMode.ViewSlideOut:
                            container.AddSubview(menuView);
                            container.AddSubview(topView);
                            topView.AddSubview(tapView);
                            break;
                        case SideMenuManager.MenuPresentMode.MenuSlideIn:
                        case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                        case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                            container.AddSubview(topView);
                            container.AddSubview(tapView);
                            container.AddSubview(menuView);
                            break;
                    }

                    if (SideMenuManager.menuFadeStatusBar)
                    {
                        var blackBar = new UIView();
                        var menuShrinkBackgroundColor = SideMenuManager.menuAnimationBackgroundColor;
                        if (menuShrinkBackgroundColor != null)
                        {
                            blackBar.BackgroundColor = menuShrinkBackgroundColor;
                        }
                        else
                        {
                            blackBar.BackgroundColor = UIColor.Black;
                        }
                        blackBar.UserInteractionEnabled = false;
                        container.AddSubview(blackBar);
                        SideMenuTransition.statusBarView = blackBar;
                    }

                    SideMenuTransition.Current.hideMenuStart(); // offstage for interactive
                }

                // perform the animation!
                var duration = TransitionDuration(transitionContext);
                var options = interactive ? UIViewAnimationOptions.CurveLinear : UIViewAnimationOptions.CurveEaseInOut;
                UIView.Animate(duration, 0, options,
                    animation: () =>
                    {
                        if (SideMenuTransition.presenting)
                        {
                            SideMenuTransition.Current.presentMenuStart(); // onstage items: slide in
                    }
                        else
                        {
                            SideMenuTransition.Current.hideMenuStart();
                        }
                        menuView.UserInteractionEnabled = false;
                    },
                    completion: () =>
                    {
                    // tell our transitionContext object that we've finished animating
                    if (transitionContext.TransitionWasCancelled)
                        {
                            var viewControllerForPresentedMenu = SideMenuTransition.Current.viewControllerForPresentedMenu;

                            if (SideMenuTransition.presenting)
                            {
                                SideMenuTransition.Current.hideMenuComplete();
                            }
                            else
                            {
                                SideMenuTransition.Current.presentMenuComplete();
                            }
                            menuView.UserInteractionEnabled = true;

                            transitionContext.CompleteTransition(false);


                            if (SideMenuTransition.switchMenus)
                            {
                                SideMenuTransition.switchMenus = false;
                                viewControllerForPresentedMenu?.PresentViewController(
                                    SideMenuTransition.presentDirection == UIRectEdge.Left
                                        ? SideMenuManager.menuLeftNavigationController
                                        : SideMenuManager.menuRightNavigationController,
                                    true, null);
                            }

                            return;
                        }

                        if (SideMenuTransition.presenting)
                        {
                            SideMenuTransition.Current.presentMenuComplete();
                            menuView.UserInteractionEnabled = true;
                            transitionContext.CompleteTransition(true);
                            switch (SideMenuManager.menuPresentMode)
                            {
                                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                                    container.AddSubview(topView);
                                    break;
                                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                                    container.InsertSubview(topView, atIndex: 0);
                                    break;
                            }

                            var statusBarView = SideMenuTransition.statusBarView;
                            if (statusBarView != null)
                            {
                                container.BringSubviewToFront(statusBarView);
                            }
                            return;
                        }

                        SideMenuTransition.Current.hideMenuComplete();
                        transitionContext.CompleteTransition(true);
                        menuView.RemoveFromSuperview();
                    });
            }

            // return how many seconds the transiton animation will take
            public override double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            {
                return SideMenuTransition.presenting ? SideMenuManager.menuAnimationPresentDuration : SideMenuManager.menuAnimationDismissDuration;
            }
        }


        // MARK: UIViewControllerTransitioningDelegate protocol methods

        public class SideMenuTransitioningDelegate : UIViewControllerTransitioningDelegate
        {
            // return the animator when presenting a viewcontroller
            // rememeber that an animator (or animation controller) is any object that aheres to the UIViewControllerAnimatedTransitioning protocol
            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForPresentedController(UIViewController presented, UIViewController presentingViewController, UIViewController source)
            {
                presenting = true;
                presentDirection = presented == SideMenuManager.menuLeftNavigationController ? UIRectEdge.Left : UIRectEdge.Right;
                return SideMenuTransition.Current.AnimatedTransitioning;
            }

            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
            {
                presenting = false;
                return SideMenuTransition.Current.AnimatedTransitioning;
            }

            public override IUIViewControllerInteractiveTransitioning GetInteractionControllerForPresentation(IUIViewControllerAnimatedTransitioning animator)
            {
                // if our interactive flag is true, return the transition manager object
                // otherwise return nil
                //TODO: Fix this. Cast not working...
                return null;// interactive ? SideMenuTransition.Current : null;
            }

            public override IUIViewControllerInteractiveTransitioning GetInteractionControllerForDismissal(IUIViewControllerAnimatedTransitioning animator)
            {
                //TODO: Fix this. Cast not working...
                return null;// interactive ? SideMenuTransition.Current : null;
            }

            public void applicationDidEnterBackgroundNotification()
            {
                var menuViewController = SideMenuTransition.presentDirection == UIRectEdge.Left ? SideMenuManager.menuLeftNavigationController : SideMenuManager.menuRightNavigationController;
                if (menuViewController != null)
                {
                    SideMenuTransition.Current.hideMenuStart();
                    SideMenuTransition.Current.hideMenuComplete();
                    menuViewController.DismissViewController(false, null);
                }
            }
        }
    }
}