using System;
using Microsoft.Phone.Shell;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Windows.Devices.Geolocation; //Provides the Geocoordinate class.
using Microsoft.Phone.Maps.Services;

namespace App
{
    public class NavigateShoes
    {
        public Route Route { get; set; }

        public NavigateShoes()
        { 
            
        }

        public void ConnectToShoes()
        { 
            // Asynch connection to shoes

            // Error handling needed through notifications. Properties?
        }

        public void PositionChanged(GeoCoordinate coord)
        {
            // Implement logic that finds out if we are on course, or should turn

            ShellToast toast = new ShellToast();
            toast.Content = "Loc change @" + DateTime.Now.ToShortTimeString() + " to " + 
                coord.Longitude.ToString("0.0000") + ", " + coord.Latitude.ToString("0.0000");
            toast.Title = "Shoe";
            toast.Show();
        }
    }
}
