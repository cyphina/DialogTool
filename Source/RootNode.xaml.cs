using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UPDialogTool
{
	/// <summary>
	/// Interaction logic for RootNode.xaml
	/// </summary>
	public partial class RootNode : UPNodeBase
	{
		private static BitmapImage hoveredImage = new BitmapImage(
			new Uri("pack://application:,,,/UPDialogTool;component/Images/sakuyacloseupt2.jpg"));
		private static BitmapImage idleImage = new BitmapImage(
			new Uri("pack://application:,,,/UPDialogTool;component/Images/sakuyacloseupt.jpg"));

		private static Brush selectedColor = new LinearGradientBrush(Color.FromRgb(8, 19, 200),
			Color.FromRgb(50, 244, 191), new Point(0, 0), new Point(1, 1));
		private static Brush unSelectedColor = new LinearGradientBrush(Color.FromRgb(0, 0, 0), Color.FromRgb(8, 19, 60),
			new Point(0.5, 0), new Point(0.5, 1));

		public RootNode()
		{
			InitializeComponent();
		}

		public override TranslateTransform Translate
		{
			get { return translation; }
			set { translation = value; }
		}
		public override Button BtnNodeDrag
		{
			get { return btnNodeDrag; }
		}
		public override Ellipse EllipseTail
		{
			get { return ellipseToPoint; }
		}

		public override bool IsSelected
		{
			set
			{
				base.IsSelected = value;
				if (value)
				{
					bdrNode.BorderBrush = selectedColor;
				}
				else
				{
					bdrNode.BorderBrush = unSelectedColor;				
				}
			}
		}
		public override string DialogText { get; set; }					
		public override string Actor { get; set; }

		private void OnMouseEnterNode(object sender, MouseEventArgs e)
		{
			ImageBrush image = new ImageBrush();
			image.ImageSource = hoveredImage;
			image.Stretch = Stretch.UniformToFill;
			bdrNode.Background = image;
		}

		private void OnMouseLeaveNode(object sender, MouseEventArgs e)
		{
			ImageBrush image = new ImageBrush();
			image.ImageSource = idleImage;
			image.Stretch = Stretch.UniformToFill;
			bdrNode.Background = image;
			Mouse.Capture(mainWindowRef.grdMain, CaptureMode.SubTree);
		}

		public override void Delete() {}

		public override NodeType GetNodeType() { return NodeType.Root; }
	}
}
