using System;
using System.Globalization;
using System.Windows.Data;

namespace App.Utils
{
    // Convert ShoeModel status values to strings pointing to Icon images
    public class ShoeStatusToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ShoeModel.Status s;
            if (!Enum.TryParse<ShoeModel.Status>((string) value, out s)) return null;
            switch (s)
            {
                case ShoeModel.Status.BothConnected:
                    return "Resources\\shoes_connected.png";
                case ShoeModel.Status.LeftConnected:
                    return "Resources\\shoes_left_red.png";
                case ShoeModel.Status.RightConnected:
                    return "Resources\\shoes_right_red.png";
                case ShoeModel.Status.BluetoothOff:
                    return "Resources\\shoes_notconnected.png";
                default:
                    return "Resources\\shoes_notconnected.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
