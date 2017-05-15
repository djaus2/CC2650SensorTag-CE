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
        //https://www.bluetooth.com/specifications/gatt/characteristics


        public const string BATTERY_UUID = "2A19"; // "AA71"

        public const string DEVICE_BATTERY_SERVICE =    "0000180F-0000-1000-8000-00805F9B34FB";
        public const string DEVICE_BATTERY_LEVEL =      "00002A19-0000-1000-8000-00805F9B34FB";

        public const string GENERIC_SERVICE =           "00001800-0000-1000-8000-00805f9b34fb";
        public const string GENERIC_APPEARANCE =        "00002A01-0000-1000-8000-00805f9b34fb";
        public const string GENERIC_PPCP =              "00002A04-0000-1000-8000-00805f9b34fb";

        public const string ATTRIBUTE =                 "00001801-0000-1000-8000-00805f9b34fb";

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
       

        public enum SensorTagProperties {Generic, Appearance, Attribute, PeripheralPreferredConnectionParameters,
            SysId, DeviceName, ModelName, SerialNumber, FirmwareDate, HardwareRevision,
            SoftwareRevision, ManufacturerId, BTSigCertification, PNPId,
            BatteryService,  BatteryLevel, Properties, NOTFOUND };

        internal static readonly Dictionary<SensorTagProperties, string>PropertiesUUIdsTable
    = new Dictionary<SensorTagProperties, string>()
    {
        { SensorTagProperties.Generic , GENERIC_SERVICE },
         { SensorTagProperties.Appearance , GENERIC_APPEARANCE },
          { SensorTagProperties.PeripheralPreferredConnectionParameters , GENERIC_PPCP },

        { SensorTagProperties.Attribute , ATTRIBUTE },

        { SensorTagProperties.BatteryService , DEVICE_BATTERY_SERVICE },
        { SensorTagProperties.BatteryLevel , DEVICE_BATTERY_LEVEL },

        { SensorTagProperties.Properties , UUID_PROPERTIES_SERVICE },
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

        internal static Dictionary<string, SensorTagProperties> UUIDsPropertyTable = null;

        public enum PropertyTypes { SysId , 
        ModelName , 
        SerialNumber , 
        FirmwareDate , 
        HardwareRevision , 
        SoftwareRevision ,  
        ManufacturerId ,  
        BTSigCertification , 
        PNPId ,
        DeviceName ,
            None,
        }

        /// <summary>
        /// Generate PropertiesPropertyTable from PropertiesUUIdsTable
        /// Swap keys with values
        /// </summary>
        public static void InitPropertyUUIds()
        {
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

        public static SetupProgressDel SetUpProgress { get; set; } = null;
        public static PassInt SetBatteryLevel { get; set; } = null;
        public static void IncProgressCounter()
        {
            SetUpProgress?.Invoke();
        }


        public  static GattDeviceService DevicePropertyService = null;
        private static GattDeviceService DeviceBatteryService = null;
        private static GattCharacteristic DeviceBatteryLevelCharacteristic = null;

        public static void SetUpBattery(GattDeviceService service)
        {
            DeviceBatteryService = service;
            var DeviceBatteryLevelCharacteristicList = DeviceBatteryService.GetCharacteristics(new Guid(DEVICE_BATTERY_LEVEL));
            DeviceBatteryLevelCharacteristic = null;
            if (DeviceBatteryLevelCharacteristicList != null)
                if (DeviceBatteryLevelCharacteristicList.Count() > 0)
                    DeviceBatteryLevelCharacteristic = DeviceBatteryLevelCharacteristicList[0];
        }

        public static async Task<byte[]> GetBatteryLevel()
        {
            Debug.WriteLine("Begin GetBatteryLevel");
            byte[] bytes = null;
            GattCharacteristicProperties flag = GattCharacteristicProperties.Read;
            if (DeviceBatteryLevelCharacteristic != null)
            {
                if (DeviceBatteryLevelCharacteristic.CharacteristicProperties.HasFlag(flag))
                {
                    try
                    {
                        GattReadResult result = null;
                        try
                        {
                            result = await DeviceBatteryLevelCharacteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
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
            if (bytes!=null)
                if (bytes.Length ==   CC2650SensorTag.DataLength[CC2650SensorTag.BATT_INDX])
                {
                    Debug.WriteLine("Battery Level: {0}", bytes[0]);
                    SetBatteryLevel?.Invoke((int)bytes[0]);
                    IncProgressCounter();
                }
            Debug.WriteLine("End GetBatteryLevel");
            return bytes;
        }

        public static async Task<byte[]> ReadProperty(SensorTagProperties property, bool showStartEndMsg)
        {
            //if (showStartEndMsg)
                Debug.WriteLine("Begin read property: {0} ", property);
            string guidstr = "";
            byte[] bytes = null;
            switch (property)
            {
                case SensorTagProperties.FirmwareDate:
                    guidstr = UUID_PROPERTY_FW_NR;
                    break;
                case SensorTagProperties.HardwareRevision:
                    guidstr = UUID_PROPERTY_HW_NR;
                    break;
                case SensorTagProperties.ManufacturerId:
                    guidstr = UUID_PROPERTY_MANUF_NR;
                    break;
                case SensorTagProperties.ModelName:
                    guidstr = UUID_PROPERTY_MODEL_NR;
                    break;
                case SensorTagProperties.PNPId:
                    guidstr = UUID_PROPERTY_PNP_ID;
                    break;
                case SensorTagProperties.SerialNumber:
                    guidstr = UUID_PROPERTY_SERIAL_NR;
                    break;
                case SensorTagProperties.SoftwareRevision:
                    guidstr = UUID_PROPERTY_SW_NR;
                    break;
                case SensorTagProperties.SysId:
                    guidstr = UUID_PROPERTY_SYSID;
                    break;
                case SensorTagProperties.BTSigCertification:
                    guidstr = UUID_PROPERTY_CERT;
                    break;
                case SensorTagProperties.DeviceName:
                    guidstr = UUID_PROPERTY_NAME;
                    break;
                case SensorTagProperties.BatteryLevel:
                    return bytes;
            }

            IReadOnlyList<GattCharacteristic> sidCharacteristicList = DevicePropertyService.GetCharacteristics(new Guid(guidstr));
            GattCharacteristicProperties flag = GattCharacteristicProperties.Read;
            if (sidCharacteristicList != null)
                if (sidCharacteristicList.Count != 0)
                {
                    GattCharacteristic characteristic = sidCharacteristicList[0];
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

                    
                }
            //if(showStartEndMsg)
            Debug.WriteLine("End read property: {0} ", property);

            return bytes;
        }


        public static List<SensorTagProperties> showbytes = new List<SensorTagProperties>() { SensorTagProperties.BatteryLevel, SensorTagProperties.SysId, SensorTagProperties.BTSigCertification, SensorTagProperties.PNPId };

        //Don't do battery at startup as its called when battery service is started.
        public async static Task<Dictionary<SensorTagProperties, byte[]> > GetProperties(bool doBattery)
        {
            byte[] bytes = null;
            Dictionary<SensorTagProperties, byte[]> deviceProprties = new Dictionary<SensorTagProperties, byte[]>();

            Array values = Enum.GetValues(typeof(SensorTagProperties));

            foreach (SensorTagProperties val in values)
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
