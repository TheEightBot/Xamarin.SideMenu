using System;
using UIKit;

namespace Xamarin.SideMenu
{
	public class UITableViewVibrantCell : UITableViewCell
	{
		UIVisualEffectView _vibrancyView = new UIVisualEffectView();

		UIVisualEffectView _vibrancySelectedBackgroundView = new UIVisualEffectView();

		UIView _defaultSelectedBackgroundView;

		UIBlurEffect _blurEffect;

		public UIBlurEffect BlurEffect
		{
			get
			{
				return _blurEffect;
			}

			set
			{
				_blurEffect = value;
				LayoutSubviews();
			}
		}

		public UITableViewVibrantCell(IntPtr handle) : base(handle) {
			Initialize();
		}

		public UITableViewVibrantCell()
		{
			Initialize();
		}

		void Initialize() {
			_vibrancyView.Frame = this.Bounds;
			_vibrancyView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			foreach (var view in this.Subviews)
				_vibrancyView.ContentView.AddSubview(view);

			AddSubview(_vibrancyView);

			BlurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light);
			_vibrancySelectedBackgroundView.Effect = BlurEffect;
			_defaultSelectedBackgroundView = this.SelectedBackgroundView;
		}


		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// shouldn't be needed but backgroundColor is set to white on iPad:
			this.BackgroundColor = UIColor.Clear;

			if (!UIAccessibility.IsReduceTransparencyEnabled) {
				_vibrancyView.Effect = BlurEffect;

				if (this.SelectedBackgroundView != null && this.SelectedBackgroundView != _vibrancySelectedBackgroundView)
				{
					_vibrancySelectedBackgroundView.ContentView.AddSubview(this.SelectedBackgroundView);
					this.SelectedBackgroundView = _vibrancySelectedBackgroundView;
				}
				else {
					_vibrancyView.Effect = null;
					this.SelectedBackgroundView = _defaultSelectedBackgroundView;
				}
			}
		}
	}
}