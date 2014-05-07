// APIS in this class should be within the restriction of the
// ones defined here http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj662941%28v=vs.105%29.aspx

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Text.RegularExpressions;

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

        // TODO: Give correct names
        const int BUFFER_SIZE = 10;
        const string LEFT_SHOE = "RN42-E0D4";
        const string RIGHT_SHOE = "RNBT-1DF8";

        // Route geometry needed to generate directional commands sent to the
        // shoes.
        private Route _route;
        private readonly Queue<RouteManeuver> _maneuvers = new Queue<RouteManeuver>();

        // Socket used to communicate with the shoes through bluetooth
		private StreamSocket _leftStreamSocket;
        private DataReader _leftReader;
        private DataWriter _leftWriter;
        
        private StreamSocket _rightStreamSocket;
        private DataReader _rightReader;
        private DataWriter _rightWriter;
        
        public ShoeModel()
        {
            App.GeoLoc.PositionChanged += GeoLoc_PositionChanged;
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public void DeInit()
        { 
            
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
                foreach (RouteManeuver man in _route.Legs.SelectMany(leg => leg.Maneuvers))
                {
                    _maneuvers.Enqueue(man);
                }

                Debug.WriteLine("ShoeModel: Route updated");
            }
        }

        public async Task ConnectToShoes()
        {
            // Ensure bluetooth is connected
            if (!await EnsureBluethoothConnected()) return;

            Debug.WriteLine("Connecting to shoe for real");

            StreamSocket[] results = await Task.WhenAll(
                Task.Run(() => ConnectToShoe(LEFT_SHOE)),
                Task.Run(() => ConnectToShoe(RIGHT_SHOE))
            );

            _leftStreamSocket = results[0];
            _rightStreamSocket = results[1];

            Status s = Status.Disconnected;

            if (_leftStreamSocket != null)
            {
                _leftWriter = new DataWriter(_leftStreamSocket.OutputStream);
                _leftReader = new DataReader(_leftStreamSocket.InputStream);
                s = Status.LeftConnected;
                new Thread(ListenToShoe).Start(new Tuple<DataReader, ShoeModel>(_leftReader, this));
                App.Log("Left shoe connected!!!");
            }

            if (_rightStreamSocket != null)
            {
                _rightWriter = new DataWriter(_rightStreamSocket.OutputStream);
                _rightReader = new DataReader(_rightStreamSocket.InputStream);
                s = s == Status.LeftConnected ? Status.Connected : Status.RightConnected;
                new Thread(ListenToShoe).Start(new Tuple<DataReader, ShoeModel>(_rightReader, this));
                App.Log("Right shoe connected!!!");
            }
            App.ViewModel.ShoeConnectionStatus = s;
        }

        private bool ShoeConnected(DataReader r)
        {
            lock (this)
            {
                if (r == null) return false;

                if (r == _rightReader && _rightStreamSocket != null) return true;

                if (r == _leftReader && _leftStreamSocket != null) return true;

                return false;
            }
        }

        private static void ListenToShoe(object o)
        {
            Tuple<DataReader, ShoeModel> t = o as Tuple<DataReader, ShoeModel>;
            if (t == null) return;

            ShoeModel m = t.Item2 as ShoeModel;
            if (m == null) return;

            DataReader reader = t.Item1 as DataReader;
            App.Log("Listening shoe read");
            string received = "";
            while (m.ShoeConnected(reader))
            {
                received += ReadMessage(reader);
                App.Log("BEFORE: " + received);

                string s = "";
                // TODO: This logic needs unit tests BAAADLY!!!
                for(int i = 0; i < received.Length; i++)
                {
                    char c = received[i];
                    if (c == '[')
                    {
                        continue;
                    }
                    if (c == ']')
                    {
                        string afterRemove = received.Remove(0, i + 1);
                        // Compass direction change
                        if (Regex.IsMatch(s, @"^(.[0-9]*)\.(.[0-9]*)$"))
                        {
                            // TODO: Event for compass change
                            string[] arr = s.Split('#');

                            try
                            {
                                int x = Int32.Parse(arr[0]);
                                int y = Int32.Parse(arr[1]);
                                Tuple<int, int> tuple = new Tuple<int, int>(x, y);
                                App.Log("COMPASS " + tuple);

                                //TODO; Tell client of compass change
                            }
                            catch (Exception ex)
                            {
                                App.Log("PARSE EX: " + ex.Message);
                            }
                        }
                        // Left vibrated
                        else if (Regex.IsMatch(s, "^L$"))
                        {
                            // TODO: Event for vibrated
                            App.Log("VIBRATED " + s);
                        }
                        // Right vibrated
                        else if (Regex.IsMatch(s, "^R$"))
                        {
                            // TODO: Event for vibrated
                            App.Log("VIBRATED " + s);
                        }

                        App.Log("R = " + afterRemove + ", S = " + s);
                        if (!Regex.IsMatch(afterRemove, @"^\[(.*)\](.*)"))
                        {
                            App.Log("Lets break now");
                            received = afterRemove;
                            break;
                        }
                        else
                        {
                            s = "";
                            App.Log("Continue parsing");
                            continue;
                        }
                    }
                    s += c;
                }
            }
        }

        private static string ReadMessage(DataReader reader)
        {
            string ret = "";
            try
            {
                // The encoding and byte order need to match the settings of the writer 
                // we previously used.
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // Once we have written the contents successfully we load the stream.
                reader.LoadAsync(BUFFER_SIZE).AsTask().Wait();
                
                // Keep reading until we consume the complete stream.
                uint count = reader.UnconsumedBufferLength;
                while (reader.UnconsumedBufferLength > 0)
                {
                    // Note that the call to readString requires a length of "code units" 
                    // to read. This is the reason each string is preceded by its length 
                    // when "on the wire".
                    ret += reader.ReadString(1);
                }

                App.Log("READ: " + ret + ", Count: " + count);
            }
            catch (Exception ex)
            {
                App.Log("READING EX: " + ex.Message);
            }

            return ret;
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
                    switch ((uint)ex.HResult)
                    {
                        case 0x8007048F:
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
                            break;
                        case 0x8007271D:
                            MessageBox.Show("To run this app, you must have ID_CAP_PROXIMITY enabled in WMAppManifest.xaml");
                            break;
                        case 0x80072740:
                            MessageBox.Show("The Bluetooth port is already in use.");
                            break;
                        case 0x8007274C:
                            MessageBox.Show(
                                "Could not connect to the left shoe Bluetooth Device. Please make sure it is switched on.");
                            break;
                        default:
                            MessageBox.Show(ex.Message);
                            break;
                    }
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
                    if (p.DisplayName != shoe) continue;
                    selectedDevice = p;
                    break;
                }
                if (selectedDevice == null)
                {
                    App.Dispatch(() =>
                    {
                        var result =
                            MessageBox.Show(
                                "Each shoe needs to be paired atleast once before using this application :)",
                                "Pair " + shoe, MessageBoxButton.OKCancel);
                        if (result != MessageBoxResult.OK) return;
                        var connectionSettingsTask = new ConnectionSettingsTask
                        {
                            ConnectionSettingsType = ConnectionSettingsType.Bluetooth
                        };
                        connectionSettingsTask.Show();
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

        public async void InstructShoes(RouteManeuverInstructionKind maneuverKind)
        {
            ShowToast("Now turn " + maneuverKind);

            List<Task> tasks = new List<Task>();
            switch (maneuverKind)
            { 
                case RouteManeuverInstructionKind.TurnLeft:
                    tasks.Add(Task.Run(() => SendMessage(_leftWriter, 1)));
                    break;
                case RouteManeuverInstructionKind.TurnRight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, 1)));
                    break;
                case RouteManeuverInstructionKind.GoStraight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, 1)));
                    tasks.Add(Task.Run(() => SendMessage(_leftWriter, 1)));
                    break;
                case RouteManeuverInstructionKind.UTurnLeft:
                case RouteManeuverInstructionKind.UTurnRight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, 1)));
                    break;
                default:
                    return;
            }

            await Task.WhenAll(tasks.ToArray());
            App.Log("Send direction instructon");
        }

        private static async void SendMessage(DataWriter d, byte msg)
        {
            if (d == null) return;

            // Create the data writer object backed by the in-memory stream.
            try
            {
                d.WriteByte(msg);
                await d.StoreAsync();
                await d.FlushAsync();

                // TODO: Wait for ACK message from shoe
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
                var toast = new ShellToast { Content = s, Title = "Buzz Way" };
                toast.Show();
            }
            App.Log(s);
        }
    }
}
