using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.Windows.Shapes.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

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
				RaisePropertyChangedEvent("Title");
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
		public HashSet<UPNodeBase> nodeList = new HashSet<UPNodeBase>(); //list of nodes
		public HashSet<UPNodeBase> selectedNodes = new HashSet<UPNodeBase>(new NodeEqualityComparer()); //list of selected nodes
		public LinkedList<UPNodeConnector> edgeList = new LinkedList<UPNodeConnector>(); //list of lines 

		public UPNodeBase draggedUponNode; //records what node we are currently dragging a connector off of, aka which node we're trying create a link off of

		//Some reusable so they don't have to be recreted with new everytime we need them
		private Thickness selectionRectThickness = new Thickness();
		private Rect selectionRectBounds = new Rect();
		private Vector mouseDelta = new Vector();

		//Number of nodes ever created in the program.  Used to give each node a unique id.
		private int nodeNum = 0;

		private SaveLoadManager saveLoadManager;

		public MainWindow()
		{
			InitializeComponent();

			connector.Stroke = Brushes.Beige;
			connector.StrokeThickness = 5;
			connector.Data = connectorLine;
			canvMain.Children.Add(connector);

			root.Translate.X = canvMain.Width / 2;
			root.Translate.Y = canvMain.Height /2;
			nodeList.Add(root);

			saveLoadManager = new SaveLoadManager(canvMain);
		}

		/// <summary>
		/// A little lesson on zooming:
		/// To emulate a zoom, we scale at a certain location.  ScaleAt takes a scalar multiplier for each direction, and a location of the origin.  Scaling around
		/// a point means we first translate our point to the origin, scale it, and then translate the scaled object back.
		/// This can be represented with the equation (x', y') = ([x - a] * scale + a, [y - b] * scale + b)
		/// Points furthest away from the scaling point get pushed further away when scaling inwards, and vice versa for scaling outwards
		/// </summary>
		/// <param name="newValue"></param>
		private void ZoomViewBox(double deltaValue)
		{
			Point p = Mouse.GetPosition(UPBorder);
			ScaleTransform s = new ScaleTransform();
			s.CenterX =  p.X - UPBorder.ActualWidth / 2;
			s.CenterY =  p.Y - UPBorder.ActualHeight / 2;		

			s.ScaleX = deltaValue;
			s.ScaleY = deltaValue;

			//Use a transform group because exposing a scale property and specifying the scale directly 
			//causes a huge jump if we zoom in and then zoom out far away since in that case the center is so far apart and
			//the deltaValue used to be the zoomValue so it would move the opposite direciton since the 
			//small decrease/increase in value didn't compensate for the large gap
			//that is caused by the extremely large distance from the center being zoomed.
			TransformGroup t = new TransformGroup();
			t.Children.Add(canvMain.RenderTransform);
			t.Children.Add(s);

			canvMain.RenderTransform = t;
		}

		//Lerp between t for values of t between 0 and 1
		private double Lerp(double start, double finish, double t)
		{
			return (1 - t) * start + t * finish;
		}

		private void winMain_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				if (zoomValue < maxZoom)
				{
					zoomValue *= 1.1;
					ZoomViewBox(1.1);
				}
			}
			else
			{
				if (zoomValue > minZoom)
				{
					zoomValue *= .9;
					ZoomViewBox(0.9);
				}
			}
		}

		void CreateNode(NodeType nodeType, Point spawnPos)
		{
			UPNodeBase newNode = null;
			switch (nodeType)
			{
				case NodeType.Condition:
					newNode = new UPNodeCondition();
					break;
				case NodeType.Dialog:
					newNode = new UPNode();
					break;
				case NodeType.Trigger:
					newNode = new UPNodeTrigger();
					break;
				default: break;
			}

			newNode.Translate.X = GridSnap(spawnPos.X, 20);
			newNode.Translate.Y = GridSnap(spawnPos.Y, 20);
			newNode.nodeID = ++nodeNum;

			//AdornerLayer aLayer = AdornerLayer.GetAdornerLayer(node.gridNode);
			//DragAdorner dragAdorner = new DragAdorner(node.gridNode);
			//aLayer.Add(dragAdorner);

			canvMain.Children.Add(newNode);
			nodeList.Add(newNode);
		}

		private void canvMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			savedMousePosition = e.GetPosition(canvMain);
			txtTitle.Visibility = Visibility.Hidden;
			rectSelection.Visibility = Visibility.Visible;
			dragState = EDragState.ERectDrag;

			canvMain.Focus();

			foreach (UPNodeBase node in nodeList)
			{
				node.IsSelected = false;
			}
			root.IsSelected = false;

			if (Keyboard.IsKeyDown(Key.F))
			{
				CreateNode(NodeType.Dialog, e.GetPosition(canvMain));
			}
			else if (Keyboard.IsKeyDown(Key.C))
			{
				CreateNode(NodeType.Condition, e.GetPosition(canvMain));
			}
			else if (Keyboard.IsKeyDown(Key.T))
			{
				CreateNode(NodeType.Trigger, e.GetPosition(canvMain));
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

		private double GridSnap(double position, int snapValue)
		{
			return Math.Floor((position + snapValue / 2) / snapValue) * snapValue;
		}

		private void MousePan(MouseEventArgs e)
		{
			Point mousePosition = e.GetPosition(UPBorder);
			mouseDelta = mousePosition - savedMousePosition;

			translation.X = GridSnap(canvasPanInitialTransform.X + mouseDelta.X * (1/zoomValue), 20);
			translation.Y = GridSnap(canvasPanInitialTransform.Y + mouseDelta.Y * (1/zoomValue), 20);
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

			foreach (UPNodeBase node in nodeList)
			{
				if (selectionRectBounds.IntersectsWith(new Rect(node.RenderTransform.Value.OffsetX + node.Translate.X,
					node.RenderTransform.Value.OffsetY + node.Translate.Y, node.ActualWidth, node.ActualHeight)))
				{
					node.IsSelected = true;
				}
				else
				{
					node.IsSelected = false;
				}
			}
		}

		/**Function called when node is being moved*/
		private void NodeMove(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Vector delta = e.GetPosition(canvMain) - savedMousePosition;

				foreach (UPNodeBase node in selectedNodes)
				{
					node.Translate.X = GridSnap(node.clickTransform.X + delta.X, 20);
					node.Translate.Y = GridSnap(node.clickTransform.Y + delta.Y, 20);

					foreach (UPNodeConnector edge in node.fromLines)
					{
						edge.lnNodeConnector.X1 = GridSnap(edge.savedStartPosition.X + delta.X, 20);
						edge.lnNodeConnector.Y1 = GridSnap(edge.savedStartPosition.Y + delta.Y, 20);
					}

					foreach (UPNodeConnector edge in node.toLines)
					{
						edge.lnNodeConnector.X2 = GridSnap(edge.savedEndPosition.X + delta.X, 20);
						edge.lnNodeConnector.Y2 = GridSnap(edge.savedEndPosition.Y + delta.Y, 20);
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

		private void ResetZoom()
		{
			ZoomViewBox(1 / zoomValue);
			zoomValue = 1;
		}

		private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
		{
			ResetZoom();
		}

		private void Clear()
		{
			selectedNodes.UnionWith(nodeList);
			foreach (UPNodeBase node in selectedNodes)
				node.Delete();

			selectedNodes.Clear();
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			Clear();
		}

		/**BFS search to get the nodes connected to the root in a nice order for exporting*/
		private LinkedList<UPNodeBase> GetExportNodes()
		{
			UPNodeBase currentNode = root;
			LinkedList<UPNodeBase> exportingNodes = new LinkedList<UPNodeBase>();
			Queue<UPNodeBase> nodeQueue = new Queue<UPNodeBase>();
			HashSet<int> visitedNodeIDs = new HashSet<int>();

			nodeQueue.Enqueue(currentNode);

			while (nodeQueue.Count > 0)
			{
				currentNode = nodeQueue.Dequeue();
				exportingNodes.AddLast(currentNode);

				foreach (UPNodeConnector conn in currentNode.fromLines)
				{
					//Ensure if our graph has two links connecting to one node, it doesn't get visited twice
					if (!visitedNodeIDs.Contains(conn.toNodeRef.nodeID))
					{
						nodeQueue.Enqueue(conn.toNodeRef);
						visitedNodeIDs.Add(conn.toNodeRef.nodeID);
					}
				}
			}
			exportingNodes.RemoveFirst();
			return exportingNodes;
		}

		//TODO: Maybe save in a more compact format
		/**Function to save the graph data so it can be reloaded with this program.  Saves more dasta than export*/
		private void BtnSave_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveDialog = new SaveFileDialog();
			if (saveDialog.ShowDialog() == true)
			{
				JsonSerializer s = new JsonSerializer();
				using (StreamWriter sw = new StreamWriter(saveDialog.FileName))
				{
					using (JsonWriter writer = new JsonTextWriter(sw))
					{
						writer.Formatting = Formatting.Indented;

						writer.WriteStartObject();
						saveLoadManager.SaveNodes(nodeList, writer);
						saveLoadManager.SaveEdges(edgeList, writer);

						//Save zoom state
						saveLoadManager.WriteProperty(writer, "Zoom", zoomValue);

						//Save number of nodes created	
						saveLoadManager.WriteProperty(writer, "NodeNum", nodeNum);

						//Save Title
						saveLoadManager.WriteProperty(writer, "Title", txtTitle.Text);

						writer.WriteEndObject();
						MessageBox.Show("Save Successful");
					}
				}
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
									string propertyName = (string)reader.Value;
									switch (propertyName)
									{
										case "Nodes":
											reader.Read(); //array start			
											List<UPNodeBase> loadedNodes = saveLoadManager.LoadNodeData(reader);
											for (int i = 1; i < loadedNodes.Count; ++i)
											{
												nodeList.Add(loadedNodes[i]);
												canvMain.Children.Add(loadedNodes[i]);
											}

											if (loadedNodes.Count > 0)
											{
												canvMain.Children.Remove(root);
												root = loadedNodes[0] as RootNode;
												canvMain.Children.Add(root);
											}
											break;

										case "Edges":
											reader.Read();
											edgeList = saveLoadManager.LoadEdges(reader, nodeList);
											foreach (UPNodeConnector edge in edgeList)
											{
												Canvas.SetZIndex(edge, -1);
												canvMain.Children.Add(edge);
											}
											break;
										case "Zoom":
											reader.Read();
											//ResetZoom();
											//zoomValue = (double)reader.Value;
											//ZoomViewBox(zoomValue);
											break;
										case "NodeNum":
											reader.Read();
											nodeNum = (int)(long)reader.Value;
											break;
										case "Title":
											reader.Read();
											txtTitle.Text = (string)reader.Value;
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

		/**Function to export for use in Unbounded Perceptions (can be exported into UE4 datatable*/
		private void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			string title = lblTitle.Content.ToString();
			if (title != "")
			{
				SaveFileDialog saveDialog = new SaveFileDialog();
				if (saveDialog.ShowDialog() == true)
				{
					JsonSerializer s = new JsonSerializer();
					using (StreamWriter sw = new StreamWriter(saveDialog.FileName + ".json"))
					{
						using (JsonWriter writer = new JsonTextWriter(sw))
						{
							writer.Formatting = Formatting.Indented;
							int index = 0;
							int edgeIndex = root.fromLines.Count - 1;
							HashSet<int> visitedNodeIndices = new HashSet<int>();

							writer.WriteStartArray();
							foreach (UPNodeBase node in GetExportNodes())
							{
								writer.WriteStartObject();
								saveLoadManager.WriteProperty(writer, "Name", title + ++index);

								writer.WritePropertyName("nextDialogue");
								writer.WriteStartArray();
								foreach (UPNodeConnector nextNodeEdge in node.fromLines)
								{
									//Ensure double connected nodes don't increase the edge count
									if (!visitedNodeIndices.Contains(nextNodeEdge.toNodeRef.nodeID))
									{
										++edgeIndex;
										visitedNodeIndices.Add(nextNodeEdge.toNodeRef.nodeID);
									}

									writer.WriteValue(edgeIndex);
								}
								writer.WriteEndArray();

								//Save in UE4 FText format (Namespace, Key, Text)
								saveLoadManager.WriteProperty(writer, "text", string.Format(@"NSLOCTEXT(""RTSDialog"", ""{0}"", ""{1}"")",
									txtTitle.Text + index, node.DialogText));

								saveLoadManager.WriteProperty(writer, "actor", node.Actor);

								writer.WriteEndObject();

							}
							writer.WriteEndArray();

							MessageBox.Show("Export Successful");
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Please Add a Title to the Document");
			}
		}

		private void lblTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			txtTitle.Visibility = Visibility.Visible;
		}

		private void canvMain_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete:
					foreach (UPNodeBase node in selectedNodes)
					{
						node.Delete();
					}
					selectedNodes.Clear();
					break;
			}
		}

		private void txtTitle_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				txtTitle.Visibility = Visibility.Hidden;
			}
		}
	}
}
