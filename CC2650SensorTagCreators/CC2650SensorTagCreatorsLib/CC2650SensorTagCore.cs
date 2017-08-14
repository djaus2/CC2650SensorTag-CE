using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CC2650SenorTagCreators
{
    public delegate Task PassString(string strn);
    public sealed partial class CC2650SensorTag
    {
        public const long MAX_LINES = 75;  //Output text gets cleared after this number of lines.(About 2 screens full)
        public static PassString PrependTextStatic { get; set; } = null;

        const GattCharacteristicProperties flagNotify = GattCharacteristicProperties.Notify;
        const GattCharacteristicProperties flagRead = GattCharacteristicProperties.Read;
        const GattCharacteristicProperties flagWrite = GattCharacteristicProperties.Write;

        public sealed class TagSensorServices
        {
            public SensorServicesCls SensorServices { get; set; } = null;
            public PropertyServiceCls PropertyServices { get; set; } = null;

            public TagSensorEvents SensorEvents = null;

            public static Dictionary<SensorServicesCls.SensorIndexes, SensorChars> Sensors = null;
            //public static Dictionary<CC2650SensorTag.SensorTagProperties, SensorChars> Properties = null;



            public TagSensorServices()
            {
                SensorServices = new SensorServicesCls();
                SensorEvents = new TagSensorEvents();
                PropertyServices = new PropertyServiceCls();
                
                if (Sensors == null)
                    Sensors = new Dictionary<SensorServicesCls.SensorIndexes, SensorChars>();
                if (PropertyServices == null)
                    PropertyServices = new PropertyServiceCls();

                SensorEvents.GetIncrements();
            }

            ~TagSensorServices()
            {
                PropertyServices = null;
                SensorEvents = null;
                SensorServices = null;
                Sensors = null;
            }

            public async Task InterogateServices(IReadOnlyList<GattDeviceService> svcs)
            {
                //With event data event an increment is added.
                //The increment is 100^(sensor)  so each sensor can be counted up to 100 times in each logging interval
                //ie Max of 1 minute 30 seconds to be safe.
                //Each sensor then uses 2 digits in this logged value.
                
                foreach (var gattService in svcs)
                {

                    var uuid = gattService.Uuid;
                    string st = uuid.ToString();
                    System.Diagnostics.Debug.WriteLine("Service: {0}\r\n", st);
                    await CC2650SensorTag.PrependTextStatic(string.Format("Service: {0}\r\n", st));
                    SensorChars sensorCharacteristics = null;

                    PropertyServiceCls.SensorTagProperties property = PropertyServiceCls.SensorTagProperties.NOTFOUND;

                    SensorServicesCls.SensorIndexes sensor = SensorServicesCls.SensorIndexes.NOTFOUND;
                    sensor = SensorServicesCls.GetSensor(st);
                    if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                    {
                        sensorCharacteristics = new SensorChars(sensor);
                        System.Diagnostics.Debug.WriteLine("Sensor: {0}", sensor);
                        await CC2650SensorTag.PrependTextStatic(string.Format("Sensor: {0}", sensor));
                    }
                    else
                    {
                        property = PropertyServices.GetProperty(st);
                        if (property == PropertyServiceCls.SensorTagProperties.NOTFOUND)
                        {

                            System.Diagnostics.Debug.WriteLine("Service Not Found: {0}", st);
                            await CC2650SensorTag.PrependTextStatic(string.Format("Service Not Found: {0}", st));
                            continue;
                        }
                        else
                        {
                            sensorCharacteristics = new SensorChars(property);
                            System.Diagnostics.Debug.WriteLine("Service: {0}", property);
                            await CC2650SensorTag.PrependTextStatic(string.Format("Service: {0}", property));
                        }
                    }


                    //if (sensor == CC2650SensorTag.SensorIndexes.REGISTERS)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("Service Ignored: {0}", st);
                    //    continue;
                    //}



                    var res = await gattService.GetCharacteristicsAsync();
                    if (res.Status != GattCommunicationStatus.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("Error getting Characteristics in {0}/{1}. Status: {2}", sensor, property, res.Status);


                        if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                            await CC2650SensorTag.PrependTextStatic(string.Format("Error getting Characteristics in {0}", sensor.ToString()));
                        else
                            await CC2650SensorTag.PrependTextStatic(string.Format("Error getting Characteristics in {0}", property.ToString()));

                        continue;
                    }
                    int count = res.Characteristics.Count();
                    var sensorCharacteristicList = res.Characteristics;
                    if (count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No Characteristics in {0}/{1}", sensor, property);
                        if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                            await CC2650SensorTag.PrependTextStatic(string.Format("No Characteristics in {0}", sensor.ToString()));
                        else
                            await CC2650SensorTag.PrependTextStatic(string.Format("No Characteristics in {0}", property.ToString()));

                        continue;
                    }
                    var characteristic1 = sensorCharacteristicList.First();

                    foreach (var characteristic in sensorCharacteristicList)
                    {
                        SensorServicesCls.CharacteristicTypes charType = SensorServicesCls.CharacteristicTypes.NOTFOUND;
                        PropertyServiceCls.SensorTagProperties charPType = PropertyServiceCls.SensorTagProperties.NOTFOUND;

                        if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                        {
                            charType = SensorServicesCls.GetSensorCharacteristicType(characteristic.Uuid.ToString());
                            System.Diagnostics.Debug.WriteLine("{0} {1}", characteristic.Uuid, charType);
                            await CC2650SensorTag.PrependTextStatic(string.Format("{0} {1}", characteristic.Uuid, charType));
                        }
                        else
                        {
                            charPType = PropertyServices.GetPropertyCharacteristicType(characteristic.Uuid.ToString());
                            System.Diagnostics.Debug.WriteLine("{0} {1}", characteristic.Uuid, charPType);
                            await CC2650SensorTag.PrependTextStatic(string.Format("{0} {1}", characteristic.Uuid, charPType));
                        }
                        if (characteristic.CharacteristicProperties.HasFlag(flagNotify))
                        {
                            GattCharacteristic CharacteristicNotification = characteristic;
                            if (CharacteristicNotification != null)
                            {
                                CharacteristicNotification.ValueChanged += SensorEvents.Notification_ValueChanged;
                                if (CharacteristicNotification.CharacteristicProperties.HasFlag(flagNotify))
                                    await CharacteristicNotification.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            }
                        }

                        if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                            if(sensorCharacteristics.Charcteristics.Keys.Contains(charType))
                            {
                                switch (charType)
                                {
                                    case SensorServicesCls.CharacteristicTypes.Notify:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Notify, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Enable:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Enable, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Period:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Period, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Data:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Data, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Configuration:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Configuration, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Registers_Address:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Registers_Address, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.Registers_Device_Id:
                                        sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.SensorServicesCls.CharacteristicTypes.Registers_Device_Id, characteristic);
                                        break;
                                    case SensorServicesCls.CharacteristicTypes.NOTFOUND:
                                        break;
                                }
                        }
                        else
                        {
                            if (property != PropertyServiceCls.SensorTagProperties.NOTFOUND)
                            {
                                if(sensorCharacteristics.CharcteristicsP.Keys.Contains(charPType))
                                sensorCharacteristics.CharcteristicsP.Add(charPType, characteristic);
                            }
                        }

                    }

                    if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                        if (!Sensors.Keys.Contains(sensor))
                        Sensors.Add(sensor, sensorCharacteristics);
                    else if (property != PropertyServiceCls.SensorTagProperties.NOTFOUND)
                        if (!PropertyServices.Properties.Keys.Contains(property))
                            PropertyServices.Properties.Add(property, sensorCharacteristics);

                }

            }


            public static async Task TurnOnSensor(SensorChars sensorCharacteristics)
            {
                SensorServicesCls.SensorIndexes sensor = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin turn on sensor: " + sensor.ToString());
                // Turn on sensor
                try
                {
                    if (sensor >= 0 && sensor != SensorServicesCls.SensorIndexes.KEYS && sensor != SensorServicesCls.SensorIndexes.IO_SENSOR && sensor != SensorServicesCls.SensorIndexes.REGISTERS)
                    {
                        if (sensorCharacteristics.Charcteristics.Keys.Contains(SensorServicesCls.CharacteristicTypes.Enable))
                        {
                            GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[SensorServicesCls.CharacteristicTypes.Enable];
                            if (characteristic != null)
                                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                                {
                                    var writer = new Windows.Storage.Streams.DataWriter();
                                    if (sensor == SensorServicesCls.SensorIndexes.MOVEMENT)
                                    {
                                        byte[] bytes = new byte[] { 0x7f, 0x00 };
                                        writer.WriteBytes(bytes);
                                    }
                                    else
                                        writer.WriteByte((Byte)0x01);

                                    var status = await characteristic.WriteValueAsync(writer.DetachBuffer());
                                }
                        }
                        else
                        {

                        }
                    }
                    //IncProgressCounter();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: TurnOnSensor() : " + sensor.ToString() + " " + ex.Message);
                }

                Debug.WriteLine("End turn on sensor: " + sensor.ToString());
            }

            public static async Task TurnOffSensor(SensorChars sensorCharacteristics)
            {
                SensorServicesCls.SensorIndexes SensorIndex = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin turn off sensor: " + SensorIndex.ToString());

                try
                {
                    // Turn on sensor
                    if (SensorIndex >= 0 && SensorIndex != SensorServicesCls.SensorIndexes.KEYS && SensorIndex != SensorServicesCls.SensorIndexes.IO_SENSOR && SensorIndex != SensorServicesCls.SensorIndexes.REGISTERS)
                    {
                        if (sensorCharacteristics.Charcteristics.Keys.Contains(SensorServicesCls.CharacteristicTypes.Enable))
                        {
                            GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[SensorServicesCls.CharacteristicTypes.Enable];
                            if (characteristic != null)
                                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                                {
                                    var writer = new Windows.Storage.Streams.DataWriter();
                                    if (SensorIndex == SensorServicesCls.SensorIndexes.MOVEMENT)
                                    {
                                        byte[] bytes = new byte[] { 0x00, 0x00 };//Fixed
                                        writer.WriteBytes(bytes);
                                    }
                                    else

                                        writer.WriteByte((Byte)0x00);

                                    var status = await characteristic.WriteValueAsync(writer.DetachBuffer());
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: TurnOffSensor(): " + SensorIndex.ToString() + " " + ex.Message);
                }
                Debug.WriteLine("End turn off sensor: " + SensorIndex.ToString());
            }

            public static async void SetSensorPeriod(SensorChars sensorCharacteristics, int period)
            {

                SensorServicesCls.SensorIndexes SensorIndex = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin SetSensorPeriod(): " + SensorIndex.ToString());


                try
                {
                    if (sensorCharacteristics.Charcteristics.Keys.Contains(SensorServicesCls.CharacteristicTypes.Period))
                    {
                        GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[SensorServicesCls.CharacteristicTypes.Period];
                        {
                            if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                            {
                                var writer = new Windows.Storage.Streams.DataWriter();
                                // Accelerometer period = [Input * 10]ms
                                writer.WriteByte((Byte)(period / 10));
                                await characteristic.WriteValueAsync(writer.DetachBuffer());
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: SetSensorPeriod(): " + SensorIndex.ToString() + " " + ex.Message);
                }
                Debug.WriteLine("End SetSensorPeriod(): " + SensorIndex.ToString());
            }
        }
    }

}

