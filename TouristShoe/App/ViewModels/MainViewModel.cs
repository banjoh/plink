using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using App.Resources;
using System.Diagnostics;
using System.Windows;
using System.Text;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Windows.Devices.Geolocation; //Provides the Geocoordinate class.
using Microsoft.Phone.Maps.Services;

namespace App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<ItemViewModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> Items { get; private set; }

        private GeoCoordinate _myLoc = new GeoCoordinate(0, 0);
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public GeoCoordinate MyLocation
        {
            get
            {
                return _myLoc;
            }
            set
            {
                if (value != _myLoc)
                {
                    _myLoc = value == null ? new GeoCoordinate(0,0) : value;
                    NotifyPropertyChanged("MyLocation");
                }
            }
        }

        private Route myRoute = default(Route);
        public Route MyRoute
        {
            get
            {
                return myRoute;
            }
            set
            {
                if (value != myRoute)
                {
                    myRoute = value == null ? default(Route) : value;
                    NotifyPropertyChanged("MyRoute");
                }
            }
        }

        private Queue<string> logs = new Queue<string>();
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string Log
        {
            get
            {
                StringBuilder b = new StringBuilder();
                foreach (string s in logs)
                {
                    b.AppendLine(s);
                }

                return b.ToString();
            }
            set
            {
                logs.Enqueue(value);
                if (logs.Count >= 4)
                {
                    logs.Dequeue();
                }
                NotifyPropertyChanged("Log");
            }
        }

        /// <summary>
        /// Sample property that returns a localized string
        /// </summary>
        public string LocalizedSampleProperty
        {
            get
            {
                return AppResources.SampleProperty;
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData()
        {
            App.IndicatingProgress = true;
            try
            {
                // Get the current position
                Geoposition myGeoposition = await App.GeoLoc.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                MyLocation = myGeoposition.Coordinate.ToGeoCoordinate();

                // Listen to changing positions and status values
                App.GeoLoc.ReportInterval = 2500;
                App.GeoLoc.PositionChanged += GeoLoc_PositionChanged;
                App.GeoLoc.StatusChanged += GeoLoc_StatusChanged;
            }
            catch
            {
                // Couldn't get current location - location might be disabled in settings
                MessageBox.Show("Current location cannot be obtained. Check that location service is turned on in phone settings then restart the application.");
            }

            // Simulate loading stored locations
            List<string> places = new List<string>();
            places.Add("Otaniementie 9, 02150");
            places.Add("Betongblandargränden 5, 02150");
            places.Add("Otakaari 1, 02150");
            places.Add("Keilaranta 17, 02150");
            places.Add("Teknikvägen 1, 02150");

            foreach (string p in places)
            {
                GeocodeQuery query = new GeocodeQuery();
                query.SearchTerm = p;
                query.MaxResultCount = 1;
                query.GeoCoordinate = MyLocation;
                query.QueryCompleted += query_QueryCompleted;
                query.QueryAsync();
            }

            App.NavShoe.RouteGeometry = MyRoute.Geometry;

            Debug.WriteLine("Data loaded");
            this.IsDataLoaded = true;
            App.IndicatingProgress = false;
        }

        void GeoLoc_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            if (App.GeoLoc == sender)
            {
                Debug.WriteLine("GeoLoc status: {0}", args.Status.ToString());
            }
        }

        void GeoLoc_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (App.GeoLoc == sender) {

                if (!App.RunningInBackground)
                {
                    // We are in the UI
                    App.Dispatch(() =>
                    {
                        MyLocation = args.Position.Coordinate.ToGeoCoordinate();
                        Log = "Loc change @" + DateTime.Now.ToShortTimeString() + " to " + args.Position.Coordinate.ToGeoCoordinate();
                    });
                }
                else
                {
                    App.NavShoe.PositionChanged(args.Position.Coordinate.ToGeoCoordinate());
                }                
                Debug.WriteLine("GeoLoc changed: {0}", args.Position.Coordinate.ToGeoCoordinate());
            }
        }

        void query_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result.Count > 0)
            {
                MapLocation loc = e.Result[0];
                this.Items.Add(new ItemViewModel() { LineOne = loc.Information.Address.Street, LineTwo = loc.Information.Address.City, Location = loc });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}