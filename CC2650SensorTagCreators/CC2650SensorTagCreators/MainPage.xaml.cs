using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CC2650SenorTagCreators

{




    /// <summary>
        /// An empty page that can be used on its own or navigated to within a Frame.
        /// </summary>
    public sealed partial class MainPage : Page
    {
        BluetoothLEAdvertisementWatcher BLEAdvWatcher;
        

        public MainPage()
        {
            this.InitializeComponent();
            SensorUUIDs.Init();
        }

        public void Start()
        {
            BluetoothLEAdvertisementFilter blaf = new BluetoothLEAdvertisementFilter();


            BLEAdvWatcher = new BluetoothLEAdvertisementWatcher();
            BLEAdvWatcher.Received += Bleaw_Received;
            System.Threading.Interlocked.Exchange(ref barrier, 0);
                
            BLEAdvWatcher.Start();
        }

 
        public IReadOnlyList<GattCharacteristic> sensorCharacteristicList = null;


        long barrier = 0;
        SensorUUIDs.TagSensorServices TagServices;
        private async void Bleaw_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            bool OK = false;
            if (System.Threading.Interlocked.Increment(ref barrier) == 1)
            {
                BLEAdvWatcher.Stop();
                Guid guidNotification;
                ulong blAdress = args.BluetoothAddress; ;
                BluetoothLEDevice blDevice = await
                    Windows.Devices.Bluetooth.BluetoothLEDevice.FromBluetoothAddressAsync(blAdress);
                var name = blDevice.Name;
                if (blDevice.DeviceInformation.Kind ==
                        Windows.Devices.Enumeration.DeviceInformationKind.AssociationEndpoint)
                {

                    var scanresp = args.AdvertisementType;
                    Windows.Devices.Enumeration.DeviceAccessStatus result;
                    try
                    {
                        result = await blDevice.RequestAccessAsync();
                    }
                    catch (Exception ex)
                    {
                        result = Windows.Devices.Enumeration.DeviceAccessStatus.DeniedBySystem;
                    }
                    if (result == Windows.Devices.Enumeration.DeviceAccessStatus.Allowed)
                    {

                        name = blDevice.Name;
                        System.Diagnostics.Debug.WriteLine(name);
                        var services = await blDevice.GetGattServicesAsync();
                        var svcs = services.Services;
                        System.Diagnostics.Debug.WriteLine(blDevice.DeviceId);

                        System.Diagnostics.Debug.WriteLine("Start");

                        TagServices = new SensorUUIDs.TagSensorServices();
                        await TagServices.InterogateService(svcs);
                        OK = true;
                    }
                }
            }
            if (!OK)
            {
                System.Threading.Interlocked.Decrement(ref barrier);
                BLEAdvWatcher.Start();
            }
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Start();
        }

        private void Button_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            if (BLEAdvWatcher != null)
                if (BLEAdvWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                    BLEAdvWatcher.Stop();
        }

        private void Button_Tapped_2(object sender, TappedRoutedEventArgs e)
        {
            Logging.StartLogging();
        }

        private void Button_Tapped_3(object sender, TappedRoutedEventArgs e)
        {
            Logging.StopLogging();
        }
    }
    
}
