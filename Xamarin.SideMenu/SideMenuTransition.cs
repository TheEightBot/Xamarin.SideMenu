using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Xamarin.SideMenu
{
    public class SideMenuTransition : UIPercentDrivenInteractiveTransition
    {
        public SideMenuManager SideMenuManager { get; set; }

        public SideMenuTransition(SideMenuManager sideMenuManager)
        {
            SideMenuManager = sideMenuManager;
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                SideMenuManager = null;
            }

            base.Dispose(disposing);
        }

        SideMenuAnimatedTransitioning animatedTransitioning;
        public SideMenuAnimatedTransitioning AnimatedTransitioning
        {
            get
            {
                if (animatedTransitioning == null)
                    animatedTransitioning = new SideMenuAnimatedTransitioning(this);

                return animatedTransitioning;
            }
        }

        SideMenuTransitioningDelegate transitioningDelegate;
        public SideMenuTransitioningDelegate TransitioningDelegate
        {
            get
            {
                if (transitioningDelegate == null)
                    transitioningDelegate = new SideMenuTransitioningDelegate(this);

                return transitioningDelegate;
            }
        }

		public bool Presenting { get; private set; }
		bool interactive = false;
        UIView originalSuperview;
        bool switchMenus = false;

        public UIRectEdge PresentDirection = UIRectEdge.Left;
        public UIView TapView;
        public UIView StatusBarView;

        UIViewController viewControllerForPresentedMenu
        {
            get
            {
                return SideMenuManager.LeftNavigationController?.PresentingViewController != null
                    ? SideMenuManager.LeftNavigationController?.PresentingViewController
                    : SideMenuManager.RightNavigationController?.PresentingViewController;
            }
        }

        UIViewController visibleViewController
        {
            get
            {
                return GetVisibleViewControllerFromViewController(UIApplication.SharedApplication.KeyWindow?.RootViewController);
            }
        }

		UIViewController GetVisibleViewControllerFromViewController(UIViewController viewController)
        {
            var navigationController = viewController as UINavigationController;
            if (navigationController != null)
                return GetVisibleViewControllerFromViewController(navigationController.VisibleViewController);

            var tabBarController = viewController as UITabBarController;
            if (tabBarController != null)
                return GetVisibleViewControllerFromViewController(tabBarController.SelectedViewController);

            var presentedViewController = viewController?.PresentedViewController;
            if (presentedViewController != null)
                return GetVisibleViewControllerFromViewController(presentedViewController);

            return viewController;
        }

        public void HandlePresentMenuLeftScreenEdge(UIScreenEdgePanGestureRecognizer edge)
        {
			this.PresentDirection = UIRectEdge.Left;
			HandlePresentMenuPan(edge);
        }

        public void HandlePresentMenuRightScreenEdge(UIScreenEdgePanGestureRecognizer edge)
        {
			this.PresentDirection = UIRectEdge.Right;
			HandlePresentMenuPan(edge);
        }

        public void HandlePresentMenuPan(UIPanGestureRecognizer pan)
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
                    this.PresentDirection = translation.X > 0 ? UIRectEdge.Left : UIRectEdge.Right;
                }

                var menuViewController = this.PresentDirection == UIRectEdge.Left
                    ? SideMenuManager.LeftNavigationController
                    : SideMenuManager.RightNavigationController;
                if (menuViewController != null && visibleViewController != null)
                {
                    interactive = true;
                    visibleViewController.PresentViewController(menuViewController, true, null);
                }
            }

            var direction = this.PresentDirection == UIRectEdge.Left ? 1 : -1;
            var distance = translation.X / SideMenuManager.MenuWidth;
            // now lets deal with different states that the gesture recognizer sends
            switch (pan.State)
            {
                case UIGestureRecognizerState.Began:
                case UIGestureRecognizerState.Changed:
                    if (pan is UIScreenEdgePanGestureRecognizer) {
                        this.UpdateInteractiveTransition((float)Math.Min(distance * direction, 1));
                    }
                    else if (distance > 0 && this.PresentDirection == UIRectEdge.Right && SideMenuManager.LeftNavigationController != null) {
                        this.PresentDirection = UIRectEdge.Left;
                        switchMenus = true;
                        this.CancelInteractiveTransition();
                    }
                    else if (distance < 0 && this.PresentDirection == UIRectEdge.Left && SideMenuManager.RightNavigationController != null) {
                        this.PresentDirection = UIRectEdge.Right;
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

        public void HandleHideMenuPan(UIPanGestureRecognizer pan)
        {
            var translation = pan.TranslationInView(pan.View);
            var direction = this.PresentDirection == UIRectEdge.Left ? -1 : 1;
            var distance = translation.X / SideMenuManager.MenuWidth * direction;
            
            switch (pan.State)
            {
                case UIGestureRecognizerState.Began:
                    interactive = true;
                    viewControllerForPresentedMenu?.DismissViewController(true, null);
                    break;
                case UIGestureRecognizerState.Changed:
                    this.UpdateInteractiveTransition((float)Math.Max(Math.Min(distance, 1), 0));
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
                        this.FinishInteractiveTransition();
                    }
                    else
                    {
                        this.CancelInteractiveTransition();
                    }
                    break;
            }
        }

		void HandleHideMenuTap(UITapGestureRecognizer tap)
        {
            viewControllerForPresentedMenu?.DismissViewController(true, null);
        }

        public void HideMenuStart()
        {
            if(menuObserver != null)
                NSNotificationCenter.DefaultCenter.RemoveObserver(menuObserver);

            var mainViewController = this.viewControllerForPresentedMenu;
            var menuView = this.PresentDirection == UIRectEdge.Left ? SideMenuManager.LeftNavigationController?.View : SideMenuManager.RightNavigationController?.View;
            if (mainViewController == null || menuView == null)
                return;

            menuView.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Alpha = 1;
            this.TapView.Frame = new CGRect(0, 0, mainViewController.View.Frame.Width, mainViewController.View.Frame.Height);
            var frame = menuView.Frame;
            frame.Y = 0;
            frame.Size = new CGSize(SideMenuManager.MenuWidth, mainViewController.View.Frame.Height);
            menuView.Frame = frame;
            if (this.StatusBarView != null)
            {
                this.StatusBarView.Frame = UIApplication.SharedApplication.StatusBarFrame;
                this.StatusBarView.Alpha = 0;
            }

            CGRect menuFrame;
            CGRect viewFrame;
            switch (SideMenuManager.PresentMode)
            {
                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                    menuView.Alpha = 1 - (float)SideMenuManager.AnimationFadeStrength;

                    menuFrame = menuView.Frame;
                    menuFrame.X = (float)(this.PresentDirection == UIRectEdge.Left ? 0 : mainViewController.View.Frame.Width - SideMenuManager.MenuWidth);
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;

                    menuView.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.AnimationTransformScaleFactor, (float)SideMenuManager.AnimationTransformScaleFactor);
                    break;

                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    menuView.Alpha = 1;

                    menuFrame = menuView.Frame;
                    menuFrame.X = this.PresentDirection == UIRectEdge.Left ? -menuView.Frame.Width : mainViewController.View.Frame.Width;
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;
                    break;

                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                    menuView.Alpha = 1;

                    menuFrame = menuView.Frame;
                    menuFrame.X = this.PresentDirection == UIRectEdge.Left ? -menuView.Frame.Width : mainViewController.View.Frame.Width;
                    menuView.Frame = menuFrame;
                    break;

                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                    menuView.Alpha = 0;

                    menuFrame = menuView.Frame;
                    menuFrame.X = (float)(this.PresentDirection == UIRectEdge.Left ? 0 : mainViewController.View.Frame.Width - SideMenuManager.MenuWidth);
                    menuView.Frame = menuFrame;

                    viewFrame = mainViewController.View.Frame;
                    viewFrame.X = 0;
                    mainViewController.View.Frame = viewFrame;
                    break;
            }
        }

        public void HideMenuComplete()
        {
            var mainViewController = this.viewControllerForPresentedMenu;
            var menuView = this.PresentDirection == UIRectEdge.Left ? SideMenuManager.LeftNavigationController?.View : SideMenuManager.RightNavigationController?.View;
            if (mainViewController == null || menuView == null)
            {
                return;
            }

            this.TapView.RemoveFromSuperview();
            this.StatusBarView?.RemoveFromSuperview();
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

        public void PresentMenuStart(CGSize? size = null)
        {
            if (size == null)
                size = SideMenuManager.appScreenRect.Size;

            var menuView = this.PresentDirection == UIRectEdge.Left ? SideMenuManager.LeftNavigationController?.View : SideMenuManager.RightNavigationController?.View;
            var mainViewController = this.viewControllerForPresentedMenu;
            if (menuView == null || mainViewController == null)
                return;

            menuView.Transform = CGAffineTransform.MakeIdentity();
            mainViewController.View.Transform = CGAffineTransform.MakeIdentity();
            var menuFrame = menuView.Frame;
            menuFrame.Size = new CGSize(SideMenuManager.MenuWidth, size.Value.Height);
            menuFrame.X = (float)(this.PresentDirection == UIRectEdge.Left ? 0 : size.Value.Width - SideMenuManager.MenuWidth);
            menuView.Frame = menuFrame;

            if (this.StatusBarView != null)
            {
                this.StatusBarView.Frame = UIApplication.SharedApplication.StatusBarFrame;
                this.StatusBarView.Alpha = 1;
            }

            int direction = 0;
            CGRect frame;
            switch (SideMenuManager.PresentMode)
            {
                case SideMenuManager.MenuPresentMode.ViewSlideOut:
                    menuView.Alpha = 1;
                    direction = this.PresentDirection == UIRectEdge.Left ? 1 : -1;
                    frame = mainViewController.View.Frame;
                    frame.X = direction * (menuView.Frame.Width);
                    mainViewController.View.Frame = frame;
                    mainViewController.View.Layer.ShadowColor = SideMenuManager.ShadowColor.CGColor;
                    mainViewController.View.Layer.ShadowRadius = (float)SideMenuManager.ShadowRadius;
                    mainViewController.View.Layer.ShadowOpacity = (float)SideMenuManager.ShadowOpacity;
                    mainViewController.View.Layer.ShadowOffset = new CGSize(0, 0);
                    break;

                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    menuView.Alpha = 1;
                    menuView.Layer.ShadowColor = SideMenuManager.ShadowColor.CGColor;
                    menuView.Layer.ShadowRadius = (float)SideMenuManager.ShadowRadius;
                    menuView.Layer.ShadowOpacity = (float)SideMenuManager.ShadowOpacity;
                    menuView.Layer.ShadowOffset = new CGSize(0, 0);
                    direction = this.PresentDirection == UIRectEdge.Left ? 1 : -1;
                    frame = mainViewController.View.Frame;
                    frame.X = direction * (menuView.Frame.Width);
                    mainViewController.View.Frame = frame;
                    mainViewController.View.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.AnimationTransformScaleFactor, (float)SideMenuManager.AnimationTransformScaleFactor);
                    mainViewController.View.Alpha = (float)(1 - SideMenuManager.AnimationFadeStrength);
                    break;

                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                    menuView.Alpha = 1;
                    menuView.Layer.ShadowColor = SideMenuManager.ShadowColor.CGColor;
                    menuView.Layer.ShadowRadius = (float)SideMenuManager.ShadowRadius;
                    menuView.Layer.ShadowOpacity = (float)SideMenuManager.ShadowOpacity;
                    menuView.Layer.ShadowOffset = new CGSize(0, 0);
                    mainViewController.View.Frame = new CGRect(0, 0, size.Value.Width, size.Value.Height);
                    mainViewController.View.Transform = CGAffineTransform.MakeScale((float)SideMenuManager.AnimationTransformScaleFactor, (float)SideMenuManager.AnimationTransformScaleFactor);
                    mainViewController.View.Alpha = (float)(1 - SideMenuManager.AnimationFadeStrength);
                    break;
            }
        }

        NSObject menuObserver;
        void presentMenuComplete()
        {
            //TODO: Review this
            menuObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, (_) => TransitioningDelegate.ApplicationDidEnterBackgroundNotification());

            var mainViewController = this.viewControllerForPresentedMenu;
            if (mainViewController == null)
                return;

            switch (SideMenuManager.PresentMode) {
                case SideMenuManager.MenuPresentMode.MenuSlideIn:
                case SideMenuManager.MenuPresentMode.MenuDissolveIn:
                case SideMenuManager.MenuPresentMode.ViewSlideInOut:
                    if (SideMenuManager.ParallaxStrength != 0) {
                        var horizontal = new UIInterpolatingMotionEffect(keyPath: "center.x", type: UIInterpolatingMotionEffectType.TiltAlongHorizontalAxis);
                        horizontal.MinimumRelativeValue = NSNumber.FromInt32(-SideMenuManager.ParallaxStrength);
                        horizontal.MinimumRelativeValue = NSNumber.FromInt32(SideMenuManager.ParallaxStrength);

                        var vertical = new UIInterpolatingMotionEffect(keyPath: "center.y", type: UIInterpolatingMotionEffectType.TiltAlongVerticalAxis);
                        vertical.MinimumRelativeValue = NSNumber.FromInt32(- SideMenuManager.ParallaxStrength);
                        vertical.MaximumRelativeValue = NSNumber.FromInt32(SideMenuManager.ParallaxStrength);

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
            SideMenuTransition _sideMenuTransition;
            public SideMenuAnimatedTransitioning(SideMenuTransition sideMenuTransition)
            {
                _sideMenuTransition = sideMenuTransition;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _sideMenuTransition = null;
                }

                base.Dispose(disposing);
            }

            // animate a change from one viewcontroller to another
            public override void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
            {
                // get reference to our fromView, toView and the container view that we should perform the transition in
                var container = transitionContext.ContainerView;
                var menuBackgroundColor = _sideMenuTransition.SideMenuManager.AnimationBackgroundColor;
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
                var menuViewController = (!_sideMenuTransition.Presenting ? screens.from : screens.to);
                var topViewController = !_sideMenuTransition.Presenting ? screens.to : screens.from;

                var menuView = menuViewController.View;
                var topView = topViewController.View;

                // prepare menu items to slide in
                if (_sideMenuTransition.Presenting)
                {
                    var tapView = new UIView();
                    tapView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
                    var exitPanGesture = new UIPanGestureRecognizer();
                    exitPanGesture.AddTarget(/*SideMenuTransition.Current, */() => _sideMenuTransition.HandleHideMenuPan(exitPanGesture));
                    var exitTapGesture = new UITapGestureRecognizer();
                    exitTapGesture.AddTarget(/*SideMenuTransition.Current, */() => _sideMenuTransition.HandleHideMenuTap(exitTapGesture));
                    tapView.AddGestureRecognizer(exitPanGesture);
                    tapView.AddGestureRecognizer(exitTapGesture);
                    _sideMenuTransition.TapView = tapView;

                    _sideMenuTransition.originalSuperview = topView.Superview;

                    // add the both views to our view controller
                    switch (_sideMenuTransition.SideMenuManager.PresentMode)
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

                    if (_sideMenuTransition.SideMenuManager.FadeStatusBar)
                    {
                        var blackBar = new UIView();
                        var menuShrinkBackgroundColor = _sideMenuTransition.SideMenuManager.AnimationBackgroundColor;
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
                        _sideMenuTransition.StatusBarView = blackBar;
                    }

                    _sideMenuTransition.HideMenuStart(); // offstage for interactive
                }

                // perform the animation!
                var duration = TransitionDuration(transitionContext);
                var options = _sideMenuTransition.interactive ? UIViewAnimationOptions.CurveLinear : UIViewAnimationOptions.CurveEaseInOut;
                UIView.Animate(duration, 0, options,
                    animation: () =>
                    {
                        if (_sideMenuTransition.Presenting)
                        {
                            _sideMenuTransition.PresentMenuStart(); // onstage items: slide in
                    }
                        else
                        {
                            _sideMenuTransition.HideMenuStart();
                        }
                        menuView.UserInteractionEnabled = false;
                    },
                    completion: () =>
                    {
                    // tell our transitionContext object that we've finished animating
                    if (transitionContext.TransitionWasCancelled)
                        {
                            var viewControllerForPresentedMenu = _sideMenuTransition.viewControllerForPresentedMenu;

                            if (_sideMenuTransition.Presenting)
                            {
                                _sideMenuTransition.HideMenuComplete();
                            }
                            else
                            {
                                _sideMenuTransition.presentMenuComplete();
                            }
                            menuView.UserInteractionEnabled = true;

                            transitionContext.CompleteTransition(false);


                            if (_sideMenuTransition.switchMenus)
                            {
                                _sideMenuTransition.switchMenus = false;
                                viewControllerForPresentedMenu?.PresentViewController(
                                    _sideMenuTransition.PresentDirection == UIRectEdge.Left
                                        ? _sideMenuTransition.SideMenuManager.LeftNavigationController
                                        : _sideMenuTransition.SideMenuManager.RightNavigationController,
                                    true, null);
                            }

                            return;
                        }

                        if (_sideMenuTransition.Presenting)
                        {
                            _sideMenuTransition.presentMenuComplete();
                            menuView.UserInteractionEnabled = true;
                            transitionContext.CompleteTransition(true);
                            switch (_sideMenuTransition.SideMenuManager.PresentMode)
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

                            var statusBarView = _sideMenuTransition.StatusBarView;
                            if (statusBarView != null)
                            {
                                container.BringSubviewToFront(statusBarView);
                            }
                            return;
                        }

                        _sideMenuTransition.HideMenuComplete();
                        transitionContext.CompleteTransition(true);
                        menuView.RemoveFromSuperview();
                    });
            }

            // return how many seconds the transiton animation will take
            public override double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            {
                return _sideMenuTransition.Presenting ? _sideMenuTransition.SideMenuManager.AnimationPresentDuration : _sideMenuTransition.SideMenuManager.AnimationDismissDuration;
            }
        }


        // MARK: UIViewControllerTransitioningDelegate protocol methods

        public class SideMenuTransitioningDelegate : UIViewControllerTransitioningDelegate
        {
            private SideMenuTransition _sideMenuTransition;
            public SideMenuTransitioningDelegate(SideMenuTransition sideMenuTransition)
            {
                _sideMenuTransition = sideMenuTransition;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _sideMenuTransition = null;
                }

                base.Dispose(disposing);
            }

            // return the animator when presenting a viewcontroller
            // rememeber that an animator (or animation controller) is any object that aheres to the UIViewControllerAnimatedTransitioning protocol
            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForPresentedController(UIViewController presented, UIViewController presentingViewController, UIViewController source)
            {
                _sideMenuTransition.Presenting = true;
                _sideMenuTransition.PresentDirection = presented == _sideMenuTransition.SideMenuManager.LeftNavigationController ? UIRectEdge.Left : UIRectEdge.Right;
                return _sideMenuTransition.AnimatedTransitioning;
            }

            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
            {
                _sideMenuTransition.Presenting = false;
                return _sideMenuTransition.AnimatedTransitioning;
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

            public void ApplicationDidEnterBackgroundNotification()
            {
                var menuViewController = _sideMenuTransition.PresentDirection == UIRectEdge.Left ? _sideMenuTransition.SideMenuManager.LeftNavigationController : _sideMenuTransition.SideMenuManager.RightNavigationController;
                if (menuViewController != null)
                {
                    _sideMenuTransition.HideMenuStart();
                    _sideMenuTransition.HideMenuComplete();
                    menuViewController.DismissViewController(false, null);
                }
            }
        }
    }
}