using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using Path = System.Windows.Shapes.Path;

namespace UPDialogTool
{
	public class MainWindowViewModel : ObservableObject
	{
		private string title = "";

		public string Title
		{
			get => title;
			set
			{
				title = value;
				RaisePropertyChangedEvent("DialogName");
			}
		}
	}

	public enum EDragState
	{
		EBackgroundDrag,
		ENodeDrag,
		ERectDrag,
		EConnectorDrag,
		EDragStateNone
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public Point savedMousePosition; //Saves the position of the mouse before a drag so we can calculate drag delta
		private TranslateTransform canvasPanInitialTransform = new TranslateTransform(); //Saves position of canvas initially so we can add the delta to it to calculate how much it's dragged

		public EDragState dragState; //what kind of drag are we performing?

		public static double zoomValue = 1;
		private const double maxZoom = 2;
		private const double minZoom = 0.25;

		public Path connector = new Path(); //Line that appears when dragging off a node to show a possible connection that could be formed
		public LineGeometry connectorLine = new LineGeometry(); //Describes the line for the path above
		public Dictionary<uint, UPNode> nodeList = new Dictionary<uint, UPNode>(); //list of nodes
		public HashSet<UPNode> selectedNodes = new HashSet<UPNode>(new NodeEqualityComparer()); //list of selected nodes
		public LinkedList<UPNodeConnector> edgeList = new LinkedList<UPNodeConnector>(); //list of lines 

		public UPNode draggedUponNode; //records what node we are currently dragging a connector off of, aka which node we're trying create a link off of

		//Some reusable so they don't have to be recreted with new everytime we need them
		private Thickness selectionRectThickness = new Thickness();
		private Rect selectionRectBounds = new Rect();
		private Vector mouseDelta = new Vector();

		//Number of nodes ever created in the program.  Used to give each node a unique id.
		private uint nodeNum = 0;

		public MainWindow()
		{
			InitializeComponent();
			//TODO: Remove this it's just for testing
			foreach (UIElement elem in canvMain.Children)
			{
				if (elem.GetType() == typeof(UPNode))
					nodeList.Add(++nodeNum, (UPNode)elem);
			}

			connector.Stroke = Brushes.Beige;
			connector.StrokeThickness = 5;
			connector.Data = connectorLine;
			canvMain.Children.Add(connector);

		}

		/// <summary>
		/// A little lesson on zooming:
		/// To emulate a zoom, we scale at a certain location.  ScaleAt takes a scalar multiplier for each direction, and a location of the origin.  Scaling around
		/// a point means we first translate our point to the origin, scale it, and then translate the scaled object back.  When translating the scaled object back,
		/// the translation will also get scaled, so zooming out will essentially bring the object closer to the origin point since the offset is scaled down, and vice
		/// versa.
		/// </summary>
		/// <param name="newValue"></param>
		private void ZoomViewBox(double newValue)
		{
			//foreach (UPNode elem in nodeList)
			//{
			//	TransformGroup transforms = new TransformGroup();
			//	Point p = e.GetPosition(canvMain);
			//	Matrix m = elem.RenderTransform.Value;
			//	m.ScaleAt(newValue, newValue, p.X, p.Y);
			//	elem.RenderTransform = new MatrixTransform(m);
			//	elem.clickTransform = elem.RenderTransform;
			//}

			//foreach (Line line in lineList)
			//{
			//	TransformGroup transforms = new TransformGroup();
			//	Point p = e.GetPosition(canvMain);
			//	Matrix m = line.RenderTransform.Value;
			//	m.ScaleAt(newValue, newValue, p.X, p.Y);
			//	line.RenderTransform = new MatrixTransform(m);
			//}

			Point p = Mouse.GetPosition(UPBorder);
			scale.ScaleX = newValue;
			scale.ScaleY = newValue;
			scale.CenterX = p.X - UPBorder.ActualWidth / 2;
			scale.CenterY = p.Y - UPBorder.ActualHeight / 2;

			//scale.Value.ScaleAt(newValue, newValue, p.X - UPBorder.ActualWidth/2, p.Y - UPBorder.ActualHeight/2);
		}

		private void winMain_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				if (zoomValue < maxZoom)
				{
					zoomValue *= 1.25;
					ZoomViewBox(zoomValue);
				}
			}
			else
			{
				if (zoomValue > minZoom)
				{
					zoomValue *= 0.8;
					ZoomViewBox(zoomValue);
				}
			}
		}

		private void canvMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			savedMousePosition = e.GetPosition(canvMain);
			txtTitle.Visibility = Visibility.Hidden;
			rectSelection.Visibility = Visibility.Visible;
			dragState = EDragState.ERectDrag;

			canvMain.Focus();

			foreach (UPNode node in nodeList.Values)
			{
				node.IsSelected = false;
			}

			if (Keyboard.IsKeyDown(Key.F))
			{
				UPNode node = new UPNode();
				TransformGroup transforms = new TransformGroup();
				Point mousePos = e.GetPosition(canvMain);
				node.translation.X = mousePos.X;
				node.translation.Y = mousePos.Y;
				node.RenderTransform = transforms;
				node.nodeID = ++nodeNum;
				//AdornerLayer aLayer = AdornerLayer.GetAdornerLayer(node.gridNode);
				//DragAdorner dragAdorner = new DragAdorner(node.gridNode);
				//aLayer.Add(dragAdorner);
				canvMain.Children.Add(node);
				nodeList.Add(++nodeNum, node);
			}
		}

		private void canvMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			rectSelection.Width = 0;
			rectSelection.Height = 0;
			rectSelection.Visibility = Visibility.Collapsed;
			dragState = EDragState.EDragStateNone;
			connector.Visibility = Visibility.Collapsed;
		}

		private void canvMain_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			savedMousePosition = e.GetPosition(UPBorder);
			canvasPanInitialTransform.X = translation.X;
			canvasPanInitialTransform.Y = translation.Y;
			dragState = EDragState.EBackgroundDrag;
		}

		private void canvMain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			dragState = EDragState.EDragStateNone;
		}

		private void MousePan(MouseEventArgs e)
		{
			Point mousePosition = e.GetPosition(UPBorder);
			mouseDelta = mousePosition - savedMousePosition;

			translation.X = canvasPanInitialTransform.X + mouseDelta.X;
			translation.Y = canvasPanInitialTransform.Y + mouseDelta.Y;

			//foreach (UPNode elem in nodeList)
			//{
			//	TransformGroup transforms = new TransformGroup();
			//	transforms.Children.Add(elem.RenderTransform);
			//	transforms.Children.Add(translation);
			//	elem.RenderTransform = transforms;
			//}

			//foreach (Line line in lineList)
			//{
			//	TransformGroup transforms = new TransformGroup();
			//	transforms.Children.Add(line.RenderTransform);
			//	transforms.Children.Add(translation);
			//	line.RenderTransform = transforms;
			//}
		}

		private void RectSelect(MouseEventArgs e)
		{
			Point mousePosition = e.GetPosition(canvMain);

			if (mousePosition.X >= savedMousePosition.X)
			{
				mouseDelta.X = mousePosition.X - savedMousePosition.X;
				mousePosition.X = savedMousePosition.X;
			}
			else
			{
				mouseDelta.X = savedMousePosition.X - mousePosition.X;
			}

			if (mousePosition.Y >= savedMousePosition.Y)
			{
				mouseDelta.Y = mousePosition.Y - savedMousePosition.Y;
				mousePosition.Y = savedMousePosition.Y;
			}
			else
			{
				mouseDelta.Y = savedMousePosition.Y - mousePosition.Y;
			}

			selectionRectBounds.X = mousePosition.X;
			selectionRectBounds.Y = mousePosition.Y;
			selectionRectBounds.Width = mouseDelta.X;
			selectionRectBounds.Height = mouseDelta.Y;

			selectionRectThickness.Left = mousePosition.X;
			selectionRectThickness.Top = mousePosition.Y;
			selectionRectThickness.Right = mouseDelta.X;
			selectionRectThickness.Bottom = mouseDelta.Y;

			rectSelection.Width = mouseDelta.X;
			rectSelection.Height = mouseDelta.Y;
			rectSelection.Margin = selectionRectThickness;

			foreach (UPNode node in nodeList.Values)
			{
				if (selectionRectBounds.IntersectsWith(new Rect(node.RenderTransform.Value.OffsetX + node.translation.X,
					node.RenderTransform.Value.OffsetY + node.translation.Y, node.ActualWidth, node.ActualHeight)))
				{
					node.IsSelected = true;
				}
				else
				{
					node.IsSelected = false;
				}
			}
		}

		private void NodeMove(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Vector delta = e.GetPosition(canvMain) - savedMousePosition;

				foreach (UPNode node in selectedNodes)
				{
					node.translation.X = node.clickTransform.X + delta.X;
					node.translation.Y = node.clickTransform.Y + delta.Y;

					for (int i = 0; i < node.fromLines.Count; ++i)
					{
						node.fromLines[i].lnNodeConnector.X1 = node.fromLines[i].savedStartPosition.X + delta.X;
						node.fromLines[i].lnNodeConnector.Y1 = node.fromLines[i].savedStartPosition.Y + delta.Y;
					}

					for (int i = 0; i < node.toLines.Count; ++i)
					{
						node.toLines[i].lnNodeConnector.X2 = node.toLines[i].savedEndPosition.X + delta.X;
						node.toLines[i].lnNodeConnector.Y2 = node.toLines[i].savedEndPosition.Y + delta.Y;
					}
				}
			}
		}

		private void canvMain_MouseMove(object sender, MouseEventArgs e)
		{
			base.OnMouseMove(e);
			switch (dragState)
			{
				case EDragState.EBackgroundDrag:
					if (e.RightButton == MouseButtonState.Pressed)
					{
						MousePan(e);
					}

					break;
				case EDragState.ERectDrag:
					if (e.LeftButton == MouseButtonState.Pressed)
					{
						RectSelect(e);
					}

					break;
				case EDragState.EConnectorDrag:
					if (e.LeftButton == MouseButtonState.Pressed)
					{
						connectorLine.EndPoint = e.GetPosition(canvMain);
					}

					break;
				case EDragState.ENodeDrag:

					{
						NodeMove(e);
					}
					break;

			}

			e.Handled = true;
		}

		public bool IsRectDisplayed(MouseEventArgs e)
		{
			return rectSelection.Visibility == Visibility.Visible;
		}

		private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
		{
			zoomValue = 1;
			ZoomViewBox(zoomValue);
		}

		private void Clear()
		{
			canvMain.Children.Clear();
			nodeList.Clear();
			selectedNodes.Clear();
			edgeList.Clear();
			canvMain.Children.Add(connector);
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			Clear();
		}

		private void SaveNodeData(UPNode node, JsonTextWriter writer)
		{
			//Save node id
			writer.WriteValue(node.nodeID);

			//Save Dialog
			writer.WriteValue(node.txtDialog.Text);

			//Save Speaker Name
			writer.WriteValue(node.txtActor.Text);

			//Save Node Position
			writer.WriteValue(node.translation.X);
			writer.WriteValue(node.translation.Y);
		}

		private void SaveEdgeData(UPNodeConnector edge, JsonTextWriter writer)
		{
			writer.WriteValue(edge.toNodeRef.nodeID);
			writer.WriteValue(edge.fromNodeRef.nodeID);
		}

		//TODO: Maybe save in a more compact format
		/**Function to save the graph data so it can be reloaded with this program.  Saves more dasta than export*/
		private void BtnSave_Click(object sender, RoutedEventArgs e)
		{
			string title = lblTitle.Content.ToString();
			string path = "";
			if (title != "")
			{
				path = "Untitled";
			}

			path = title;

			JsonSerializer s = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(path))
			{
				using (JsonTextWriter writer = new JsonTextWriter(sw))
				{
					writer.WriteStartObject();
					writer.WritePropertyName("Nodes");
					writer.WriteStartArray();
					foreach (UPNode node in nodeList.Values)
					{
						SaveNodeData(node, writer);
					}
					writer.WriteEndArray();
					writer.WritePropertyName("Edges");
					writer.WriteStartArray();
					foreach (UPNodeConnector edge in edgeList)
					{
						SaveEdgeData(edge, writer);
					}
					writer.WriteEndArray();


					//Save zoom state
					writer.WritePropertyName("Zoom");
					writer.WriteValue(zoomValue);

					//Save number of nodes created
					writer.WritePropertyName("NodeNum");
					writer.WriteValue(nodeNum);
					writer.WriteEndObject();
					MessageBox.Show("Save Successful");
				}
			}
		}

		private void LoadNodeData(JsonTextReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				UPNode node = new UPNode();
				node.nodeID = (uint)(long)reader.Value;
				reader.Read();
				node.txtDialog.Text = (string)reader.Value;
				reader.Read();
				node.txtActor.Text = (string)reader.Value;
				reader.Read();
				node.translation.X = (double)reader.Value;
				reader.Read();
				node.translation.Y = (double)reader.Value;
				nodeList[node.nodeID] = node;
				canvMain.Children.Add(node);
			}
		}

		private void LoadEdges(JsonTextReader reader)
		{
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				UPNodeConnector edge = new UPNodeConnector();
				uint fromNodeID = (uint)(long)reader.Value;
				edge.fromNodeRef = nodeList[fromNodeID];
				edge.fromNodeRef.fromLines.Add(edge);
				reader.Read();
				uint toNodeID = (uint)(long)reader.Value;
				edge.toNodeRef = nodeList[toNodeID];
				edge.toNodeRef.toLines.Add(edge);
				edgeList.AddLast(edge);
				canvMain.Children.Add(edge);							
			}
		}

		private void BtnLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openDialog = new OpenFileDialog();
			if (openDialog.ShowDialog() == true)
			{
				using (StreamReader sr = new StreamReader(openDialog.FileName))
				{
					using (JsonTextReader reader = new JsonTextReader(sr))
					{
						if (reader != null)
						{
							Clear();
							while (reader.Read())
							{
								if (reader.TokenType == JsonToken.PropertyName)
								{
									string propertyName = (string) reader.Value;
									switch (propertyName)
									{
										case "Nodes":
											reader.Read(); //array start
											LoadNodeData(reader);
											break;
										case "Edges":
											reader.Read();
											LoadEdges(reader);
											break;
										case "Zoom":
											reader.Read();
											zoomValue = (double) reader.Value;
											scale.ScaleX = zoomValue;
											scale.ScaleY = zoomValue;
											break;
										case "NodeNum":
											reader.Read();
											nodeNum = (uint) (long) reader.Value;
											break;
									}
								}
							}

							MessageBox.Show("Load Successful");
						}
					}
				}
			}
		}

		/**Functin to export for use in Unbounded Perceptions (can be exported into UE4 datatable*/
		private void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			string title = lblTitle.Content.ToString();
			string path = "";
			if (title != "")
			{
				path = "Untitled";
			}

			path = title;

			JsonSerializer s = new JsonSerializer();
			using (StreamWriter sw = new StreamWriter(path))
			{
				using (JsonWriter writer = new JsonTextWriter(sw))
				{
					writer.WriteStartArray();
					foreach (UPNode node in nodeList.Values)
					{
						writer.WriteStartObject();

						writer.WritePropertyName("Name");
						writer.WriteValue(title + node.nodeID);

						writer.WritePropertyName("nextDialogue");
						writer.WriteStartArray();
						foreach (UPNodeConnector nextNodeEdge in node.fromLines)
						{
							writer.WriteValue(nextNodeEdge.toNodeRef.nodeID);
						}
						writer.WriteEndArray();

						writer.WritePropertyName("text");
						//Save in UE4 FText format
						writer.WriteValue(string.Format(@"NSLOCTEXT(""[RTSDialog]"", ""{0}"", ""{1}"")",
							title + node.nodeID, node.txtDialog.Text));

						writer.WritePropertyName("actor");
						writer.WriteValue(node.txtActor.Text);

						writer.WriteEndObject();
					}
					writer.WriteEndArray();

					MessageBox.Show("Export Successful");
				}
			}
		}

		private void lblTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			txtTitle.Visibility = Visibility.Visible;
		}


		private void txtTitle_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				txtTitle.Visibility = Visibility.Hidden;
		}

	}
}
