//Maps & Location namespaces
// Provides the GeoCoordinate class.
//Provides the Geocoordinate class.
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using System.Threading;
using App.ViewModels;

namespace App
{
    public partial class MainPage
    {
        private readonly MapLayer _myCurrentLocationLayer = new MapLayer();
        private readonly MapLayer _placesLayer = new MapLayer();
        private readonly MapLayer _headingLayer = new MapLayer();
        private MapRoute _mapRoute;
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Thread.Sleep(1000);

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            MapControl.Layers.Clear();

            MapControl.Layers.Add(_myCurrentLocationLayer);
            MapControl.Layers.Add(_placesLayer);
            MapControl.Layers.Add(_headingLayer);

            // Listen to view model changes
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            App.ViewModel.Items.CollectionChanged += Items_CollectionChanged;
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in e.NewItems)
            {
                var it = (ItemViewModel)item;
                if (it.Location == null) continue;
                AddPlaceToMap(it.Location.GeoCoordinate);
            }

            if (!App.ViewModel.DoneGeoCoding) return;

            // Update the route on map
            var coords = new List<GeoCoordinate> {App.ViewModel.MyLocation};
            foreach (var it in App.ViewModel.Items)
            {
                if (it.Location == null) continue;
                coords.Add(it.Location.GeoCoordinate);
            }

            UpdateRoute(coords);
        }

        private void UpdateMyCurrectLocation()
        {
            // Create my location marker
            var locCircle = new Ellipse 
            {
                Fill = new SolidColorBrush(Colors.Green),
                Height = 10,
                Width = 10
            };
            
            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = App.ViewModel.MyLocation,
                PositionOrigin = new Point(0, 0)
            };

            _myCurrentLocationLayer.Clear();
            _myCurrentLocationLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15, MapAnimationKind.Parabolic);
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MyLocation")
            {
                UpdateMyCurrectLocation();
            }

            if (e.PropertyName == "HeadingLocation")
            {
                App.Dispatch(() => AddHeadingLocToMap(App.ViewModel.HeadingLocation));
            }
        }

        private void AddHeadingLocToMap(GeoCoordinate coord)
        {
            // Create my location marker
            var locCircle = new Ellipse { Fill = new SolidColorBrush(Colors.Brown), Height = 10, Width = 10 };

            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = coord,
                PositionOrigin = new Point(0, 0)
            };

            _placesLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15);
        }

        private void AddPlaceToMap(GeoCoordinate coord)
        {
            // Create my location marker
            var locCircle = new Ellipse { Fill = new SolidColorBrush(Colors.Red), Height = 10, Width = 10 };

            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = coord,
                PositionOrigin = new Point(0, 0)
            };

            _placesLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15);
        }

        private void UpdateRoute(IReadOnlyCollection<GeoCoordinate> places)
        {
            if (places.Count < 2) return;
            
            // Get the route for the new set
            var routeQuery = new RouteQuery
            {
                TravelMode = TravelMode.Walking,
                RouteOptimization = RouteOptimization.MinimizeDistance,
                Waypoints = places
            };
            routeQuery.QueryCompleted += routeQuery_QueryCompleted;
            routeQuery.QueryAsync();
        }

        void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            if (e.Error != null) return;
            // Remove old route
            if (_mapRoute != null)
            {
                MapControl.RemoveRoute(_mapRoute);
            }

            // Update UI route
            Route route = e.Result;
            Debug.WriteLine("MainPage: Update route to Models");
            App.ViewModel.MyRoute = route;
            _mapRoute = new MapRoute(route);
            MapControl.AddRoute(_mapRoute);
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

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/PlacesDetails.xaml", UriKind.Relative));
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.Log("Tapped image");
            App.ShoeModel.ConnectToShoes();
        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            switch (b.Content as string)
            { 
                case "Continue Straight":
                    App.ShoeModel.InstructShoes(RouteManeuverInstructionKind.GoStraight);
                    break;
                case "Go Left":
                    App.ShoeModel.InstructShoes(RouteManeuverInstructionKind.TurnLeft);
                    break;
                case "Go Right":
                    App.ShoeModel.InstructShoes(RouteManeuverInstructionKind.TurnRight);
                    break;
                case "Turn Back":
                    App.ShoeModel.InstructShoes(RouteManeuverInstructionKind.UTurnLeft);
                    break;
                default:
                    break;
            }
        }
    }
}