using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UPDialogTool
{
	[ValueConversion(typeof(int), typeof(int))]
	class InvertIntConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double original = (double)value;
			return -original;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double original = (double)value;
			return -original;
		}
	}
}