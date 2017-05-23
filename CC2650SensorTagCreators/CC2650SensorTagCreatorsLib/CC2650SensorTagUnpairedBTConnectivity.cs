using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CC2650SenorTagCreators
{
    public sealed partial class CC2650SensorTag
    {
        public class CC2650SensorTagUnpairedBTConnectivity
        {
            BluetoothLEAdvertisementWatcher BLEAdvWatcher;

            public CC2650SensorTag.TagSensorServices TagServices { get; internal set; } = null;

            public CC2650SensorTagUnpairedBTConnectivity()
            {
                TagServices = new CC2650SensorTag.TagSensorServices();
                Logging.Connectivity = this;
            }

            ~CC2650SensorTagUnpairedBTConnectivity()
            {
                BLEAdvWatcher = null;
                TagServices = null;
            }

            long barrier = 0;

            public void Start()
            {
                BluetoothLEAdvertisementFilter blaf = new BluetoothLEAdvertisementFilter();


                BLEAdvWatcher = new BluetoothLEAdvertisementWatcher();
                BLEAdvWatcher.Received += Bleaw_Received;
                System.Threading.Interlocked.Exchange(ref barrier, 0);

                BLEAdvWatcher.Start();
            }

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
                                System.Diagnostics.Debug.WriteLine("Endpoint Device Name: {0}", name);
                                await CC2650SensorTag.PrependTextStatic(string.Format("Endpoint Device Name: {0}", name));
                                var services = await blDevice.GetGattServicesAsync();
                                var svcs = services.Services;
                                System.Diagnostics.Debug.WriteLine("Endpoint Device Id: {0}", blDevice.DeviceId);
                                await CC2650SensorTag.PrependTextStatic(string.Format("Endpoint Device Id: {0}", blDevice.DeviceId));
                                System.Diagnostics.Debug.WriteLine("Start");
                                string nm = blDevice.Name;
                                string did = blDevice.DeviceId;
                                string info = blDevice.DeviceInformation.Name;
                                if (svcs != null)
                                {
                                    int num = svcs.Count;

                                    if (num != 0)
                                    {
                                        foreach (var x in svcs)
                                        {
                                            string sdf = x.Uuid.ToString();
                                            string asdcb = x.DeviceId;
                                            System.Diagnostics.Debug.WriteLine("{0} = {1}", sdf, asdcb);
                                        }
                                        await TagServices.InterogateServices(svcs);

                                        OK = true;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("No Services.");
                                        OK = false;
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("ull Services.");
                                    OK = false;
                                }
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

            public void Stop()
            {
                if (BLEAdvWatcher != null)
                    if (BLEAdvWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                        BLEAdvWatcher.Stop();
            }
        }
    }
}
