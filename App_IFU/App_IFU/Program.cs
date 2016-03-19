using Gadgeteer.Modules.GHIElectronics;
using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GHI.Processor;
using System.IO;
using Microsoft.SPOT.Net.NetworkInformation;

namespace App_IFU
{
    public partial class Program
    {
        #region Flashing Firmware / App
        public const int BLOCK_SIZE = 65536;

        public static void FlashFirmware()
        {
            // Reserve the memory needed to buffer the update.
            // A lot of RAM is needed so it is recommended to do this at the program start.
            InFieldUpdate.Initialize(InFieldUpdate.Types.Application);

            // Start loading the new firmware on the RAM reserved in last step.
            // Nothing is written to FLASH In this stage. Power loss and failures are okay
            // Simply abort this stage any way you like!
            // Files can come from Storage, from network, from serial bus and any Other way.
            LoadFile("\\SD\\IFU\\aplikasi2.hex", InFieldUpdate.Types.Application);

            //LoadFile("\\SD\\Config.hex", InFieldUpdate.Types.Configuration);
            //LoadFile("\\SD\\Firmware.hex", InFieldUpdate.Types.Firmware);
            //LoadFile("\\SD\\Firmware2.hex", InFieldUpdate.Types.Firmware); //Only if your device has two firmware files.

            // This method will copy The new firmware from RAM to FLASH.
            // This function will not return But will reset the system when done.
            // Power loss during Before this function resets the system quill result in a corrupted firmware.
            // A manual update will be needed if this method failed, due to power loss for example.
            InFieldUpdate.FlashAndReset();
        }

        public static void LoadFile(string filename, InFieldUpdate.Types type)
        {
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                var data = new byte[BLOCK_SIZE];

                for (int i = 0; i < stream.Length / BLOCK_SIZE; i++)
                {
                    stream.Read(data, 0, BLOCK_SIZE);
                    InFieldUpdate.Load(type, data, BLOCK_SIZE);
                }

                stream.Read(data, 0, (int)stream.Length % BLOCK_SIZE);
                InFieldUpdate.Load(type, data, (int)stream.Length % BLOCK_SIZE);
            }
        }
        // This method is run when the mainboard is powered up or reset.   
        #endregion

        Window mainWindow;
        StackPanel stackPanel;
        Font CurrentFont;
        const string SSID = "majelis taklim";
        const string KeyWifi = "123qweasd";
        void ProgramStarted()
        {
            Debug.Print("Program Started");
            //setup display
            CurrentFont = Resources.GetFont(Resources.FontResources.NinaB);
            mainWindow = displayTE35.WPFWindow;
            stackPanel = new StackPanel(Orientation.Vertical);
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.SetMargin(10, 120, 10, 10);
            stackPanel.Children.Add(new Text(CurrentFont, "Hello, I'm the first firmware..."));
            stackPanel.Children.Add(new Text(CurrentFont, "Tap me to update to the firmware 2..."));
            mainWindow.Child = stackPanel;
            mainWindow.TouchDown += MainWindow_TouchDown;
            //SetupNetwork();
        }
        private void MainWindow_TouchDown(object sender, Microsoft.SPOT.Input.TouchEventArgs e)
        {
            //lepas handler
            mainWindow.TouchDown -= MainWindow_TouchDown;
            //show info
            stackPanel.Children.Add(new Text(CurrentFont, "update is starting..."));
            stackPanel.Invalidate();
            //flash app
            FlashFirmware();
        }
        #region Download Update App
        void SetupNetwork()
        {
            //setup wifi
            wifiRS21.DebugPrintEnabled = true;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged; ;
            wifiRS21.NetworkDown += WifiRS21_NetworkDown;
            wifiRS21.NetworkUp += WifiRS21_NetworkUp;
            // use the router's DHCP server to set my network info
            if (!wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Open();
            if (!wifiRS21.NetworkInterface.IsDhcpEnabled)
            {
                wifiRS21.UseDHCP();
                wifiRS21.NetworkInterface.EnableDhcp();
                wifiRS21.NetworkInterface.EnableDynamicDns();
            }
            // look for avaiable networks
            var scanResults = wifiRS21.NetworkInterface.Scan();

            // go through each network and print out settings in the debug window
            foreach (GHI.Networking.WiFiRS9110.NetworkParameters result in scanResults)
            {
                Debug.Print("****" + result.Ssid + "****");
                Debug.Print("ChannelNumber = " + result.Channel);
                Debug.Print("networkType = " + result.NetworkType);
                Debug.Print("PhysicalAddress = " + GetMACAddress(result.PhysicalAddress));
                Debug.Print("RSSI = " + result.Rssi);
                Debug.Print("SecMode = " + result.SecurityMode);
            }

            // locate a specific network
            GHI.Networking.WiFiRS9110.NetworkParameters[] info = wifiRS21.NetworkInterface.Scan(SSID);
            if (info != null)
            {
                wifiRS21.NetworkInterface.Join(info[0].Ssid, KeyWifi);
                wifiRS21.UseThisNetworkInterface();
                bool res = wifiRS21.IsNetworkConnected;
                Debug.Print("Network joined");
                Debug.Print("active:" + wifiRS21.NetworkInterface.ActiveNetwork.Ssid);


            }
            //when network ready, download update
            DownloadUpdate();
        }
        private void WifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
        }

        private void WifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
        }

        // borrowed from GHI's documentation
        string GetMACAddress(byte[] PhysicalAddress)
        {
            return ByteToHex(PhysicalAddress[0]) + "-"
                                + ByteToHex(PhysicalAddress[1]) + "-"
                                + ByteToHex(PhysicalAddress[2]) + "-"
                                + ByteToHex(PhysicalAddress[3]) + "-"
                                + ByteToHex(PhysicalAddress[4]) + "-"
                                + ByteToHex(PhysicalAddress[5]);
        }

        string ByteToHex(byte number)
        {
            string hex = "0123456789ABCDEF";
            return new string(new char[] { hex[(number & 0xF0) >> 4], hex[number & 0x0F] });
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
        }

        void DownloadUpdate()
        {   
            //this method for downloading updated app, make sure your board is connected to the network
            Gadgeteer.Networking.HttpRequest wc = WebClient.GetFromWeb("http://yourweb.com/firmware.hex");
            wc.ResponseReceived += new HttpRequest.ResponseHandler(wc_ResponseReceived);
        }
        void wc_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            //when download has been completed, store it to sd card
            var state = response.StatusCode;
            var DownloadedFile = response.RawContentBytes;
            sdCard.StorageDevice.WriteFile("\\SD\\IFU\\aplikasi.hex",DownloadedFile);
        }
        #endregion
    }
}
