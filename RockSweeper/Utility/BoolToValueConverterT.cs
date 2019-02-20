﻿using System;
using System.Windows.Data;
using System.Windows.Media;

namespace RockSweeper.Utility
{
    public class BoolToValueConverter<T> : IValueConverter
    {
        public T FalseValue { get; set; }

        public T TrueValue { get; set; }

        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            if (value == null)
            {
                return FalseValue;
            }
            else
            {
                return ( bool ) value ? TrueValue : FalseValue;
            }
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            return value != null ? value.Equals( TrueValue ) : false;
        }
    }

    public class BoolToBrushConverter : BoolToValueConverter<Brush> { }
}
