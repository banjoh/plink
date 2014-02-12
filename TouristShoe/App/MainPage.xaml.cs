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
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            MapControl.Layers.Clear();

            MapControl.Layers.Add(myCurrentLocationLayer);
            MapControl.Layers.Add(placesLayer);

            // Get the current location of the device
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
            if (e.PropertyName == "MyLocationProperty")
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

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }
    }
}