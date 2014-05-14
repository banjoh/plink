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
using App.Utils;
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
        private PlacesViewModel _navItem;
        private SearchViewModel _searchItem;

        private readonly Point ORIGIN = new Point(0.4, 0.4);
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

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
            if (!App.ViewModel.DoneGeoCoding) return;

            UpdateMap();

            foreach (var it in App.ViewModel.Places)
            {
                it.PropertyChanged -= PlaceViewModel_PropertyChanged;
                it.PropertyChanged += PlaceViewModel_PropertyChanged;
            }
        }

        void UpdateMap()
        {
            // Update the route on map
            var coords = new List<GeoCoordinate> { App.ViewModel.MyLocation };
            _placesLayer.Clear();
            foreach (var it in App.ViewModel.Places)
            {
                if (it.VisitStatusProperty == PlacesViewModel.VisitStatus.DontVisit || it.Location == null) continue;
                MapUtil.AddPlaceToMap(it.Location.GeoCoordinate, _placesLayer);
                coords.Add(it.Location.GeoCoordinate);
            }

            // Remove old route
            if (_mapRoute != null)
            {
                MapControl.RemoveRoute(_mapRoute);
                _mapRoute = null;
            }
            if (MapUtil.QueryRoute(coords, routeQuery_QueryCompleted)) App.ViewModel.Progress = true;
        }

        void PlaceViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VisitStatusProperty")
            {
                PlacesViewModel it = sender as PlacesViewModel;
                if (it == null) return;

                UpdateMap();
            }
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MyLocation")
            {
                MapUtil.UpdateMyCurrectLocation(_myCurrentLocationLayer);

                // TODO: Use rectangular area
                MapControl.SetView(App.ViewModel.MyLocation, 15, MapAnimationKind.Parabolic);
            }

            /*if (e.PropertyName == "HeadingLocation")
            {
                App.Dispatch(() => AddHeadingLocToMap(App.ViewModel.HeadingLocation));
                
                // TODO: Use rectangular area
                MapControl.SetView(App.ViewModel.MyLocation, 15);
            }*/
        }

        void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            App.ViewModel.Progress = false;
            if (e.Error != null) return;

            // Remove old route
            if (_mapRoute != null)
            {
                MapControl.RemoveRoute(_mapRoute);
                _mapRoute = null;
            }

            // Update UI route
            Route route = e.Result;
            App.Log("MainPage: Update route to Models");
            App.ViewModel.MyRoute = route;
            _mapRoute = new MapRoute(route);
            MapControl.AddRoute(_mapRoute);

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15, MapAnimationKind.Parabolic);
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }

            if (App.ViewModel.DoneGeoCoding) UpdateMap();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PlacesDetails placesDetails = e.Content as PlacesDetails;
            if (placesDetails != null)
            {
               placesDetails.DataContext = _navItem;
            }

            SearchMap searchMap = e.Content as SearchMap;
            if (searchMap != null)
            {
                searchMap.Item = _searchItem;
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

        private void Direction_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
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

        private void PlacesLLS_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector lls = sender as LongListSelector;
            if (sender != PlacesLLS || lls == null) return;
            App.Log("PlacesLLS_OnSelectionChanged");
                
            
            PlacesViewModel item = lls.SelectedItem as PlacesViewModel;
            if (item == null) return;

            if (_signTapped)
            {
                item.VisitStatusProperty = item.VisitStatusProperty == PlacesViewModel.VisitStatus.Visit
                    ? PlacesViewModel.VisitStatus.DontVisit
                    : PlacesViewModel.VisitStatus.Visit;
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

        private void SearchLLS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector lls = sender as LongListSelector;
            if (sender != SearchLLS || lls == null) return;
            App.Log("SearchLLS_OnSelectionChanged");

            SearchViewModel selectedItem = lls.SelectedItem as SearchViewModel;
            if (selectedItem == null || selectedItem.Location == null) return;

            // Open the search maps page
            _searchItem = selectedItem;
            NavigationService.Navigate(new Uri("/SearchMap.xaml", UriKind.Relative));

            // A little hack to get the LLS to fire SelectionChange after clicking the same list item
            lls.SelectedItem = null;
        }

        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
<<<<<<< HEAD
              var s = new ShoeModel {DIST = Convert.ToInt32(DistanceBox.Text)};

            throw new NotImplementedException();
=======
            NavigationService.Navigate(new Uri("/Config.xaml", UriKind.Relative));
>>>>>>> 103285b073515e4702c957123ba3e203aa518cf4
        }
    }
}