using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Shapes;

namespace UPDialogTool
{
	public class UPNodeViewModel : ObservableObject
	{
		private string dialogName = "";
		private string dialogActor = "";
		private string dialog = "";

		public string DialogName
		{
			get => dialogName;
			set
			{
				dialogName = value;
				RaisePropertyChangedEvent("DialogName");
			}
		}

		public string DialogActor
		{
			get => dialogActor;
			set
			{
				dialogActor = value;
				RaisePropertyChangedEvent("DialogActor");
			}
		}

		public string Dialog
		{
			get => dialog;
			set
			{
				dialog = value;
				RaisePropertyChangedEvent("DialogActor");
			}
		}
	}

	class NodeEqualityComparer : IEqualityComparer<UPNode>
	{
		public bool Equals(UPNode x, UPNode y)
		{
			return x.nodeID.Equals(y.nodeID);
		}

		public int GetHashCode(UPNode obj)
		{
			return (int)obj.nodeID;
		}
	}

	/// <summary>
	/// View logic for UPNode.xaml
	/// </summary>
	public partial class UPNode : UserControl
	{
		public TranslateTransform clickTransform = new TranslateTransform(); //Saved transform before about to drag so an offset can be applied to it

		private bool isSelected = false; //What nodes are selected (so they can be moved via dragging)
		private MainWindow mainWindowRef = null;

		public uint nodeID; //Unique id for each node  
		public List<UPNodeConnector> fromLines = new List<UPNodeConnector>(); //Lines leading to the next dialog
		public List<UPNodeConnector> toLines = new List<UPNodeConnector>(); //Dialog lines which lead to this node										 

		private static Brush selectedColor = new LinearGradientBrush(Color.FromRgb(66, 244, 217),
			Color.FromRgb(50, 244, 191), new Point(0, 0), new Point(1, 1));
		private static Brush unSelectedColor = new LinearGradientBrush(Color.FromRgb(0, 0, 0), Color.FromRgb(128, 61, 135),
			new Point(0.5, 0), new Point(0.5, 1));


		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				if (value)
				{
					bdrNode.BorderBrush = selectedColor;
					SetCachedTransformData();
					mainWindowRef.selectedNodes.Add(this);
				}
				else
				{
					bdrNode.BorderBrush = unSelectedColor;				
					mainWindowRef.selectedNodes.Remove(this);
				}

				isSelected = value;
			}
		}

		/**Before dragging nodes or lines, saves the current position of the nodes/lines so they can be offset by the delta*/
		private void SetCachedTransformData()
		{
			clickTransform.X = translation.X;
			clickTransform.Y = translation.Y;

			foreach (UPNodeConnector line in fromLines)
			{
				line.savedStartPosition = new Vector(line.lnNodeConnector.X1,line.lnNodeConnector.Y1);
			}

			foreach (UPNodeConnector line in toLines)
			{
				line.savedEndPosition = new Vector(line.lnNodeConnector.X2,line.lnNodeConnector.Y2);
			}
		}

		/**Gets location of the node's button that the edge will be dragged off of*/
		public Point GetBtnLoc()
		{
			return btnNodeDrag.TransformToAncestor(mainWindowRef.canvMain)
				.Transform(new Point(btnNodeDrag.ActualWidth / 2, btnNodeDrag.ActualHeight / 2));
		}

		/**Gets location of the node's button that the edge will be dragged onto*/
		public Point GetTailLoc()
		{
			return ellipseToPoint.TransformToAncestor(mainWindowRef.canvMain)
				.Transform(new Point(btnNodeDrag.ActualWidth / 2, btnNodeDrag.ActualHeight / 2));
		}

		public UPNode()
		{
			InitializeComponent();	
			mainWindowRef = (MainWindow) Application.Current.MainWindow;
			//Need to make this a callback because transformToAncestor doesn't properly work until the object is ready for rendering
			this.Loaded += OnNodeLoaded;
		}

		private void OnNodeLoaded(object sender, RoutedEventArgs e)
		{
			foreach (UPNodeConnector edge in fromLines)
			{
				edge.lnNodeConnector.X1 = edge.fromNodeRef.GetBtnLoc().X;
				edge.lnNodeConnector.Y1 = edge.fromNodeRef.GetBtnLoc().Y;
			}

			foreach (UPNodeConnector edge in toLines)
			{
				edge.lnNodeConnector.X2 = edge.toNodeRef.GetTailLoc().X;
				edge.lnNodeConnector.Y2 = edge.toNodeRef.GetTailLoc().Y;
			}	
		}

		private void OnMouseEnterNode(object sender, MouseEventArgs e)
		{
			ImageBrush image = new ImageBrush();
			image.ImageSource =
				new BitmapImage(
					new Uri("pack://application:,,,/UPDialogTool;component/Images/hijirichristmastransB.jpg"));
			image.Stretch = Stretch.UniformToFill;
			bdrNode.Background = image;

		}

		private void OnMouseLeaveNode(object sender, MouseEventArgs e)
		{
			ImageBrush image = new ImageBrush();
			image.ImageSource =
				new BitmapImage(
					new Uri("pack://application:,,,/UPDialogTool;component/Images/hijirichristmastrans.jpg"));
			image.Stretch = Stretch.UniformToFill;
			bdrNode.Background = image;
			Mouse.Capture(mainWindowRef.grdMain, CaptureMode.SubTree);
		}

		private void OnNodeMouseLMBDown(object sender, MouseButtonEventArgs e)
		{
			mainWindowRef.savedMousePosition = e.GetPosition(mainWindowRef.canvMain);
			Mouse.Capture(gridNode, CaptureMode.SubTree);
			e.Handled = true;

			//I've tried to optimize this by putting this in other callbacks, but every extraneous case is covered by saving the positional data before dragging on this event
			foreach(UPNode node in mainWindowRef.selectedNodes)
				node.SetCachedTransformData();
		}

		private void OnNodeMouseLMBUp(object sender, MouseButtonEventArgs e)
		{
			if (mainWindowRef.dragState == EDragState.EDragStateNone)
			{
				//If we have multiple selected, we don't want them deselected
				if (!Keyboard.IsKeyDown(Key.LeftCtrl))
				{
					foreach (UPNode node in mainWindowRef.selectedNodes)
					{
						node.isSelected = false;
						node.bdrNode.BorderBrush = unSelectedColor;
					}
					mainWindowRef.selectedNodes.Clear();

					IsSelected = true;
				}
				else
				{
					IsSelected = !IsSelected;
				}
			}

			if (mainWindowRef.dragState == EDragState.EConnectorDrag)
			{
				if (!mainWindowRef.draggedUponNode.Equals(this))
				{
					UPNodeConnector line = new UPNodeConnector();
					line.IsHitTestVisible = false;

					Point startPoint = mainWindowRef.draggedUponNode.GetBtnLoc();
					line.lnNodeConnector.X1 = startPoint.X;
					line.lnNodeConnector.Y1 = startPoint.Y;

					Point endPoint = GetTailLoc();

					line.lnNodeConnector.X2 = endPoint.X;
					line.lnNodeConnector.Y2 = endPoint.Y;

					line.lnNodeConnector.StrokeThickness = 10;
					line.lnNodeConnector.StrokeStartLineCap = PenLineCap.Round;
					line.lnNodeConnector.StrokeEndLineCap = PenLineCap.Triangle;

					line.fromNodeRef = mainWindowRef.draggedUponNode;;
					line.toNodeRef = this;

					mainWindowRef.canvMain.Children.Add(line);

					mainWindowRef.draggedUponNode.fromLines.Add(line);
					toLines.Add(line);

					mainWindowRef.edgeList.AddLast(line);

					mainWindowRef.dragState = EDragState.EDragStateNone;
				}
			}
		}

		private void OnNodeMouseMove(object sender, MouseEventArgs e)
		{
			if (mainWindowRef.dragState == EDragState.EDragStateNone && e.LeftButton == MouseButtonState.Pressed)	  
			{
				if (!IsSelected)
				{
					//If we have multiple selected, we don't want them deselected
					if (!Keyboard.IsKeyDown(Key.LeftCtrl))
					{
						foreach (UPNode node in mainWindowRef.nodeList.Values)
						{
							node.IsSelected = false;
						}

						IsSelected = true;
					}
					else
					{
						IsSelected = !IsSelected;
					}
				}

				mainWindowRef.dragState = EDragState.ENodeDrag;
			}
		}

		private void OnNodeBtnPreviewLMBDown(object sender, MouseButtonEventArgs e)
		{
			mainWindowRef.connectorLine.StartPoint = GetBtnLoc();
			mainWindowRef.dragState = EDragState.EConnectorDrag;
			mainWindowRef.draggedUponNode = this;
			mainWindowRef.connector.Visibility = Visibility.Visible;
			e.Handled = true;
		}

		//private void gridNode_MouseMove(object sender, MouseEventArgs e)
		//{

		//		if (e.LeftButton == MouseButtonState.Pressed)
		//		{
		//			if (!IsSelected)
		//			{
		//				foreach (UPNode node in mainWindowRef.nodeList)
		//				{
		//					node.IsSelected = false;
		//				}

		//				IsSelected = true;
		//			}

		//			Vector delta = e.GetPosition(mainWindowRef.canvMain) - clickPoint;
		//			foreach (UPNode node in mainWindowRef.nodeList)
		//			{
		//				if (node.IsSelected)
		//				{
		//					TransformGroup transforms = new TransformGroup();
		//					transforms.Children.Add(node.RenderTransform);
		//					transforms.Children.Add(new TranslateTransform(delta.X, delta.Y));
		//					node.RenderTransform = transforms;

		//					foreach (Line line in node.fromLines)
		//					{
		//						line.X1 += delta.X;
		//						line.Y1 += delta.Y;
		//					}

		//					foreach (Line line in node.toLines)
		//					{
		//						transforms = new TransformGroup();
		//						line.X2 += delta.X;
		//						line.Y2 += delta.Y;
		//					}
		//				}

		//			}
		//		}
		//		Console.WriteLine(DateTime.Now.TimeOfDay);
		//		clickPoint = e.GetPosition(mainWindowRef.canvMain);
		//		e.Handled = true;
		//	}
		//}
	}
}

