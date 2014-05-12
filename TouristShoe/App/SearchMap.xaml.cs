using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using App.ViewModels;
using System.Device.Location;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using App.Utils;

namespace App
{
    public partial class SearchMap : PhoneApplicationPage
    {
        private readonly MapLayer _myCurrentLocationLayer = new MapLayer();
        private readonly MapLayer _placesLayer = new MapLayer();
        private readonly MapLayer _headingLayer = new MapLayer();

        public SearchMap()
        {
            InitializeComponent();

            MapControl.Layers.Clear();

            MapControl.Layers.Add(_myCurrentLocationLayer);
            MapControl.Layers.Add(_placesLayer);
            MapControl.Layers.Add(_headingLayer);
        }

        public SearchViewModel Item { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DataContext = Item;

            MapUtil.AddPlaceToMap(Item.Location.GeoCoordinate, _placesLayer);
            MapUtil.UpdateMyCurrectLocation(_myCurrentLocationLayer);

            if (MapUtil.QueryRoute(new List<GeoCoordinate> { App.ViewModel.MyLocation, Item.Location.GeoCoordinate }, routeQuery_QueryCompleted))
                App.ViewModel.Progress = true;
        }

        void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            App.ViewModel.Progress = false;
            if (e.Error != null) return;

            // Update UI route
            Route route = e.Result;
            App.Log("SearchMap: Update route to Models");
            App.ViewModel.MyRoute = route;
            
            MapControl.AddRoute(new MapRoute(route));

            // TODO: Use rectangular area
            MapControl.SetView(App.ViewModel.MyLocation, 15, MapAnimationKind.Parabolic);
        }
    }
}