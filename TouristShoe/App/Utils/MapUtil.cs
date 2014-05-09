using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;

namespace App.Utils
{
    public static class MapUtil
    {
        private static readonly Point ORIGIN = new Point(0.4, 0.4);

        public static void AddPlaceToMap(GeoCoordinate coord, MapLayer layer)
        {
            // Create my location marker
            var locImage = new Image { Width = 30, Height = 35 };
            locImage.Source = new BitmapImage(new Uri(@"/Resources/map_symbol_green.png", UriKind.Relative));

            var overlay = new MapOverlay
            {
                Content = locImage,
                GeoCoordinate = coord,
                PositionOrigin = ORIGIN
            };

            layer.Add(overlay);
        }

        public static void AddHeadingLocToMap(GeoCoordinate coord, MapLayer layer)
        {
            // Create my location marker
            var locCircle = new Ellipse { Fill = new SolidColorBrush(Colors.Brown), Height = 10, Width = 10 };

            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = coord,
                PositionOrigin = ORIGIN
            };

            layer.Add(overlay);
        }

        public static bool QueryRoute(IReadOnlyCollection<GeoCoordinate> locations, EventHandler<QueryCompletedEventArgs<Route>> handler)
        {
            if (locations.Count < 2) return false;

            // Get the route for the new set
            var routeQuery = new RouteQuery
            {
                TravelMode = TravelMode.Walking,
                RouteOptimization = RouteOptimization.MinimizeDistance,
                Waypoints = locations
            };
            
            routeQuery.QueryCompleted += handler;
            routeQuery.QueryAsync();
            return true;
        }

        public static void UpdateMyCurrectLocation(MapLayer layer)
        {
            // Create my location marker
            var locCircle = new Ellipse
            {
                Fill = new SolidColorBrush(Colors.Green),
                Height = 20,
                Width = 20
            };

            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = App.ViewModel.MyLocation,
                PositionOrigin = ORIGIN
            };

            layer.Clear();
            layer.Add(overlay);
        }
    }
}
