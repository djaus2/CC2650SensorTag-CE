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
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
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
        CC2650SensorTag.TagSensorServices TagServices = null;
        CC2650SensorTag.PropertyService PropertyService = null;

        public MainPage()
        {
            this.InitializeComponent();
            CC2650SensorTag.Init();

            TagServices = new CC2650SensorTag.TagSensorServices();
            PropertyService = new CC2650SensorTag.PropertyService();
            TagServices.PropertyCls = PropertyService;


            MP = this;
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

        private async void Bleaw_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (sender == null)
                return;
            bool OK = false;
            if (System.Threading.Interlocked.Increment(ref barrier) == 1)
            {
                BLEAdvWatcher.Stop();
                Guid guidNotification;
                ulong blAdress = args.BluetoothAddress; ;
                BluetoothLEDevice blDevice = await
                    Windows.Devices.Bluetooth.BluetoothLEDevice.FromBluetoothAddressAsync(blAdress);
                if (!(blDevice == null))
                {              
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
                            System.Diagnostics.Debug.WriteLine("Endpoint Device Name: {0}",name);
                            await PrependText(string.Format("Endpoint Device Name: {0}", name));
                            var services = await blDevice.GetGattServicesAsync();
                            var svcs = services.Services;
                            System.Diagnostics.Debug.WriteLine("Endpoint Device Id: {0}",blDevice.DeviceId);
                            await PrependText(string.Format("Endpoint Device Id: {0}", blDevice.DeviceId));
                            System.Diagnostics.Debug.WriteLine("Start");

                            
                            await TagServices.InterogateServices(svcs);
                            OK = true;
                        }
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

        private async void Button_Tapped_2(object sender, TappedRoutedEventArgs e)
        {
            await Logging.StartLogging();
        }

        private void Button_Tapped_3(object sender, TappedRoutedEventArgs e)
        {
            Logging.StopLogging();
        }

        private async void Button_Tapped_4(object sender, TappedRoutedEventArgs e)
        {
            StorageFolder storageFolder = KnownFolders.DocumentsLibrary;;
            //        Windows.Storage.StorageFolder storageFolder =
            //Windows.Storage.ApplicationData.Current.LocalFolder;
            var sampleFile = await storageFolder.CreateFileAsync("sample.txt",
                    CreationCollisionOption.GenerateUniqueName);
            //var sampleFile =
            //   await storageFolder.GetFileAsync("sample.txt");
        }

        static MainPage MP;

        public static async Task PrependTextStatic(string str)
        {
            await MP.PrependText(str); 
        }

        public async Task PrependText(string str)
        {
            if (str.ToLower() == "cls")
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TxtOutput.Text = "";
                });
            }
            else
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
               {
                   TxtOutput.Text = str + "\r\n" + TxtOutput.Text;
               });
            }
        }

        private async void Button_Tapped_5(object sender, TappedRoutedEventArgs e)
        {
            var res = await PropertyService.GetBatteryLevel();
            if (res != null)
                if (res.Length > 0)
                    await PrependTextStatic(string.Format("Battery Level: {0}\r\n", res[0]));
        }

        private async void Button_Tapped_6(object sender, TappedRoutedEventArgs e)
        {
            var res = await PropertyService.GetProperties(true);
            if (res != null)
                if (res.Count > 0)
                    foreach (var x in res)
                    {
                        byte[] bytes = x.Value;
                        if (bytes != null)
                        {
                            await PrependTextStatic("cls");
                            string strn = "";
                            if (!CC2650SensorTag.PropertyService.showbytes.Contains(x.Key))
                            {
                                strn = System.Text.Encoding.UTF8.GetString(bytes);
                                if (strn != null)
                                {
                                    if (strn != "")
                                    {
                                        await PrependTextStatic(string.Format("{0} [{1}]: {2}", x.Key, strn.Length, strn));
                                    }
                                }
                            }
                            else
                            {
                                strn =  "";
                                for (int i = 0; i < bytes.Length; i++)
                                {
                                    strn += " " + bytes[i].ToString("X2");
                                }
                                if (strn != "")
                                {
                                    await PrependTextStatic(string.Format("{0} [{1}]: [{2} ]", x.Key, bytes.Length, strn));
                                }
                            }
                                //NB:
                                //    Re: PNP_ID App got: pnp_id[7] { 01 0D 00 00 00 10 01 }
                                //    From:
                                //    https://e2e.ti.com/support/wireless_connectivity/bluetooth_low_energy/f/538/p/434053/1556237
                                //
                                //    In devinfoservice.c, you can find vendor ID and product ID information below where TI's vendor ID is 0x000D. 
                                //    static uint8 devInfoPnpId[DEVINFO_PNP_ID_LEN] ={ 
                                //    1, // Vendor ID source (1=Bluetooth SIG) 
                                //    LO_UINT16(0x000D), HI_UINT16(0x000D), // Vendor ID (Texas Instruments) 
                                //    LO_UINT16(0x0000), HI_UINT16(0x0000), // Product ID (vendor-specific) 
                                //    LO_UINT16(0x0110), HI_UINT16(0x0110) // Product version (JJ.M.N)};  
                                //
                        }
                    }
        }
    }
    
}
