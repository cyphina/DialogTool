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
	/// Interaction logic for UPNodeConnector.xaml
	/// </summary>

	public partial class UPNodeConnector : UserControl
	{
		private bool isSelected; //Is line selected so it can be moved or redragged
		public Vector savedStartPosition = new Vector(); //Saved position of line so offset can be applied to it
		public Vector savedEndPosition = new Vector(); //Saved position of line so offset can be applied to it

		public UPNode fromNodeRef;
		public UPNode toNodeRef;

		private static Brush selectedColor = new LinearGradientBrush(Color.FromRgb(66, 244, 217),
			Color.FromRgb(50, 244, 191), new Point(0, 0), new Point(1, 1));
		private static Brush unSelectedColor = new LinearGradientBrush(Color.FromRgb(37, 99, 118), Color.FromRgb(47, 166, 58),
			new Point(0.5, 0), new Point(0.5, 1));

		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				isSelected = value;
				if (value)
					lnNodeConnector.Stroke = selectedColor;
				else
				{
					lnNodeConnector.Stroke = unSelectedColor;
				}
			}
		}

		public void Clear()
		{
			if (fromNodeRef != null)
			{
				fromNodeRef.fromLines.Remove(this);
			}

			if (toNodeRef != null)
			{
				toNodeRef.toLines.Remove(this);
			}

			(Parent as Canvas).Children.Remove(this);
			(Application.Current.MainWindow as MainWindow).edgeList.Remove(this);
		}

		private void lnNodeConnector_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.X))
			{
				Clear();
			}
		}

		public UPNodeConnector()
		{
			InitializeComponent();
		}
	}
}
