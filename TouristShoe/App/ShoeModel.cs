// APIS in this class should be within the restriction of the
// ones defined here http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj662941%28v=vs.105%29.aspx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using Windows.Storage.Streams;
using Microsoft.Phone.Tasks;

namespace App
{
    public class ShoeModel
    {
        public enum Status
        { 
            Connected,
            LeftConnected,
            RightConnected,
            Disconnected,
            BluetoothOff
        }

        // Route geometry needed to generate directional commands sent to the
        // shoes.
        private Route _route;
        private readonly Queue<RouteManeuver> _maneuvers = new Queue<RouteManeuver>();

        // Socket used to communicate with the shoes through bluetooth
		private StreamSocket _leftStreamSocket;
        private StreamSocket _rightStreamSocket;

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

        public async Task ConnectToShoes()
        {
            // Ensure bluetooth is connected
            if (!await EnsureBluethoothConnected()) return;

            Debug.WriteLine("Connecting to shoe for real");
            const string left = "RN42-E0D4";
            const string right = "";

            StreamSocket[] results = await Task.WhenAll(
                Task.Run(() => ConnectToShoe(left)),
                Task.Run(() => ConnectToShoe(right))
            );

            _leftStreamSocket = results[0];
            _rightStreamSocket = results[1];

            if (_leftStreamSocket != null && _rightStreamSocket != null)
            {
                App.ViewModel.ShoeConnectionStatus = Status.Connected;
                App.Log("Both shoes connected!!!");
            }
            else if (_leftStreamSocket != null)
            {
                App.ViewModel.ShoeConnectionStatus = Status.LeftConnected;
                App.Log("Left shoe connected!!!");
            }
            else if (_rightStreamSocket != null)
            {
                App.ViewModel.ShoeConnectionStatus = Status.RightConnected;
                App.Log("Right shoe connected!!!");
            }
            else
            {
                App.ViewModel.ShoeConnectionStatus = Status.Disconnected;
                App.Log("Both shoes disconnected :(");
            }
        }

        private static async Task<bool> EnsureBluethoothConnected()
        {
            try
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var pairedDevices = await PeerFinder.FindAllPeersAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Execute code in UI thread
                App.Dispatch(() =>
                {
                    if ((uint)ex.HResult == 0x8007048F)
                    {
                        var result = MessageBox.Show("Your bluetooth network is turned off. Do you want to turn it ON?",
                            "Bluetooth Off", MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            var connectionSettingsTask = new ConnectionSettingsTask
                            {
                                ConnectionSettingsType = ConnectionSettingsType.Bluetooth
                            };
                            connectionSettingsTask.Show();
                        }
                    }
                    else if ((uint)ex.HResult == 0x8007271D)
                    {
                        //0x80070005 - previous error code that may be wrong?
                        MessageBox.Show("To run this app, you must have ID_CAP_PROXIMITY enabled in WMAppManifest.xaml");
                    }
                    else if ((uint)ex.HResult == 0x80072740)
                    {
                        MessageBox.Show("The Bluetooth port is already in use.");
                    }
                    else if ((uint)ex.HResult == 0x8007274C)
                    {
                        MessageBox.Show(
                            "Could not connect to the left shoe Bluetooth Device. Please make sure it is switched on.");
                    }
                    else MessageBox.Show(ex.Message);
                });
                return false;
            }
        }

        private static async Task<StreamSocket> ConnectToShoe(string shoe)
        {
            StreamSocket s = null;
            try
            {
                // Note: You can only browse and connect to paired devices!
                // Configure PeerFinder to search for all paired devices.
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var pairedDevices = await PeerFinder.FindAllPeersAsync();

                PeerInformation selectedDevice = null;

                foreach (PeerInformation p in pairedDevices)
                {
                    App.Log(p.DisplayName);
                    if (p.DisplayName == shoe)
                    {
                        selectedDevice = p;
                        break;
                    }
                }
                if (selectedDevice == null)
                {
                    App.Dispatch(() =>
                    {
                        var result =
                            MessageBox.Show(
                                "Each shoe needs to be paired atleast once before using this application :)",
                                "Pair " + shoe, MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            var connectionSettingsTask = new ConnectionSettingsTask
                            {
                                ConnectionSettingsType = ConnectionSettingsType.Bluetooth
                            };
                            connectionSettingsTask.Show();
                        }
                    });
                    App.Log("Shoe " + shoe + " NOT FOUND");
                    return null;
                }

                // Attempt a connection

                // Make sure ID_CAP_NETWORKING is enabled in your WMAppManifest.xml, or the next 
                // line will throw an Access Denied exception.
                // In this example, the second parameter of the call to ConnectAsync() is the RFCOMM port number, and can range 
                // in value from 1 to 30.

                s = new StreamSocket();
                await s.ConnectAsync(selectedDevice.HostName, "1");
            }
            catch (Exception ex)
            {
                s = null;
                App.Log(ex.Message);
            }

            return s;
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

            InstructShoes(maneuver.InstructionKind);
        }

        public void InstructShoes(RouteManeuverInstructionKind maneuverKind)
        {
            ShowToast("Now turn " + maneuverKind);

            List<Task> tasks = new List<Task>();
            byte instruction;
            switch (maneuverKind)
            { 
                case RouteManeuverInstructionKind.TurnLeft:
                    instruction = 1;
                    tasks.Add(Task.Run(() => SendMessage(_leftStreamSocket, instruction)));
                    break;
                case RouteManeuverInstructionKind.TurnRight:
                    instruction = 2;
                    tasks.Add(Task.Run(() => SendMessage(_rightStreamSocket, instruction)));
                    break;
                case RouteManeuverInstructionKind.GoStraight:
                    instruction = 3;
                    tasks.Add(Task.Run(() => SendMessage(_rightStreamSocket, instruction)));
                    tasks.Add(Task.Run(() => SendMessage(_leftStreamSocket, instruction)));
                    break;
                case RouteManeuverInstructionKind.UTurnLeft:
                case RouteManeuverInstructionKind.UTurnRight:
                    instruction = 4;
                    tasks.Add(Task.Run(() => SendMessage(_rightStreamSocket, instruction)));
                    break;
                default:
                    return;
            }

            // TODO: Send message to shoe
            App.Log("Turn " + instruction);

            Task.WaitAll(tasks.ToArray());
        }

        private async void SendMessage(StreamSocket s, byte msg)
        {
            // Create the data writer object backed by the in-memory stream.
            try
            {
                using (var dataWriter = new DataWriter(s.OutputStream))
                {
                    // Parse the input stream and write each element separately.
                    dataWriter.WriteByte(msg);

                    // Send the contents of the writer to the backing stream.
                    Debug.WriteLine(Convert.ToString(await dataWriter.StoreAsync()));

                    // For the in-memory stream implementation we are using, the flushAsync call 
                    // is unnecessary, but other types of streams may require it.
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
            }
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
