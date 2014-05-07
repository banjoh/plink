using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
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
            Places = new ObservableCollection<PlacesViewModel>();
            Searches = new ObservableCollection<SearchViewModel>();
            Progress = false;
        }

        public ObservableCollection<PlacesViewModel> Places { get; private set; }
        public ObservableCollection<SearchViewModel> Searches { get; private set; }
        
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

        private ShoeModel.Status _shoeLogo;
        public ShoeModel.Status ShoeConnectionStatus
        {
            get
            {
                return _shoeLogo;
            }
            set
            {
                _shoeLogo = value;
                NotifyPropertyChanged("ShoeConnectionStatus");
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

        private bool _progress;
        public bool Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                NotifyPropertyChanged("Progress");
            }
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData()
        {
            Debug.WriteLine("Loading data");
            Progress = true;
            ShoeConnectionStatus = ShoeModel.Status.Disconnected;
            Debug.WriteLine("Connecting shoe");
            Task connecTask = App.ShoeModel.ConnectToShoes();
            try
            {
                // Get the current position
                Debug.WriteLine("Getting position");
                Geoposition myGeoposition = await App.GeoLoc.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                Debug.WriteLine("Done getting pos");
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
                var query = new GeocodeQuery { SearchTerm = p, MaxResultCount = 1, GeoCoordinate = MyLocation };
                query.QueryCompleted += places_Query_QueryCompleted;
                query.QueryAsync();
            }

            // Wait for the connection task
            Debug.WriteLine("Waiting for shoe");
            await connecTask;
            Debug.WriteLine("DONE waiting for shoe");
            
            Debug.WriteLine("Data loaded");
            IsDataLoaded = true;
            Progress = false;
        }

        public bool SearchAddress(string term)
        {
            if (String.IsNullOrWhiteSpace(term)) return false;

            Progress = true;
            var searchQuery = new GeocodeQuery { SearchTerm = term, GeoCoordinate = MyLocation };
            searchQuery.QueryCompleted += searchQuery_QueryCompleted;
            
            Searches.Clear();
            Searches.Add(new SearchViewModel()
            {
                LineOne = "Searching..."
            });

            searchQuery.QueryAsync();

            return true;
        }

        void searchQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result.Count <= 0)
            {
                App.Dispatch(() =>
                {
                    Searches.Clear();
                    Searches.Add(new SearchViewModel()
                    {
                        LineOne = "No search results"
                    });
                });
            }
            else
            {
                App.Dispatch(() =>
                {
                    Searches.Clear();
                    foreach (MapLocation loc in e.Result)
                    {
                        Searches.Add(new SearchViewModel()
                        {
                            LineOne = String.IsNullOrWhiteSpace(loc.Information.Address.Street) ? loc.Information.Address.City : loc.Information.Address.Street +
                                (String.IsNullOrWhiteSpace(loc.Information.Address.HouseNumber) ? "" : " " + loc.Information.Address.HouseNumber),
                            LineTwo = loc.Information.Address.City,
                            Location = loc
                        });
                    }
                });
            }

            App.Dispatch(() => Progress = false );
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

        void places_Query_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            _placesNotGeoCoded--;
            if (e.Result.Count <= 0) return;
            var loc = e.Result[0];
            App.Dispatch(() => Places.Add(new PlacesViewModel()
            {
                LineOne = loc.Information.Address.Street,
                LineTwo = loc.Information.Address.City,
                Location = loc,
                VisitStatusProperty = PlacesViewModel.VisitStatus.No,
                ThumbNail = @"Assets\white_church_thumb.jpg",
                LargeImage = @"Assets\white_church.jpg"
            }));
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