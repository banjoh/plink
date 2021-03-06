﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace App.Utils
{
    // Convert ShoeModel status values to strings pointing to Icon images
    public class ShoeStatusToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ShoeModel.Status s;
            try
            {
                s = (ShoeModel.Status) value;
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }

            switch (s)
            {
                case ShoeModel.Status.Connected:
                    return "Resources\\shoes_connected.png";
                case ShoeModel.Status.LeftConnected:
                    return "Resources\\shoes_left_red.png";
                case ShoeModel.Status.RightConnected:
                    return "Resources\\shoes_right_red.png";
                case ShoeModel.Status.Disconnected:
                    return "Resources\\shoes_notconnected.png";
                default:
                    return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
