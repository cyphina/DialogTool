using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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

		private SaveLoadManager saveLoadManager = new SaveLoadManager();

		public MainWindow()
		{
			InitializeComponent();

			connector.Stroke = Brushes.Beige;
			connector.StrokeThickness = 5;
			connector.Data = connectorLine;
			canvMain.Children.Add(connector);

			nodeList.Add(root);
			root.Translate.X = 5000;
			root.Translate.Y = 5020;
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
			Point p = Mouse.GetPosition(UPBorder);
			scale.ScaleX = newValue;
			scale.ScaleY = newValue;
			scale.CenterX = p.X - UPBorder.ActualWidth / 2;
			scale.CenterY = p.Y - UPBorder.ActualHeight / 2;
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

			translation.X = GridSnap(canvasPanInitialTransform.X + mouseDelta.X, 20);
			translation.Y = GridSnap(canvasPanInitialTransform.Y + mouseDelta.Y, 20);
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

		private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
		{
			zoomValue = 1;
			ZoomViewBox(zoomValue);
		}

		private void Clear()
		{
			canvMain.Children.Clear();

			selectedNodes.UnionWith(nodeList);
			foreach(UPNodeBase node in selectedNodes)
				node.Delete();
			
			selectedNodes.Clear();

			//Readd the helper visuals to the canvas since we cleared them as well
			canvMain.Children.Add(connector);
			canvMain.Children.Add(rectSelection);
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			Clear();
			nodeList.Add(root);
			canvMain.Children.Add(root);
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
											foreach (UPNodeBase node in loadedNodes)
											{
												nodeList.Add(node);
												canvMain.Children.Add(node);
											}

											if (loadedNodes.Count > 0)
												root = loadedNodes[0] as RootNode;
											break;
										case "Edges":
											reader.Read();
											edgeList = saveLoadManager.LoadEdges(reader, nodeList);
											foreach (UPNodeConnector edge in edgeList)
											{
												canvMain.Children.Add(edge);
											}
											break;
										case "Zoom":
											reader.Read();
											zoomValue = (double)reader.Value;
											scale.ScaleX = zoomValue;
											scale.ScaleY = zoomValue;
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
