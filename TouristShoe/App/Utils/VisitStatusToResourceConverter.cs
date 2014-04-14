﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using App.ViewModels;

namespace App.Utils
{
    // Convert ShoeModel status values to strings pointing to Icon images
    public class VisitStatusToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ItemViewModel.VisitStatus s;
            try
            {
                s = (ItemViewModel.VisitStatus) value;
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }

            switch (s)
            {
                case ItemViewModel.VisitStatus.Yes:
                    return "Resources\\minus_green.png";
                case ItemViewModel.VisitStatus.No:
                    return "Resources\\plus_green.png";
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