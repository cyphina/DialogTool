﻿using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Shapes;

namespace UPDialogTool
{
	/// <summary>
	/// View logic for UPNode.xaml
	/// </summary>
	public partial class UPNode : UPNodeBase
	{						 
		private static Brush selectedColor = new LinearGradientBrush(Color.FromRgb(66, 244, 217),
			Color.FromRgb(50, 244, 191), new Point(0, 0), new Point(1, 1));
		private static Brush unSelectedColor = new LinearGradientBrush(Color.FromRgb(0, 0, 0), Color.FromRgb(128, 61, 135),
			new Point(0.5, 0), new Point(0.5, 1));

		private static BitmapImage hoveredImage = new BitmapImage(
			new Uri("pack://application:,,,/UPDialogTool;component/Images/hijirichristmasTransB.png"));	
		private static BitmapImage idleImage = 	new BitmapImage(
			new Uri("pack://application:,,,/UPDialogTool;component/Images/hijirichristmastrans.png"));

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
			get { return txtDialog.Text; }
			set { txtDialog.Text = value; }
		}

		public override string Actor
		{
			get { return txtActor.Text; }	
			set { txtActor.Text = value; }
		}

		public UPNode()
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

		public override NodeType GetNodeType() { return NodeType.Dialog; }
	}
}

