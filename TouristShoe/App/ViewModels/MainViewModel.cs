using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            Items = new ObservableCollection<ItemViewModel>();
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
                if (value == _myLoc) return;
                _myLoc = value ?? new GeoCoordinate(0,0);
                NotifyPropertyChanged("MyLocation");
            }
        }

        private Route _myRoute = default(Route);
        public Route MyRoute
        {
            get
            {
                return _myRoute;
            }
            set
            {
                if (value == _myRoute) return;
                if (value != null) _myRoute = value;
                NotifyPropertyChanged("MyRoute");
            }
        }

        private readonly Queue<string> _logs = new Queue<string>();
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string Log
        {
            get
            {
                var b = new StringBuilder();
                foreach (var s in _logs)
                {
                    b.AppendLine(s);
                }

                return b.ToString();
            }
            set
            {
                _logs.Enqueue(value);
                if (_logs.Count >= 4)
                {
                    _logs.Dequeue();
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
                var myGeoposition = await App.GeoLoc.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                MyLocation = myGeoposition.Coordinate.ToGeoCoordinate();

                // Listen to changing positions and status values
                App.GeoLoc.PositionChanged += GeoLoc_PositionChanged;
                App.GeoLoc.StatusChanged += GeoLoc_StatusChanged;
            }
            catch
            {
                // Couldn't get current location - location might be disabled in settings
                MessageBox.Show("Current location cannot be obtained. Check that location service is turned on in phone settings then restart the application.");
            }

            // Simulate loading stored locations
            var places = new List<string>
            {
                "Otaniementie 9, 02150",
                "Betongblandargränden 5, 02150",
                "Otakaari 1, 02150",
                "Keilaranta 17, 02150",
                "Teknikvägen 1, 02150"
            };

            foreach (var query in places.Select(p => new GeocodeQuery {SearchTerm = p, MaxResultCount = 1, GeoCoordinate = MyLocation}))
            {
                query.QueryCompleted += query_QueryCompleted;
                query.QueryAsync();
            }

            Items.Add(new ItemViewModel { LineOne = "runtime two", LineTwo = "Dictumst eleifend facilisi faucibus", LineThree = "Suscipit torquent ultrices vehicula volutpat maecenas praesent accumsan bibendum dictumst eleifend facilisi faucibus" });
            Items.Add(new ItemViewModel { LineOne = "runtime three", LineTwo = "Habitant inceptos interdum lobortis", LineThree = "Habitant inceptos interdum lobortis nascetur pharetra placerat pulvinar sagittis senectus sociosqu suscipit torquent" });
            Items.Add(new ItemViewModel { LineOne = "runtime four", LineTwo = "Nascetur pharetra placerat pulvinar", LineThree = "Ultrices vehicula volutpat maecenas praesent accumsan bibendum dictumst eleifend facilisi faucibus habitant inceptos" });
            
            Debug.WriteLine("Data loaded");
            IsDataLoaded = true;
            App.IndicatingProgress = false;
        }

        static void GeoLoc_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            if (App.GeoLoc == sender)
            {
                Debug.WriteLine("GeoLoc status: {0}", args.Status.ToString());
            }
        }

        void GeoLoc_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (App.GeoLoc != sender || App.RunningInBackground) return;
            // We are in the UI
            App.Dispatch(() =>
            {
                MyLocation = args.Position.Coordinate.ToGeoCoordinate();
                Log = "Loc change @" + DateTime.Now.ToShortTimeString() + " to " + args.Position.Coordinate.ToGeoCoordinate();
            });                
            Debug.WriteLine("GeoLoc changed: {0}", args.Position.Coordinate.ToGeoCoordinate());
        }

        void query_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result.Count <= 0) return;
            var loc = e.Result[0];
            Items.Add(new ItemViewModel { LineOne = loc.Information.Address.Street, LineTwo = loc.Information.Address.City, Location = loc });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            var handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}