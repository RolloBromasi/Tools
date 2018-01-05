﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace vrClusterManager.ValueConversion
{
#if true
	public class InverseBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}
	}
#else
	// https://stackoverflow.com/questions/1039636/how-to-bind-inverse-boolean-properties-in-wpf
	[ValueConversion(typeof(bool), typeof(bool))]
	public class InverseBooleanConverter : IValueConverter
	{
#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(bool))
				throw new InvalidOperationException("The target must be a boolean");

			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}

#endregion
	}
#endif
}