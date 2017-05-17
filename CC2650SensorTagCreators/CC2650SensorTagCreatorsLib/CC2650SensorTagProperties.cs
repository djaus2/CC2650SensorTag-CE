using Windows.System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel.DataAnnotations;
using System;
using Windows.Devices.Enumeration;

namespace CC2650SenorTagCreators
{
    public delegate void DeviceInfoDel(DeviceInformation SetDdevInfo);
    public delegate void SetupProgressDel();
    public delegate void PassInt(int i);

    public sealed partial class CC2650SensorTag
    {

        public class PropertyServiceCls
        {
            //A GATT Service has Characteristics which if Readable can (in this case) return SensorTag properties

            public Dictionary<SensorTagProperties, SensorChars> Properties = null;

            //https://www.bluetooth.com/specifications/gatt/characteristics


            public const string BATTERY_UUID = "2A19"; // "AA71"

            //Service and Characteristic UUIDs are listed together in these tables

            public const string DEVICE_BATTERY_SERVICE =    "0000180F-0000-1000-8000-00805F9B34FB";
            public const string DEVICE_BATTERY_LEVEL =      "00002A19-0000-1000-8000-00805F9B34FB";

            public const string GENERIC_SERVICE =           "00001800-0000-1000-8000-00805f9b34fb";
            public const string GENERIC_APPEARANCE =        "00002A01-0000-1000-8000-00805f9b34fb";
            public const string GENERIC_PPCP =              "00002A04-0000-1000-8000-00805f9b34fb";

            public const string ATTRIBUTE_SERVICE =         "00001801-0000-1000-8000-00805f9b34fb";

            public const string UUID_PROPERTIES_SERVICE =   "0000180a-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_SYSID =       "00002A23-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_MODEL_NR =    "00002A24-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_SERIAL_NR =   "00002A25-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_FW_NR =       "00002A26-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_HW_NR =       "00002A27-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_SW_NR =       "00002A28-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_MANUF_NR =    "00002A29-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_CERT =        "00002A2A-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_PNP_ID =      "00002A50-0000-1000-8000-00805f9b34fb";
            public const string UUID_PROPERTY_NAME =        "00002A00-0000-1000-8000-00805f9b34fb";

            /// <summary>
            /// Look up a service/property(characteristic)'s UUID
            /// </summary>
            internal static readonly Dictionary<SensorTagProperties, string> PropertiesUUIdsTable
            = new Dictionary<SensorTagProperties, string>()
            {
            { SensorTagProperties.GenericService , GENERIC_SERVICE },
            { SensorTagProperties.Appearance , GENERIC_APPEARANCE },
            { SensorTagProperties.PeripheralPreferredConnectionParameters , GENERIC_PPCP },

            { SensorTagProperties.AttributeService , ATTRIBUTE_SERVICE },

            { SensorTagProperties.BatteryService , DEVICE_BATTERY_SERVICE },
            { SensorTagProperties.BatteryLevel , DEVICE_BATTERY_LEVEL },

            { SensorTagProperties.PropertiesService , UUID_PROPERTIES_SERVICE },
            { SensorTagProperties.SysId , UUID_PROPERTY_SYSID },
            { SensorTagProperties.ModelName , UUID_PROPERTY_MODEL_NR },
            { SensorTagProperties.SerialNumber , UUID_PROPERTY_SERIAL_NR },
            { SensorTagProperties.FirmwareDate , UUID_PROPERTY_FW_NR },
            { SensorTagProperties.HardwareRevision , UUID_PROPERTY_HW_NR },
            { SensorTagProperties.SoftwareRevision , UUID_PROPERTY_SW_NR },
            { SensorTagProperties.ManufacturerId , UUID_PROPERTY_MANUF_NR },
            { SensorTagProperties.BTSigCertification , UUID_PROPERTY_CERT },
            { SensorTagProperties.PNPId , UUID_PROPERTY_PNP_ID },
            { SensorTagProperties.DeviceName , UUID_PROPERTY_NAME },

            {SensorTagProperties.NOTFOUND,""},
            };

            /// <summary>
            /// Get a service/property(characteristic) from its UUID (Generated)
            /// </summary>
            internal static Dictionary<string, SensorTagProperties> UUIDsPropertyTable = null;

            /// <summary>
            /// Services are the Services
            /// Others are the Service Characteristics
            /// Services precede their Charactedristics.
            /// Hence Attribute Servive has no Characteristics
            /// </summary>
            public enum SensorTagProperties
            {
                GenericService, Appearance, PeripheralPreferredConnectionParameters,
                AttributeService,
                PropertiesService, SysId, DeviceName, ModelName, SerialNumber, FirmwareDate,
                HardwareRevision, SoftwareRevision, ManufacturerId, BTSigCertification, PNPId,
                BatteryService, BatteryLevel, NOTFOUND
            };

            /// <summary>
            /// Generate PropertiesPropertyTable from PropertiesUUIdsTable
            /// Swap keys with values
            /// </summary>
            public  PropertyServiceCls()
            {
                Properties = new Dictionary<SensorTagProperties, SensorChars>();

                UUIDsPropertyTable = new Dictionary<string, SensorTagProperties>();
                foreach (var x in PropertiesUUIdsTable)
                    if (!UUIDsPropertyTable.Keys.Contains(x.Value.ToUpper()))
                        UUIDsPropertyTable.Add(x.Value.ToUpper(), x.Key);
                    else
                    {
                        //Errant condition
                        //Each value and each key shuld be unique in the original table
                        //Note ToUpper so all searches based on UUIDs use UC of strings
                    }
            }

            /// <summary>
            /// Look up a property(service or characteristic) based upon its UUID.
            /// Avoid possible error if UUID isn't in the table
            /// </summary>
            /// <param name="uuid"></param>
            /// <returns>The Service or Characteristic(Property)</returns>
            internal  SensorTagProperties GetProperty(string uuid)
            {
                SensorTagProperties property = SensorTagProperties.NOTFOUND;
                if (UUIDsPropertyTable.Keys.Contains(uuid.ToUpper()))
                {
                    property = UUIDsPropertyTable[uuid.ToUpper()];
                }
                return property;
            }

            /// <summary>
            /// NB: Should be able to merge this with the previous method (2Do)
            /// Look up a property(service or characteristic) based upon its UUID.
            /// Avoid possible error if UUID isn't in the table
            /// </summary>
            /// <param name="uuid"></param>
            /// <returns>The Service or Characteristic(Property)</returns>
            internal SensorTagProperties GetPropertyCharacteristicType(string uuid)
            {
                SensorTagProperties res = SensorTagProperties.NOTFOUND;
                if (UUIDsPropertyTable.Keys.Contains(uuid.ToUpper()))
                    res = UUIDsPropertyTable[uuid.ToUpper()];
                return res;
            }




            /// <summary>
            /// These are were used to pass info back the MainPage.
            /// Not used at momemnt in this version of the app
            /// </summary>
            public static SetupProgressDel SetUpProgress { get; set; } = null;
            public static PassInt SetBatteryLevel { get; set; } = null;
            public static void IncProgressCounter()
            {
                SetUpProgress?.Invoke();
            }

            /// <summary>
            /// Look up battery level from Battery Service
            /// </summary>
            /// <returns>%Battery level (0-100)</returns>
            public async Task<byte[]> GetBatteryLevel()
            {
                Debug.WriteLine("Begin GetBatteryLevel");
                if (!Properties.Keys.Contains(SensorTagProperties.BatteryService))
                {
                    Debug.WriteLine("Error: Battery Service not available.");
                    Debug.WriteLine("End GetBatteryLevel");
                    return null;
                }

                if (!Properties[SensorTagProperties.BatteryService].CharcteristicsP.Keys.Contains
                    (SensorTagProperties.BatteryLevel)
                    )
                {
                    Debug.WriteLine("Missing Property {0}", SensorTagProperties.BatteryLevel);
                    Debug.WriteLine("End GetBatteryLevel");
                    return null;
                }

                byte[] bytes = null;
                GattCharacteristicProperties flag = GattCharacteristicProperties.Read;
                GattCharacteristic deviceBatteryLevelCharacteristic = Properties[SensorTagProperties.BatteryService].CharcteristicsP[SensorTagProperties.BatteryLevel];
                if (deviceBatteryLevelCharacteristic != null)
                {
                    if (deviceBatteryLevelCharacteristic.CharacteristicProperties.HasFlag(flag))
                    {
                        try
                        {
                            GattReadResult result = null;
                            try
                            {
                                result = await deviceBatteryLevelCharacteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                            }
                            catch (Exception ex)
                            {
                                string msg = ex.Message;
                                Debug.WriteLine("Error GetBatteryLevel1(): " + msg);
                            }

                            var status = result.Status;
                            if (status == GattCommunicationStatus.Success)
                            {
                                var dat = result.Value;
                                var xx = dat.GetType();
                                var yy = dat.Capacity;
                                var zz = dat.Length;

                                bytes = new byte[result.Value.Length];

                                Windows.Storage.Streams.DataReader.FromBuffer(result.Value).ReadBytes(bytes);

                            }
                        }

                        catch (Exception ex)
                        {
                            string msg = ex.Message;
                            Debug.WriteLine("Error GetBatteryLevel2(): " + msg);
                        }


                    }

                }
                if (bytes != null)
                    if (bytes.Length == CC2650SensorTag.SensorServicesCls.DataLength[CC2650SensorTag.SensorServicesCls.BATT_INDX])
                    {
                        Debug.WriteLine("Battery Level: {0}", bytes[0]);
                        SetBatteryLevel?.Invoke((int)bytes[0]);
                        IncProgressCounter();
                    }
                Debug.WriteLine("End GetBatteryLevel");
                return bytes;
            }

            /// <summary>
            /// Look up a specific SensorTag property using Property Service
            /// </summary>
            /// <param name="property">The property to look upo</param>
            /// <param name="showStartEndMsg">Debugging option</param>
            /// <returns>Byte[]</returns>
            public async Task<byte[]> ReadProperty(SensorTagProperties property, bool showStartEndMsg)
            {
                if (showStartEndMsg)
                    Debug.WriteLine("Begin read property: {0} ", property);

                byte[] bytes = null;

                if (!Properties.Keys.Contains(SensorTagProperties.PropertiesService))
                {
                    Debug.WriteLine("Error: Properties database not set up.");
                    return null;
                }

                if (!Properties[SensorTagProperties.PropertiesService].CharcteristicsP.Keys.Contains(property))
                {
                    Debug.WriteLine("Missing Property {0}", property);
                    return null;
                }

                GattCharacteristicProperties flag = GattCharacteristicProperties.Read;
                GattCharacteristic characteristic = Properties[SensorTagProperties.PropertiesService].CharcteristicsP[property];

                if (characteristic.CharacteristicProperties.HasFlag(flag))
                {
                    try
                    {
                        GattReadResult result = null;
                        try
                        {
                            result = await characteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                        }
                        catch (Exception ex)
                        {
                            string msg = ex.Message;
                            Debug.WriteLine("Error ReadProperty1(): " + msg);
                        }

                        var status = result.Status;
                        if (status == GattCommunicationStatus.Success)
                        {
                            var dat = result.Value;
                            var xx = dat.GetType();
                            var yy = dat.Capacity;
                            var zz = dat.Length;

                            bytes = new byte[result.Value.Length];

                            Windows.Storage.Streams.DataReader.FromBuffer(result.Value).ReadBytes(bytes);
                            IncProgressCounter();
                        }
                    }

                    catch (Exception ex)
                    {
                        string msg = ex.Message;
                        Debug.WriteLine("Error ReadProperty2(): " + msg);
                    }
                }

                if (showStartEndMsg)
                    Debug.WriteLine("End read property: {0} ", property);

                return bytes;
            }


            /// <summary>
            /// If in list, when a property is in list, property value bytes are treated as byte array
            /// If not in a list the property bytes are treated as a string
            /// </summary>
            public static List<SensorTagProperties> showbytes = new List<SensorTagProperties>() { SensorTagProperties.BatteryLevel, SensorTagProperties.SysId, SensorTagProperties.BTSigCertification, SensorTagProperties.PNPId };

            /// <summary>
            /// Look up all property values for SensorTag
            /// </summary>
            /// <returns>list of properties and byte[]</returns>
            public async Task<Dictionary<SensorTagProperties, byte[]>> GetProperties(bool doBattery)
            {
                if (!Properties.Keys.Contains(SensorTagProperties.PropertiesService))
                {
                    Debug.WriteLine("Error: Properties database not set up.");
                    return null;
                }

                byte[] bytes = null;
                Dictionary<SensorTagProperties, byte[]> deviceProprties = new Dictionary<SensorTagProperties, byte[]>();

                Array values = Enum.GetValues(typeof(SensorTagProperties));

                //foreach (SensorTagProperties val in values)
                for (SensorTagProperties val = SensorTagProperties.SysId; val <= SensorTagProperties.PNPId; val++)
                {
                    bytes = null;
                    if (val == SensorTagProperties.BatteryLevel)
                        if (doBattery)
                            bytes = await GetBatteryLevel();
                        else
                            continue;
                    else
                        bytes = await ReadProperty(val, false);
                    if (bytes != null)
                    {
                        if (!showbytes.Contains(val))
                        {
                            string res = System.Text.Encoding.UTF8.GetString(bytes);
                            if (res != null)
                                if (res != "")
                                {
                                    deviceProprties.Add(val, bytes);
                                    Debug.WriteLine("{0} [{1}]: {2}", val.ToString(), res.Length, res);

                                }

                        }
                        else
                        {
                            if (bytes != null)
                            {
                                deviceProprties.Add(val, bytes);
                                string str = val.ToString() + "[" + bytes.Length.ToString() + "] {";
                                Debug.Write(str);
                                for (int i = 0; i < bytes.Length; i++)
                                {
                                    Debug.Write(" " + bytes[i].ToString("X2"));
                                }
                                Debug.WriteLine(" }");
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
                return deviceProprties;
            }
        }

    }
}
