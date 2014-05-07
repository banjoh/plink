//Maps & Location namespaces
// Provides the GeoCoordinate class.
//Provides the Geocoordinate class.
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using System.Threading;
using App.ViewModels;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace App
{
    public partial class MainPage
    {
        private readonly MapLayer _myCurrentLocationLayer = new MapLayer();
        private readonly MapLayer _placesLayer = new MapLayer();
        private readonly MapLayer _headingLayer = new MapLayer();
        private MapRoute _mapRoute;
        private bool _signTapped = false;
        private bool _mapSymbolTapped = false;
        private PlacesViewModel _navItem = null;

        private readonly Point ORIGIN = new Point(0.4, 0.4);
        
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
            App.ViewModel.Places.CollectionChanged += Items_CollectionChanged;
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in e.NewItems)
            {
                var it = item as PlacesViewModel;
                if (it == null || it.Location == null) continue;
                AddPlaceToMap(it.Location.GeoCoordinate);
            }

            if (!App.ViewModel.DoneGeoCoding) return;

            // Update the route on map
            var coords = new List<GeoCoordinate> {App.ViewModel.MyLocation};
            foreach (var it in App.ViewModel.Places)
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
                Height = 20,
                Width = 20
            };
            
            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = App.ViewModel.MyLocation,
                PositionOrigin = ORIGIN
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

            /*if (e.PropertyName == "HeadingLocation")
            {
                App.Dispatch(() => AddHeadingLocToMap(App.ViewModel.HeadingLocation));
            }*/
        }

        private void AddHeadingLocToMap(GeoCoordinate coord)
        {
            // Create my location marker
            var locCircle = new Ellipse { Fill = new SolidColorBrush(Colors.Brown), Height = 10, Width = 10 };

            var overlay = new MapOverlay
            {
                Content = locCircle,
                GeoCoordinate = coord,
                PositionOrigin = ORIGIN
            };

            _placesLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15);
        }

        private void AddPlaceToMap(GeoCoordinate coord)
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

            _placesLayer.Add(overlay);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15);
        }

        private void UpdateRoute(IReadOnlyCollection<GeoCoordinate> locations)
        {
            if (locations.Count < 2) return;

            // Get the route for the new set
            var routeQuery = new RouteQuery
            {
                TravelMode = TravelMode.Walking,
                RouteOptimization = RouteOptimization.MinimizeDistance,
                Waypoints = locations
            };
            App.ViewModel.Progress = true;
            routeQuery.QueryCompleted += routeQuery_QueryCompleted;
            routeQuery.QueryAsync();
        }

        void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            App.ViewModel.Progress = false;
            if (e.Error != null) return;

            // Remove old route
            if (_mapRoute != null)
            {
                MapControl.RemoveRoute(_mapRoute);
            }

            // Update UI route
            Route route = e.Result;
            App.Log("MainPage: Update route to Models");
            App.ViewModel.MyRoute = route;
            _mapRoute = new MapRoute(route);
            MapControl.AddRoute(_mapRoute);

            PivotControl.SelectedItem = Map;
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PlacesDetails placesDetails = e.Content as PlacesDetails;
            if (placesDetails != null)
            {
               placesDetails.Item = _navItem;
            }
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            DebugBox.Visibility = DebugBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void ShoeImage_OnTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender != ShoeImage) return;

            App.Log("Tapped image");
            App.ViewModel.Progress = true;
            await App.ShoeModel.ConnectToShoes();
            App.ViewModel.Progress = false;
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
            }
        }

        private void MapSymbol_OnTap(object sender, GestureEventArgs e)
        {
            App.Log("MapSymbol tapped");
            _mapSymbolTapped = true;
        }

        private void PlusMinusSign_OnTap(object sender, GestureEventArgs e)
        {
            App.Log("PlusMinusSign tapped");
            _signTapped = true;
        }

        private void LongListSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector lls = sender as LongListSelector;
            if (sender != PlacesLLS || lls == null) return;
            App.Log("LongListSelector_OnSelectionChanged");
                
            
            PlacesViewModel item = lls.SelectedItem as PlacesViewModel;
            if (item == null) return;

            if (_signTapped)
            {
                item.VisitStatusProperty = item.VisitStatusProperty == PlacesViewModel.VisitStatus.Yes
                    ? PlacesViewModel.VisitStatus.No
                    : PlacesViewModel.VisitStatus.Yes;
                _signTapped = false;
            }
            else if (_mapSymbolTapped)
            {
                // Navigate to the selected map location
            }
            else
            {
                _navItem = item;
                // Navigate to the details page by default
                NavigationService.Navigate(new Uri("/PlacesDetails.xaml", UriKind.Relative));
            }

            // A little hack to get the LLS to fire SelectionChange after clicking the same list item
            lls.SelectedItem = null;
        }

        private void GoImage_OnTap(object sender, GestureEventArgs e)
        {
            if (sender != GoImage) return;

            MessageBox.Show("Please lock your phone and put it in your pocket then LOOK UP!", "Ready to GO?", MessageBoxButton.OK);
            // TODO: Put the app in the background
        }

        private void SearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender != SearchBox || e == null || e.Key != System.Windows.Input.Key.Enter) return;

            if (App.ViewModel.SearchAddress(SearchBox.Text))
            {
                // Give this page focus to hide keyboard.
                this.Focus();
            }
        }

        private void StackPanel_Tap(object sender, GestureEventArgs e)
        {
            SearchViewModel selectedItem = SearchLLS.SelectedItem as SearchViewModel;

            if (selectedItem == null || selectedItem.Location == null) return;

            // Update the route on map
            var coords = new List<GeoCoordinate> { App.ViewModel.MyLocation };
            coords.Add(selectedItem.Location.GeoCoordinate);

            _placesLayer.Clear();
            AddPlaceToMap(selectedItem.Location.GeoCoordinate);
            UpdateRoute(coords);
        }
    }
}