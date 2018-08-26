using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace UPDialogTool
{
	class DragAdorner : Adorner
	{
		public DragAdorner(UIElement adornedElement) : base(adornedElement)
		{

		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			Rect adornedElementRect = new Rect(this.AdornedElement.DesiredSize);

			SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
			renderBrush.Opacity = 0.2;
			Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
			double renderRadius = 5.0;

			// Draw a circle at each corner.
			drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopLeft, renderRadius, renderRadius);
			drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopRight, renderRadius, renderRadius);
			drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomLeft, renderRadius, renderRadius);
			drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomRight, renderRadius, renderRadius);
		}
	}
}
