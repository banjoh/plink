using System;
using Microsoft.Phone.Shell;
using System.Diagnostics;

//Maps & Location namespaces
using System.Device.Location; // Provides the GeoCoordinate class.
using Windows.Devices.Geolocation; //Provides the Geocoordinate class.
using Microsoft.Phone.Maps.Services;

// Bluetooth
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;

namespace App
{
    public class NavigateShoes
    {
        public Route Route { get; set; }
		private StreamSocket socket = new StreamSocket();

        public NavigateShoes()
        { 
            
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
                    await socket.ConnectAsync(selectedDevice.HostName, "1");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                Debug.WriteLine("Connected!!!");
            }
        }
		
		private async void DoSomethingUseful()
        {  
                // Create the data writer object backed by the in-memory stream.
            try
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(socket.OutputStream))
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
            }
        }

        public void PositionChanged(GeoCoordinate coord)
        {
            // Implement logic that finds out if we are on course, or should turn

            ShellToast toast = new ShellToast();
            toast.Content = "Loc change @" + DateTime.Now.ToShortTimeString() + " to " + 
                coord.Longitude.ToString("0.0000") + ", " + coord.Latitude.ToString("0.0000");
            toast.Title = "Shoe";
            toast.Show();
        }
    }
}
