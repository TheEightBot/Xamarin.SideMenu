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

		SideMenuManager _sideMenuManager;

		UISegmentedControl _presentationMode, _menuBlurStyle;

		UISlider _menuFadeStrength, _menuShadowOpacity, _menuScreenWidth, _menuTransformScaleFactor;

		UISwitch _menuFadeStatusBar;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

			this.NavigationItem.SetLeftBarButtonItem(
				new UIBarButtonItem("Left Menu", UIBarButtonItemStyle.Plain, (sender, e) => { 
					PresentViewController(_sideMenuManager.LeftNavigationController, true, null);
				}),
				false);

			this.NavigationItem.SetRightBarButtonItem(
				new UIBarButtonItem("Right Menu", UIBarButtonItemStyle.Plain, (sender, e) => { 
					PresentViewController(_sideMenuManager.RightNavigationController, true, null);
				}),
				false);

            _sideMenuManager = new SideMenuManager();

            View.BackgroundColor = UIColor.White;
            Title = "Swipe Here";

			var menuPresentMode = new UILabel { 
				Text = "Menu Present Mode",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuPresentMode);

			_presentationMode = new UISegmentedControl {
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_presentationMode.InsertSegment("Slide In", 0, false);
			_presentationMode.InsertSegment("Slide Out", 1, false);
			_presentationMode.InsertSegment("In + Out", 2, false);
			_presentationMode.InsertSegment("Dissolve", 3, false);
			_presentationMode.SelectedSegment = 0;

			_presentationMode.ValueChanged += (sender, e) => {
				_sideMenuManager.PresentMode = (SideMenuManager.MenuPresentMode)(int)_presentationMode.SelectedSegment;
			};

			this.View.Add(_presentationMode);

			var menuBlurStyle = new UILabel
			{
				Text = "Menu Blur Style",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuBlurStyle);

			_menuBlurStyle = new UISegmentedControl
			{
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_menuBlurStyle.InsertSegment("None", 0, false);
			_menuBlurStyle.InsertSegment("Extra Light", 1, false);
			_menuBlurStyle.InsertSegment("Light", 2, false);
			_menuBlurStyle.InsertSegment("Dark", 3, false);
			_menuBlurStyle.SelectedSegment = 0;

			_menuBlurStyle.ValueChanged += (sender, e) =>
			{
				if (_menuBlurStyle.SelectedSegment == 0)
					_sideMenuManager.BlurEffectStyle = default(UIBlurEffectStyle);
				else
					_sideMenuManager.BlurEffectStyle = (UIBlurEffectStyle)(int)_menuBlurStyle.SelectedSegment - 1;
			};

			this.View.Add(_menuBlurStyle);

			var menuFadeStrength = new UILabel
			{
				Text = "Menu Fade Strength",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuFadeStrength);

			_menuFadeStrength = new UISlider
			{
				MinValue = 0f,
				MaxValue = 1f,
				Continuous = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_menuFadeStrength.ValueChanged += (sender, e) => {
				_sideMenuManager.AnimationFadeStrength = _menuFadeStrength.Value;
			};
			this.View.Add(_menuFadeStrength);

			var menuShadowOpacity = new UILabel
			{
				Text = "Menu Shadow Opacity",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuShadowOpacity);

			_menuShadowOpacity = new UISlider
			{
				MinValue = 0f,
				MaxValue = 1f,
				Value = .5f,
				Continuous = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_menuShadowOpacity.ValueChanged += (sender, e) =>
			{
				_sideMenuManager.ShadowOpacity = _menuShadowOpacity.Value;
			};
			this.View.Add(_menuShadowOpacity);

			var menuScreenWidth = new UILabel
			{
				Text = "Menu Screen Width",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuScreenWidth);

			_menuScreenWidth = new UISlider
			{
				MinValue = 0f,
				MaxValue = 1f,
				Value = .75f,
				Continuous = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_menuScreenWidth.ValueChanged += (sender, e) =>
			{
				_sideMenuManager.MenuWidth = this.View.Frame.Width * _menuScreenWidth.Value;
			};
			this.View.Add(_menuScreenWidth);

			var menuTransformScaleFactor = new UILabel
			{
				Text = "Menu Transform Scale Factor",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuTransformScaleFactor);

			_menuTransformScaleFactor = new UISlider
			{
				MinValue = 0f,
				MaxValue = 2f,
				Value = 1f,
				Continuous = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_menuTransformScaleFactor.ValueChanged += (sender, e) =>
			{
				_sideMenuManager.AnimationTransformScaleFactor = _menuTransformScaleFactor.Value;
			};
			this.View.Add(_menuTransformScaleFactor);

			var menuFadeStatusBar = new UILabel
			{
				Text = "Menu Fade Status Bar",
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.View.Add(menuFadeStatusBar);

			_menuFadeStatusBar = new UISwitch
			{
				On = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			_menuFadeStatusBar.ValueChanged += (sender, e) => {
				_sideMenuManager.FadeStatusBar = _menuFadeStatusBar.On;
			};
			this.View.Add(_menuFadeStatusBar);

			var padding = 8f;

			this.View.AddConstraints(
				new NSLayoutConstraint[] {
					NSLayoutConstraint.Create(
						menuPresentMode, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						this.TopLayoutGuide, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuPresentMode, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
                  	NSLayoutConstraint.Create(
						menuPresentMode, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
                  	NSLayoutConstraint.Create(
						menuPresentMode, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

					NSLayoutConstraint.Create(
						_presentationMode, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuPresentMode, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_presentationMode, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_presentationMode, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
                  	NSLayoutConstraint.Create(
						_presentationMode, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

					NSLayoutConstraint.Create(
						menuBlurStyle, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_presentationMode, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuBlurStyle, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuBlurStyle, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
					  NSLayoutConstraint.Create(
						menuBlurStyle, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

                  	NSLayoutConstraint.Create(
						_menuBlurStyle, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuBlurStyle, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_menuBlurStyle, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_menuBlurStyle, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
					  NSLayoutConstraint.Create(
						_menuBlurStyle, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

                  	NSLayoutConstraint.Create(
						menuFadeStrength, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_menuBlurStyle, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuFadeStrength, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuFadeStrength, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
					  NSLayoutConstraint.Create(
						menuFadeStrength, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

					  NSLayoutConstraint.Create(
						_menuFadeStrength, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuFadeStrength, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_menuFadeStrength, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_menuFadeStrength, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
					  NSLayoutConstraint.Create(
						_menuFadeStrength, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

                  	NSLayoutConstraint.Create(
						menuShadowOpacity, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_menuFadeStrength, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuShadowOpacity, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuShadowOpacity, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
					  NSLayoutConstraint.Create(
						menuShadowOpacity, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

				  	NSLayoutConstraint.Create(
						_menuShadowOpacity, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuShadowOpacity, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_menuShadowOpacity, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_menuShadowOpacity, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						_menuShadowOpacity, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

                  	NSLayoutConstraint.Create(
						menuScreenWidth, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_menuShadowOpacity, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuScreenWidth, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuScreenWidth, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						menuScreenWidth, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

				  	NSLayoutConstraint.Create(
						_menuScreenWidth, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuScreenWidth, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_menuScreenWidth, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_menuScreenWidth, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						_menuScreenWidth, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

				  	NSLayoutConstraint.Create(
						menuTransformScaleFactor, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_menuScreenWidth, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuTransformScaleFactor, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuTransformScaleFactor, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						menuTransformScaleFactor, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

				  	NSLayoutConstraint.Create(
						_menuTransformScaleFactor, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						menuTransformScaleFactor, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						_menuTransformScaleFactor, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						_menuTransformScaleFactor, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						_menuTransformScaleFactor, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 29f),

                  	NSLayoutConstraint.Create(
						menuFadeStatusBar, NSLayoutAttribute.Top,
						NSLayoutRelation.Equal,
						_menuTransformScaleFactor, NSLayoutAttribute.Bottom,
						1f, padding),
					NSLayoutConstraint.Create(
						menuFadeStatusBar, NSLayoutAttribute.Leading,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Leading,
						1f, padding),
				  	NSLayoutConstraint.Create(
						menuFadeStatusBar, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				  	NSLayoutConstraint.Create(
						menuFadeStatusBar, NSLayoutAttribute.Height,
						NSLayoutRelation.Equal,
						null, NSLayoutAttribute.NoAttribute,
						1f, 22f),

					NSLayoutConstraint.Create(
						_menuFadeStatusBar, NSLayoutAttribute.CenterY,
						NSLayoutRelation.Equal,
						menuFadeStatusBar, NSLayoutAttribute.CenterY,
						1f, padding),
                  	NSLayoutConstraint.Create(
						_menuFadeStatusBar, NSLayoutAttribute.Trailing,
						NSLayoutRelation.Equal,
						this.View, NSLayoutAttribute.Trailing,
						1f, -padding),
				}
			);

			SetupSideMenu();
        }

		void SetupSideMenu()
		{
			_sideMenuManager.LeftNavigationController = new UISideMenuNavigationController(_sideMenuManager, new UITableViewController(), leftSide: true);
			_sideMenuManager.RightNavigationController = new UISideMenuNavigationController(_sideMenuManager, new UITableViewController(), leftSide: false);

			// Enable gestures. The left and/or right menus must be set up above for these to work.
			// Note that these continue to work on the Navigation Controller independent of the View Controller it displays!
			_sideMenuManager.AddPanGestureToPresent(toView: this.NavigationController?.NavigationBar);

			_sideMenuManager.AddScreenEdgePanGesturesToPresent(toView: this.NavigationController?.View);

			// Set up a cool background image for demo purposes
			_sideMenuManager.AnimationBackgroundColor = UIColor.FromPatternImage(UIImage.FromFile("stars.png"));
		}

//		void SetDefaults()
//		{
//			let modes:[SideMenuManager.MenuPresentMode] = [.MenuSlideIn, .ViewSlideOut, .MenuDissolveIn]
//		presentModeSegmentedControl.selectedSegmentIndex = modes.indexOf(SideMenuManager.menuPresentMode)!
        
//        let styles:[UIBlurEffectStyle] = [.Dark, .Light, .ExtraLight]
//        if let menuBlurEffectStyle = SideMenuManager.menuBlurEffectStyle {

//			blurSegmentControl.selectedSegmentIndex = styles.indexOf(menuBlurEffectStyle) ?? 0
//        } else {
//            blurSegmentControl.selectedSegmentIndex = 0
//        }

//darknessSlider.value = Float(SideMenuManager.menuAnimationFadeStrength)

//		shadowOpacitySlider.value = Float(SideMenuManager.menuShadowOpacity)

//		shrinkFactorSlider.value = Float(SideMenuManager.menuAnimationTransformScaleFactor)

//		screenWidthSlider.value = Float(SideMenuManager.menuWidth / view.frame.width)

//		blackOutStatusBar.on = SideMenuManager.menuFadeStatusBar
//    }

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