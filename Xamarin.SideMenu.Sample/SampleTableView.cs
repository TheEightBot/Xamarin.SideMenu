using System;
using UIKit;

namespace Xamarin.SideMenu.Sample
{
	public class SampleTableView : UITableViewController
	{
		public SampleTableView()
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			this.TableView.RegisterClassForCellReuse(typeof(UITableViewVibrantCell), "VibrantCell");
		}

		public override nint RowsInSection(UITableView tableView, nint section)
		{
			return 3;
		}

		public override UITableViewCell GetCell(UITableView tableView, Foundation.NSIndexPath indexPath)
		{
			var cell = tableView.DequeueReusableCell("VibrantCell");

			cell.TextLabel.Text = "Index " + indexPath.Row;

			return cell;
		}

		public override void RowSelected(UITableView tableView, Foundation.NSIndexPath indexPath)
		{
			tableView.DeselectRow(indexPath, true);

			var rnd = new Random(Guid.NewGuid().GetHashCode());

			var vc = new UIViewController() { };
			vc.View.BackgroundColor = UIColor.FromRGB(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));

			this.ShowViewController(vc, this);
		}
	}
}

