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
        private GeoCoordinate _headingLoc = new GeoCoordinate(0, 0);
        private int _placesNotGeoCoded = 0;

        public bool DoneGeoCoding
        {
            get
            {
                return _placesNotGeoCoded <= 0;
            }
        }

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

        public GeoCoordinate HeadingLocation
        {
            get
            {
                return _headingLoc;
            }
            set
            {
                if (value == _headingLoc) return;
                _headingLoc = value ?? new GeoCoordinate(0, 0);
                NotifyPropertyChanged("HeadingLocation");
            }
        }

        private string _shoeLogo;
        public string ShoeConnectionStatus
        {
            get
            {
                return _shoeLogo;
            }
            set
            {
                ShoeModel.Status s;
                if (Enum.TryParse<ShoeModel.Status>(value, out s))
                {
                    switch (s)
                    {
                        case ShoeModel.Status.BothConnected:
                            _shoeLogo = "Resources\\shoes_connected.png";
                            break;
                        case ShoeModel.Status.LeftConnected:
                            _shoeLogo = "Resources\\shoes_left_red.png";
                            break;
                        case ShoeModel.Status.RightConnected:
                            _shoeLogo = "Resources\\shoes_right_red.png";
                            break;
                        default:
                            _shoeLogo = "Resources\\shoes_notconnected.png";
                            break;
                    }
                    NotifyPropertyChanged("ShoeConnectionStatus");
                }
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
                _myRoute = value ?? default(Route);
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
                foreach (string s in _logs)
                {
                    b.AppendLine(s);
                }

                return b.ToString();
            }
            set
            {
                _logs.Enqueue(value);
                if (_logs.Count >= 8)
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
            Debug.WriteLine("Loading data");
            App.IndicatingProgress = true;
            ShoeConnectionStatus = ShoeModel.Status.Disconnected.ToString();
            try
            {
                // Get the current position
                Geoposition myGeoposition = await App.GeoLoc.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
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

                /*"Kyhhkysmäki 2, 02650",
                "Lintuvaarantie 30, 02650",
                "Sepelkyyhkyntie 2, 02650"*/
            };

            _placesNotGeoCoded = places.Count;
            
            foreach (var p in places)
            {
                var query = new GeocodeQuery {SearchTerm = p, MaxResultCount = 1, GeoCoordinate = MyLocation};
                query.QueryCompleted += query_QueryCompleted;
                query.QueryAsync();
            }

            //this.Items.Add(new ItemViewModel() { LineOne = "runtime two", LineTwo = "Dictumst eleifend facilisi faucibus", LineThree = "Suscipit torquent ultrices vehicula volutpat maecenas praesent accumsan bibendum dictumst eleifend facilisi faucibus" });
            //this.Items.Add(new ItemViewModel() { LineOne = "runtime two", LineTwo = "Dictumst eleifend facilisi faucibus", LineThree = "Suscipit torquent ultrices vehicula volutpat maecenas praesent accumsan bibendum dictumst eleifend facilisi faucibus" });
            //this.Items.Add(new ItemViewModel() { LineOne = "runtime two", LineTwo = "Dictumst eleifend facilisi faucibus", LineThree = "Suscipit torquent ultrices vehicula volutpat maecenas praesent accumsan bibendum dictumst eleifend facilisi faucibus" });
            
            Debug.WriteLine("Data loaded");
            App.ShoeModel.ConnectToShoes();
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
            if (App.GeoLoc != sender || App.RunningInBackground) return;
            
            // We are in the UI
            App.Dispatch(() =>
            {
                MyLocation = args.Position.Coordinate.ToGeoCoordinate();
                //Log = "Loc change @" + DateTime.Now.ToShortTimeString() + " to " + args.Position.Coordinate.ToGeoCoordinate();
            });                
            Debug.WriteLine("GeoLoc changed: {0}", args.Position.Coordinate.ToGeoCoordinate());
        }

        void query_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            _placesNotGeoCoded--;
            if (e.Result.Count <= 0) return;
            var loc = e.Result[0];
            this.Items.Add(new ItemViewModel() { LineOne = loc.Information.Address.Street, LineTwo = loc.Information.Address.City, Location = loc });
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