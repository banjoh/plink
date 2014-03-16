// APIS in this class should be within the restriction of the
// ones defined here http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj662941%28v=vs.105%29.aspx

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Linq;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Windows.Devices.Geolocation; //Provides the Geocoordinate class.
using Microsoft.Phone.Maps.Services;

// Bluetooth
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;

namespace App
{
    public class ShoeModel
    {
        // Route geometry needed to generate directional commands sent to the
        // shoes.
        private Route _route = null;
        private Queue<RouteManeuver> _maneuvers = new Queue<RouteManeuver>();

        // Socket used to communicate with the shoes through bluetooth
		private readonly StreamSocket _socket = new StreamSocket();

        public ShoeModel()
        {
            App.GeoLoc.PositionChanged += GeoLoc_PositionChanged;
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MyRoute" && sender == App.ViewModel)
            {
                // Store a local instance of the the RouteGeometry passed from the UI
                //_routeGeometry = App.ViewModel.MyRoute.Geometry;
                _route = App.ViewModel.MyRoute;
                
                // Store all maneuvers in a Queue
                _maneuvers.Clear();
                foreach (RouteLeg leg in _route.Legs)
                {
                    foreach (RouteManeuver man in leg.Maneuvers)
                    {
                        _maneuvers.Enqueue(man);
                    }
                }

                Debug.WriteLine("ShoeModel: Route updated");
            }
        }

        public async void ConnectToShoes()
        {
            // Note: You can only browse and connect to paired devices!
            // Configure PeerFinder to search for all paired devices.
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();

            if (pairedDevices.Count == 0)
            {
                Debug.WriteLine("No paired devices were found.");
            }
            else
            {
                // Select a paired device. In this example, just pick the first one.
                PeerInformation selectedDevice = pairedDevices[0];

                foreach(PeerInformation p in pairedDevices)
                {
                    Debug.WriteLine(p.DisplayName);
                } 
                
                // Attempt a connection
              
                // Make sure ID_CAP_NETWORKING is enabled in your WMAppManifest.xml, or the next 
                // line will throw an Access Denied exception.
                // In this example, the second parameter of the call to ConnectAsync() is the RFCOMM port number, and can range 
                // in value from 1 to 30.
                try
                {
                    await _socket.ConnectAsync(selectedDevice.HostName, "1");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                Debug.WriteLine("Connected!!!");
            }
        }
		
        void GeoLoc_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (App.GeoLoc != sender) return;
            var coord = args.Position.Coordinate.ToGeoCoordinate();
            // Implement logic that finds out if we are on course, or should turn

            // We are in the UI
            Debug.WriteLine("GeoLoc changed ShoeModel: {0}", coord);

            CalculateNavigationInstruction(coord);
        }

        private void CalculateNavigationInstruction(GeoCoordinate coord)
        {
            if (_maneuvers.Count <= 0)
            {
                Debug.WriteLine("ShoeModel: RouteGeometry not set");
                return;
            }

            // Calculate instruction to send to shoe
            // http://stackoverflow.com/questions/8564428/check-if-user-is-near-route-checkpoint-with-gps
            RouteManeuver man = _maneuvers.First();
            double dist = coord.GetDistanceTo(man.StartGeoCoordinate);
            if (dist < 10)  // When distance b2n is below 10 meters instruct shoe
            {
                InstructShoes(man);
                if (dist < 5)   // Remove this maneuver, lets get the nex one
                {
                    _maneuvers.Dequeue();
                }
            }

            App.ViewModel.HeadingLocation = man.StartGeoCoordinate;

            // Log
            var s = "Dist: " + dist + ", direction = " + man.InstructionKind; 
            App.Log(s);
        }

        private void InstructShoes(RouteManeuver maneuver)
        {
            ShowToast("Now turn " + maneuver.InstructionKind);

            // Create the data writer object backed by the in-memory stream.
            /*try
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(_socket.OutputStream))
                {
                    // Parse the input stream and write each element separately.
                    dataWriter.WriteByte(1);

                    // Send the contents of the writer to the backing stream.
                    Debug.WriteLine(Convert.ToString(await dataWriter.StoreAsync()));

                    // For the in-memory stream implementation we are using, the flushAsync call 
                    // is superfluous,but other types of streams may require it.
                    Debug.WriteLine(Convert.ToString(await dataWriter.FlushAsync()));

                    // In order to prolong the lifetime of the stream, detach it from the 
                    // DataWriter so that it will not be closed when Dispose() is called on 
                    // dataWriter. Were we to fail to detach the stream, the call to 
                    // dataWriter.Dispose() would close the underlying stream, preventing 
                    // its subsequent use by the DataReader below.
                    dataWriter.DetachStream();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }*/
        }

        private static void ShowToast(string s)
        {
            if (App.RunningInBackground)
            {
                var toast = new ShellToast { Content = s, Title = "Shoe" };
                toast.Show();
            }
            App.Log(s);
        }
    }
}
