using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using App.Resources;
using System.Windows.Shapes;
using System.Windows.Media;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Windows.Devices.Geolocation; //Provides the Geocoordinate class.
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;

using App.ViewModels;

namespace App
{
    public partial class MainPage : PhoneApplicationPage
    {
        private readonly MapLayer myCurrentLocationLayer = new MapLayer();
        private readonly MapLayer placesLayer = new MapLayer();
        private MapRoute mapRoute = null;
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            MapControl.Layers.Clear();

            MapControl.Layers.Add(myCurrentLocationLayer);
            MapControl.Layers.Add(placesLayer);

            // Listen to view model changes
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            App.ViewModel.Items.CollectionChanged += Items_CollectionChanged;
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (object item in e.NewItems)
            {
                ItemViewModel it = (ItemViewModel)item;
                AddPlaceToMap(it.Location.GeoCoordinate);
            }

            // Update the route on map
            List<GeoCoordinate> coords = new List<GeoCoordinate>();
            coords.Add(App.ViewModel.MyLocation);
            foreach (ItemViewModel it in App.ViewModel.Items)
            {
                coords.Add(it.Location.GeoCoordinate);
            }

            UpdateRoute(coords);
        }

        private void UpdateMyCurrectLocation()
        {
            // Create my location marker
            Ellipse locCircle = new Ellipse();
            locCircle.Fill = new SolidColorBrush(Colors.Green);
            locCircle.Height = 20;
            locCircle.Width = 20;
            locCircle.Opacity = 50;

            MapOverlay overlay = new MapOverlay()
            {
                Content = locCircle,
                GeoCoordinate = App.ViewModel.MyLocation,
                PositionOrigin = new Point(0, 0)
            };

            myCurrentLocationLayer.Clear();
            myCurrentLocationLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15, MapAnimationKind.Parabolic);
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MyLocation")
            {
                UpdateMyCurrectLocation();
            }
        }

        private void AddPlaceToMap(GeoCoordinate coord)
        {
            // Create my location marker
            Ellipse locCircle = new Ellipse();
            locCircle.Fill = new SolidColorBrush(Colors.Red);
            locCircle.Height = 20;
            locCircle.Width = 20;
            locCircle.Opacity = 50;

            MapOverlay overlay = new MapOverlay()
            {
                Content = locCircle,
                GeoCoordinate = coord,
                PositionOrigin = new Point(0, 0)
            };

            placesLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15);
        }

        private void UpdateRoute(List<GeoCoordinate> route)
        {
            // Cancel previous query
            RouteQuery routeQuery = new RouteQuery();

            // Get the route for the new set
            routeQuery.TravelMode = TravelMode.Walking;
            routeQuery.RouteOptimization = RouteOptimization.MinimizeDistance;
            routeQuery.Waypoints = route;
            routeQuery.QueryCompleted += routeQuery_QueryCompleted;
            routeQuery.QueryAsync();
        }

        void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            if (e.Error == null)
            {
                // Remove old route
                if (mapRoute != null)
                {
                    MapControl.RemoveRoute(mapRoute);
                }

                // Update UI route
                Route route = e.Result;
                Debug.WriteLine("MainPage: Update route to Models");
                App.ViewModel.MyRoute = route;
                mapRoute = new MapRoute(route);
                MapControl.AddRoute(mapRoute);
            }
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            DebugBox.Visibility = DebugBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}