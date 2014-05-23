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
using System.Runtime.CompilerServices;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Microsoft.Phone.Maps.Services;

// Bluetooth
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Microsoft.Phone.Tasks;

using App.Utils;

[assembly: InternalsVisibleTo("UnitTests")]

namespace App
{
    public class ShoeModel : INotifyPropertyChanged
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
        const int BUFFER_SIZE = 3;
        const string LEFT_SHOE = "LEFT";
        const string RIGHT_SHOE = "RIGHT";

        // Commads
        const byte VIBRATE = 1;
        const byte TESTCONN = 2;

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
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        int _dist = 10;
        public int Distance
        {
            get { return _dist; }
            set 
            {
                if (value == _dist) return;
                _dist = value;
                NotifyPropertyChanged("Distance");
            }
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender != App.ViewModel) return;

            if (e.PropertyName == "MyRoute")
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

            if (e.PropertyName == "MyLocation")
            {
                CalculateNavigationInstruction(App.ViewModel.MyLocation);
            }
        }

        public async Task ConnectToShoes()
        {
            ResetLeft();
            ResetRight();

            // Ensure bluetooth is connected
            if (!await EnsureBluethoothConnected()) return;

            Debug.WriteLine("Connecting to shoe for real");

            StreamSocket[] results = await Task.WhenAll(
                Task.Run(() => ConnectToShoe(LEFT_SHOE)),
                Task.Run(() => ConnectToShoe(RIGHT_SHOE))
            );

            _leftStreamSocket = results[0];
            _rightStreamSocket = results[1];
            
            if (_leftStreamSocket != null)
            {
                _leftWriter = new DataWriter(_leftStreamSocket.OutputStream);
                _leftReader = new DataReader(_leftStreamSocket.InputStream);
                new Thread(ListenToShoe).Start(new Tuple<DataReader, ShoeModel>(_leftReader, this));
                Debug.WriteLine("Left shoe connected!!!");
            }

            if (_rightStreamSocket != null)
            {
                _rightWriter = new DataWriter(_rightStreamSocket.OutputStream);
                _rightReader = new DataReader(_rightStreamSocket.InputStream);
                new Thread(ListenToShoe).Start(new Tuple<DataReader, ShoeModel>(_rightReader, this));
                Debug.WriteLine("Right shoe connected!!!");
            }

            UpdateShoeStatus();
        }

        private void ResetRight()
        {
            _rightStreamSocket = null;
            _rightReader = null;
            _rightWriter = null;

            UpdateShoeStatus();
        }

        private void ResetLeft()
        {
            _leftStreamSocket = null;
            _leftReader = null;
            _leftWriter = null;

            UpdateShoeStatus();
        }

        private void UpdateShoeStatus()
        {
            Status s = Status.Disconnected;

            if (_rightWriter != null)
            {
                s = Status.RightConnected;
            }

            if (_leftWriter != null)
            {
                s = s == Status.RightConnected ? Status.Connected : Status.LeftConnected;
            }

            App.Dispatch(() => App.ViewModel.ShoeConnectionStatus = s);
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
            Debug.WriteLine("Listening shoe read");
            string received = "";
            while (m.ShoeConnected(reader))
            {
                received += ReadMessage(reader);

                received = ProcessMessage(received);
            }
        }



        internal static string ProcessMessage(string received)
        {            
            Debug.WriteLine("INPUT: " + received);

            // Remove any empty brackets first
            received = received.Replace("[]", "");

            string s = "";
            string remainder = received;
            bool opened = false;
            for(int i = 0; i < received.Length; i++)
            {
                char c = received[i];
                // Open the brackets
                if (c == '[')
                {
                    opened = true;
                    continue;
                }

                // Close the brackets
                if (c == ']' && opened)
                {
                    opened = false;
                    // Remove the string in question and the empty brackets
                    remainder = remainder.Replace("[" + s + "]", "");

                    Debug.WriteLine("Remainder = " + remainder + ", Consume = " + s);
                    Consume(s);

                    if (!Regex.IsMatch(remainder, @"^(.*)\[(.*)\](.*)"))
                    {
                        Debug.WriteLine("Lets break now");
                        break;
                    }
                    else
                    {
                        s = "";
                        Debug.WriteLine("Continue parsing");
                        continue;
                    }
                }

                // Append char to value string
                if (opened)
                    s += c;
            }

            // Remove all "hanging" strings. These are strings not within any brackets
            if (remainder.Contains("]") || remainder.Contains("["))
                return remainder;
            else
            {
                Debug.WriteLine(remainder + " string will be lost as its a hanging string");
                return "";
            }
        }

        private static void Consume(string s)
        {
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
                    Debug.WriteLine("COMPASS " + tuple);

                    //TODO; Tell client of compass change
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("PARSE EX: " + ex.Message);
                }
            }
            // Left vibrated
            else if (Regex.IsMatch(s, "^L$"))
            {
                // TODO: Event for vibrated
                Debug.WriteLine("VIBRATED " + s);
            }
            // Right vibrated
            else if (Regex.IsMatch(s, "^R$"))
            {
                // TODO: Event for vibrated
                Debug.WriteLine("VIBRATED " + s);
            }
            // Acknowledgement
            else if (Regex.IsMatch(s, "^ACK$"))
            {
                Debug.WriteLine("ACKNOWLEDGEMENT");
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

                Debug.WriteLine("READ: " + ret + ", Count: " + count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("READING EX: " + ex.Message);
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
                    Debug.WriteLine(p.DisplayName);
                    if (!p.DisplayName.StartsWith(shoe)) continue;
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
                                "Pair " + shoe + " shoe", MessageBoxButton.OKCancel);
                        if (result != MessageBoxResult.OK) return;
                        var connectionSettingsTask = new ConnectionSettingsTask
                        {
                            ConnectionSettingsType = ConnectionSettingsType.Bluetooth
                        };
                        connectionSettingsTask.Show();
                    });
                    Debug.WriteLine("Shoe " + shoe + " NOT FOUND");
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
                Debug.WriteLine(ex.Message);
            }

            return s;
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
            if (dist < Distance)  // When distance b2n is below 10 meters instruct shoe
            {
                InstructShoes(man.InstructionKind);
                if (dist < Distance / 2)   // Remove this maneuver, lets get the next one
                {
                    _maneuvers.Dequeue();
                }
            }

            App.ViewModel.HeadingLocation = man.StartGeoCoordinate;

            // Log
            var s = "Dist: " + dist + ", direction = " + man.InstructionKind; 
            Debug.WriteLine(s);
        }

        delegate void Instruct();

        public async void InstructShoes(RouteManeuverInstructionKind maneuverKind)
        {
            ShowToast("Now " + maneuverKind);

            List<Task> tasks = new List<Task>();
            switch (maneuverKind)
            { 
                case RouteManeuverInstructionKind.TurnLeft:
                    tasks.Add(Task.Run(() => SendMessage(_leftWriter, VIBRATE)));
                    break;
                case RouteManeuverInstructionKind.TurnRight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, VIBRATE)));
                    break;
                case RouteManeuverInstructionKind.GoStraight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, VIBRATE)));
                    tasks.Add(Task.Run(() => SendMessage(_leftWriter, VIBRATE)));
                    break;
                case RouteManeuverInstructionKind.UTurnLeft:
                case RouteManeuverInstructionKind.UTurnRight:
                    tasks.Add(Task.Run(() => SendMessage(_rightWriter, VIBRATE)));
                    tasks.Add(Task.Run(() => SendMessage(_leftWriter, VIBRATE)));
                    break;
                default:
                    return;
            }

            await Task.WhenAll(tasks.ToArray());
            Debug.WriteLine("Send direction instruction");
        }

        private async Task<bool> SendMessage(DataWriter d, byte msg)
        {
            if (d == null) return false;

            // Create the data writer object backed by the in-memory stream.
            try
            {
                d.WriteByte(msg);
                await d.StoreAsync();
                await d.FlushAsync();

                return true;
                // TODO: Wait for ACK message from shoe
            }
            catch (Exception ex)
            {
                if (d == _leftWriter) ResetLeft();
                if (d == _rightWriter) ResetRight();

                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private static void ShowToast(string s)
        {
            if (App.RunningInBackground)
            {
                var toast = new ShellToast { Content = s, Title = "Buzz Way" };
                toast.Show();
            }
            Debug.WriteLine(s);
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
