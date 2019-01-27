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
	public enum NodeType{
	 	Root, 
		Dialog,
		Condition,
		Trigger
	}

	public class UPNodeViewModel : ObservableObject
	{
		private string dialogActor = "";
		private string dialog = "";

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

  class NodeEqualityComparer : IEqualityComparer<UPNodeBase>
    {
	    public bool Equals(UPNodeBase x, UPNodeBase y)
	    {
		    return x.nodeID.Equals(y.nodeID);
	    }

	    public int GetHashCode(UPNodeBase obj)
	    {
		    return (int)obj.nodeID;
	    }
    }

	/**The base class of all UPNodes.  Comes in three flavors: DialogNodes, ChoiceNodes, and TriggerNodes*/
    public abstract partial class UPNodeBase : UserControl
    {
	    public TranslateTransform clickTransform = new TranslateTransform(); //Saved transform before about to drag so an offset can be applied to it
	    private bool isSelected = false; //What nodes are selected (so they can be moved via dragging)
	    protected MainWindow mainWindowRef = null;

	    public int nodeID; //Unique id for each node  
	    public LinkedList<UPNodeConnector> fromLines = new LinkedList<UPNodeConnector>(); //Lines leading to the next dialog (come out from this node)
	    public LinkedList<UPNodeConnector> toLines = new LinkedList<UPNodeConnector>(); //Dialog lines which lead to this node (come to this node)	

		/**Any node classes must have a XML named attribute that exposes the render transform*/
	    public abstract TranslateTransform Translate { get; set;  } 
		public abstract Button BtnNodeDrag { get; }
		public abstract Ellipse EllipseTail { get; }
	    public abstract string DialogText { get; set; }
	    public abstract string Actor { get; set; }
	    public virtual bool IsSelected
	    {
		    get { return isSelected; }
		    set
		    {
			    if (value)
			    {
				    SetCachedTransformData();
				    mainWindowRef.selectedNodes.Add(this);
			    }
			    else
			    {			
				    mainWindowRef.selectedNodes.Remove(this);
			    }
			    isSelected = value;
		    }
	    }

	    /**Before dragging nodes or lines, saves the current position of the nodes/lines so they can be offset by the delta*/
	    private void SetCachedTransformData()
	    {
		    clickTransform.X = Translate.X;
		    clickTransform.Y = Translate.Y;

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
		    return BtnNodeDrag.TransformToAncestor(mainWindowRef.canvMain)
			    .Transform(new Point(BtnNodeDrag.ActualWidth / 2, BtnNodeDrag.ActualHeight / 2));
	    }

	    /**Gets location of the node's button that the edge will be dragged onto*/
	    public Point GetTailLoc()
	    {
		    return EllipseTail.TransformToAncestor(mainWindowRef.canvMain)
			    .Transform(new Point(BtnNodeDrag.ActualWidth / 2, BtnNodeDrag.ActualHeight / 2));
	    }

        public UPNodeBase()
        {
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

	    protected virtual void OnNodeMouseLMBDown(object sender, MouseButtonEventArgs e)
	    {
		    mainWindowRef.savedMousePosition = e.GetPosition(mainWindowRef.canvMain);
		    e.Handled = true;

		    //I've tried to optimize this by putting this in other callbacks, but every extraneous case is covered by saving the positional data before dragging on this event
		    foreach(UPNodeBase node in mainWindowRef.selectedNodes)
			    node.SetCachedTransformData();
	    }

	    protected virtual void OnNodeMouseLMBUp(object sender, MouseButtonEventArgs e)
	    {
		    //If we're not dragging anything, then we're either selecting a single node or deselecting something
		    if (mainWindowRef.dragState == EDragState.EDragStateNone)
		    {
			    //If we have multiple selected, we don't want them deselected
			    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
			    {
				    UPNodeBase[] nodes = mainWindowRef.selectedNodes.ToArray();
				    foreach (UPNodeBase node in nodes)
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

		    //If we're dragging an edge from this node, display it
		    if (mainWindowRef.dragState == EDragState.EConnectorDrag)
		    {
			    if (!mainWindowRef.draggedUponNode.Equals(this) && !toLines.Any(elem => elem.fromNodeRef == mainWindowRef.draggedUponNode))
			    {
				    UPNodeConnector line = new UPNodeConnector();
				    //line.IsHitTestVisible = false;

				    Point startPoint = mainWindowRef.draggedUponNode.GetBtnLoc();
				    line.lnNodeConnector.X1 = startPoint.X;
				    line.lnNodeConnector.Y1 = startPoint.Y;

				    Point endPoint = GetTailLoc();

				    line.lnNodeConnector.X2 = endPoint.X;
				    line.lnNodeConnector.Y2 = endPoint.Y;

				    line.lnNodeConnector.StrokeThickness = 10;
				    line.lnNodeConnector.StrokeStartLineCap = PenLineCap.Flat;
				    line.lnNodeConnector.StrokeEndLineCap = PenLineCap.Flat;

				    line.fromNodeRef = mainWindowRef.draggedUponNode;
				    line.toNodeRef = this;

				    mainWindowRef.canvMain.Children.Add(line);
					Canvas.SetZIndex(line,-1);

				    mainWindowRef.draggedUponNode.fromLines.AddLast(line);
				    toLines.AddLast(line);

				    mainWindowRef.edgeList.AddLast(line);

				    mainWindowRef.dragState = EDragState.EDragStateNone;
			    }
		    }
	    }

	    protected virtual void OnNodeMouseMove(object sender, MouseEventArgs e)
	    {
		    if (mainWindowRef.dragState == EDragState.EDragStateNone && e.LeftButton == MouseButtonState.Pressed)	  
		    {
			    if (!IsSelected)
			    {
				    //If we have multiple selected, we don't want them deselected
				    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
				    {
					    foreach (UPNodeBase node in mainWindowRef.nodeList)
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

		/**When the button is clicked*/
	    protected virtual void OnNodeBtnPreviewLMBDown(object sender, MouseButtonEventArgs e)
	    {
		    mainWindowRef.connectorLine.StartPoint = GetBtnLoc();
		    mainWindowRef.dragState = EDragState.EConnectorDrag;
		    mainWindowRef.draggedUponNode = this;
		    mainWindowRef.connector.Visibility = Visibility.Visible;
		    e.Handled = true;
	    }

		/**Helper function for deleting a node*/
	    public virtual void Delete()
	    {
		    while(toLines.Count > 0)
		    {
			    mainWindowRef.canvMain.Children.Remove(toLines.Last.Value);
				mainWindowRef.edgeList.Remove(toLines.Last.Value);
				toLines.Last.Value.fromNodeRef.fromLines.Remove(toLines.Last.Value);
			    toLines.RemoveLast();							
		    }

		    while(fromLines.Count > 0)
		    {
			    mainWindowRef.canvMain.Children.Remove(fromLines.Last.Value);
				mainWindowRef.edgeList.Remove(fromLines.Last.Value);
				fromLines.Last.Value.toNodeRef.toLines.Remove(fromLines.Last.Value);
			    fromLines.RemoveLast();
		    }

		    mainWindowRef.nodeList.Remove(this);
		    mainWindowRef.canvMain.Children.Remove(this);
	    }

	    public abstract NodeType GetNodeType();
    }
}
