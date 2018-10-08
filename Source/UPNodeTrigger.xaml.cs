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
    /// Interaction logic for UPNodeTrigger.xaml
    /// </summary>
    public partial class UPNodeTrigger : UPNodeBase
    {
	    private static Brush selectedColor = new LinearGradientBrush(Color.FromRgb(61, 180, 120),
		    Color.FromRgb(20, 95, 77), new Point(0, 0), new Point(1, 1));
	    private static Brush unSelectedColor = new LinearGradientBrush(Color.FromRgb(0, 0, 0), Color.FromRgb(61, 135, 132),
		    new Point(0.5, 0), new Point(0.5, 1));

	    private static BitmapImage hoveredImage = new BitmapImage(
		    new Uri("pack://application:,,,/UPDialogTool;component/Images/kokorostarT2.png"));	
	    private static BitmapImage idleImage = 	new BitmapImage(
		    new Uri("pack://application:,,,/UPDialogTool;component/Images/kokorostarT.png"));

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
	    public override string DialogText
	    {
		    get { return cboxTriggerType.SelectedIndex + "\\n" + txtObjects.Text + "\\n" + txtValues.Text; }
		    set
		    {
			    string[] fields = value.Split(new [] {"\\n"}, StringSplitOptions.None);
			    cboxTriggerType.SelectedIndex = int.Parse(fields[0]);
			    txtObjects.Text = fields[1];
			    txtValues.Text = fields[2];
		    }
	    }

	    public override string Actor
	    {
		    get { return "Trigger"; }
		    set { }
	    }

	    public override NodeType GetNodeType()
	    {
		    return NodeType.Trigger;
	    }

        public UPNodeTrigger()
        {
            InitializeComponent();
        }

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
    }
}
