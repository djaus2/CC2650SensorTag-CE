using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.Storage;
using System.Threading;
using System.Collections;

namespace CC2650SenorTagCreators
{
    public sealed partial class CC2650SensorTag
    {
        const GattCharacteristicProperties flagNotify = GattCharacteristicProperties.Notify;
        const GattCharacteristicProperties flagRead = GattCharacteristicProperties.Read;
        const GattCharacteristicProperties flagWrite = GattCharacteristicProperties.Write;

        public sealed class TagSensorServices
        {
            public static Dictionary<CC2650SensorTag.SensorIndexes, SensorChars> Sensors = null;
            public static Dictionary<CC2650SensorTag.SensorTagProperties, SensorChars> Properties = null;

            public static bool doCallback = false;
            public delegate void SensorDataDelegate(SensorData data);
            public SensorDataDelegate CallMeBack { get; set; } = null;

            public TagSensorServices()
            {
                if (Sensors == null)
                    Sensors = new Dictionary<SensorIndexes, SensorChars>();
                if (Properties == null)
                    Properties = new Dictionary<SensorTagProperties, SensorChars>();
            }
            public async Task InterogateServices(IReadOnlyList<GattDeviceService> svcs)
            {
                foreach (var gattService in svcs)
                {

                    var uuid = gattService.Uuid;
                    string st = uuid.ToString();
                    System.Diagnostics.Debug.WriteLine("Service: {0}\r\n", st);
                    await MainPage.PrependTextStatic(string.Format("Service: {0}\r\n",st));
                    SensorChars sensorCharacteristics = null;
                    CC2650SensorTag.SensorTagProperties property = SensorTagProperties.NOTFOUND;
                    CC2650SensorTag.SensorIndexes sensor = CC2650SensorTag.SensorIndexes.NOTFOUND;
                    sensor = CC2650SensorTag.GetSensor(st);
                    if (sensor != CC2650SensorTag.SensorIndexes.NOTFOUND)
                    {
                        sensorCharacteristics = new SensorChars(sensor);
                        System.Diagnostics.Debug.WriteLine("Sensor: {0}", sensor);
                        await MainPage.PrependTextStatic(string.Format("Sensor: {0}", sensor));
                    }
                    else
                    {
                        property = CC2650SensorTag.GetProperty(st);
                        if (property == SensorTagProperties.NOTFOUND)
                        {
                            System.Diagnostics.Debug.WriteLine("Service Not Found: {0}", st);
                            await MainPage.PrependTextStatic(string.Format("Service Not Found: {0}", st));
                            continue;
                        }
                        else
                        {
                            sensorCharacteristics = new SensorChars(property);
                            System.Diagnostics.Debug.WriteLine("Service: {0}", property);
                            await MainPage.PrependTextStatic(string.Format("Service: {0}", property));
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


                        if (sensor != SensorIndexes.NOTFOUND)
                            await MainPage.PrependTextStatic(string.Format("Error getting Characteristics in {0}", sensor.ToString()));
                        else
                            await MainPage.PrependTextStatic(string.Format("Error getting Characteristics in {0}", property.ToString()));

                        continue;
                    }
                    int count = res.Characteristics.Count();
                    var sensorCharacteristicList = res.Characteristics;
                    if (count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No Characteristics in {0}/{1}", sensor, property);
                        if (sensor != SensorIndexes.NOTFOUND)
                            await MainPage.PrependTextStatic(string.Format("No Characteristics in {0}", sensor.ToString()));
                        else
                            await MainPage.PrependTextStatic(string.Format("No Characteristics in {0}", property.ToString()));

                        continue;
                    }
                    var characteristic1 = sensorCharacteristicList.First();

                    foreach (var characteristic in sensorCharacteristicList)
                    {
                        CharacteristicTypes charType = CharacteristicTypes.NOTFOUND;
                        SensorTagProperties charPType =SensorTagProperties.NOTFOUND;

                        if (sensor != SensorIndexes.NOTFOUND)
                        {
                            charType = CC2650SensorTag.GetSensorCharacteristicType(characteristic.Uuid.ToString());
                            System.Diagnostics.Debug.WriteLine("{0} {1}", characteristic.Uuid, charType);
                            await MainPage.PrependTextStatic(string.Format("{0} {1}", characteristic.Uuid, charType));
                        }
                        else
                        {
                            charPType = CC2650SensorTag.GetPropertyCharacteristicType(characteristic.Uuid.ToString());
                            System.Diagnostics.Debug.WriteLine("{0} {1}", characteristic.Uuid, charPType);
                            await MainPage.PrependTextStatic(string.Format("{0} {1}", characteristic.Uuid, charPType));
                        }
                        if (characteristic.CharacteristicProperties.HasFlag(flagNotify))
                        {
                            GattCharacteristic CharacteristicNotification = characteristic;
                            if (CharacteristicNotification != null)
                            {
                                CharacteristicNotification.ValueChanged += Notification_ValueChanged;
                                if (CharacteristicNotification.CharacteristicProperties.HasFlag(flagNotify))
                                    await CharacteristicNotification.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            }
                        }

                        if (sensor != SensorIndexes.NOTFOUND)
                        {
                            switch (charType)
                            {
                                case CC2650SensorTag.CharacteristicTypes.Notify:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Notify, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Enable:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Enable, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Period:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Period, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Data:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Data, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Configuration:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Configuration, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Registers_Address:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Registers_Address, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.Registers_Device_Id:
                                    sensorCharacteristics.Charcteristics.Add(CC2650SensorTag.CharacteristicTypes.Registers_Device_Id, characteristic);
                                    break;
                                case CC2650SensorTag.CharacteristicTypes.NOTFOUND:
                                    break;
                            }
                        }
                        else
                        {


                        }

                    }

                    if (sensor != SensorIndexes.NOTFOUND)
                        Sensors.Add(sensor, sensorCharacteristics);
                    else if (property != SensorTagProperties.NOTFOUND)
                        Properties.Add(property, sensorCharacteristics);
                    ////await TurnOnSensor(sensorCharacteristics); //This launches a new thread for this action but stalls the constructor thread.

                }

            }

            bool chkIgnoreZeros = true;
            bool SetSensorsManualMode = false;
            bool PeriodicUpdatesOnly = false;

            private bool checkArray(CC2650SensorTag.SensorIndexes SensorIndex, byte[] bArray)
            {
                bool ret = false;
                if (bArray != null)
                {
                    if (bArray.Length == DataLength[(int)SensorIndex])
                    {
                        int count = 0;
                        for (int i = 0; i < bArray.Length; i++)
                        {
                            count += (int)bArray[i];
                        }
                        if ((count == 0) && (chkIgnoreZeros))
                        {
                            //Only optical or keys can be all zeros
                            if (SensorIndex == SensorIndexes.OPTICAL)
                                ret = true;
                            else if (SensorIndex == SensorIndexes.KEYS)
                                ret = true;
                            else
                                Debug.WriteLine("Invalid byte[] recvd: All zeros " + SensorIndex.ToString());
                        }
                        else if (DataLength[(int)SensorIndex] != DataLengthUsed[(int)SensorIndex])
                        {
                            //Eg Humidity uses 2 out 4 bytes
                            count = 0;
                            for (int i = 0; i < DataLengthUsed[(int)SensorIndex]; i++)
                            {
                                count += (int)bArray[i];
                            }
                            if (count == 0)
                            {
                                Debug.WriteLine("Invalid used byte[] recvd: All zeros " + SensorIndex.ToString());
                            }
                            else
                                ret = true;
                        }
                        else
                            ret = true;
                    }
                    else
                        Debug.WriteLine("Invalid byte[] recvd: Num bytes " + SensorIndex.ToString());
                }
                else
                {
                    Debug.WriteLine("Invalid byte[] recvd: Null " + SensorIndex.ToString());
                }
                if (!ret)
                {
                    string str = "Invalid byte[]: ";
                    for (int i = 0; i < bArray.Length; i++)
                        str += "[" + bArray[i].ToString() + "] ";
                    Debug.WriteLine(str);
                }
                if (ret)
                {
                    //If running in periodic mode, turn notifications off after 8th Update (we still start in Update mode)
                    if ((SensorIndex != SensorIndexes.MOVEMENT) && (SensorIndex != SensorIndexes.OPTICAL))
                    {
                        ////////if ((SetSensorsManualMode) && (PeriodicUpdatesOnly) && (this.NotificationState == NotificationStates.on))
                        ////////    Task.Run(() => this.DisableNotify()).Wait();
                    }
                    System.Threading.Interlocked.Increment(ref Logging.EventCount);
                }

                return ret;
            }






            public void Notification_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
            {
                var uuid = sender.Service.Uuid;
                string st = uuid.ToString();
                SensorData data = null;
                long incr = 0;

                CC2650SensorTag.SensorIndexes sensor = CC2650SensorTag.GetSensor(st);

                if ( sensor != SensorIndexes.NOTFOUND)
                { 
                byte[] bArray = new byte[eventArgs.CharacteristicValue.Length];
                DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(bArray);
                    if ((bArray.Length == CC2650SensorTag.DataLength[(int)sensor]) ||
                        ((sensor == SensorIndexes.REGISTERS) && ((bArray.Length > 0) && (bArray.Length < 5)))) //Can be 1 to 4 for Registers
                    {
                        //if (sensor != SensorUUIDs.SensorIndexes.REGISTERS)
                        //    System.Diagnostics.Debug.WriteLine(st);

                        
                        long nm = 32;

                        switch (sensor)
                        {
                            case CC2650SensorTag.SensorIndexes.REGISTERS:
                                data = Registers_Handler(sensor, bArray);
                                incr = 1;
                                break;
                            case CC2650SensorTag.SensorIndexes.IR_SENSOR:
                                data = IR_Sensor_Handler(sensor, bArray);
                                incr = 100;
                                break;
                            case CC2650SensorTag.SensorIndexes.HUMIDITY:
                                data = Humidity_Handler(sensor, bArray);
                                incr = 10000;
                                break;
                            case CC2650SensorTag.SensorIndexes.BAROMETRIC_PRESSURE:
                                data = Pressure_Handler(sensor, bArray);
                                incr = 1000000;
                                break;
                            case CC2650SensorTag.SensorIndexes.OPTICAL:
                                data = Optical_Handler(sensor, bArray);
                                incr = 100000000;
                                break;
                            case CC2650SensorTag.SensorIndexes.IO_SENSOR:
                                data = IO_Sensor_Handler(sensor, bArray);
                                incr = 10000000000;
                                break;
                            case CC2650SensorTag.SensorIndexes.KEYS:
                                data = Keys(sensor, bArray);
                                incr = 1000000000000;
                                break;
                            case CC2650SensorTag.SensorIndexes.MOVEMENT:
                                data = Movement_Handler(sensor, bArray);
                                incr = 100000000000000;
                                break;
                            case CC2650SensorTag.SensorIndexes.NOTFOUND:
                                data = NotFound_Handler(sensor, bArray);
                                incr = 10000000000000000;
                                //9223372036854775807
                                break;
                        }
                    }
                    else
                    {
                        CC2650SensorTag.SensorTagProperties property = CC2650SensorTag.GetProperty(st);
                        if (property != SensorTagProperties.NOTFOUND)
                        {
                            byte[] bArray2 = new byte[eventArgs.CharacteristicValue.Length];
                            DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(bArray2);
                            if (true)
                            //((bArray.Length == CC2650SensorTag.DataLength[(int)sensor]) ||
                            //((sensor == SensorIndexes.REGISTERS) && ((bArray.Length > 0) && (bArray.Length < 5)))) //Can be 1 to 4 for Registers
                            {
                                //if (sensor != SensorUUIDs.SensorIndexes.REGISTERS)
                                //    System.Diagnostics.Debug.WriteLine(st);

                                data = null;
                                long nm = 32;
                      
                                switch (property)
                                {
                                }
                            }
                        }
                        else
                        {
                            data = NotFound_Handler(sensor, bArray);
                            incr = 10000000000000000;
                        }
                    }
                    long res;
                    if (Logging.KeepCounting)
                        res = System.Threading.Interlocked.Increment(ref Logging.EventCount);
                    else
                        res = System.Threading.Interlocked.Add(ref Logging.EventCount, incr);

                    if (data != null)
                    {
                       // Debug.WriteLine("{0} ", data.Sensor_Index);
                        //Debug.Write("{0} ", data.Sensor_Index.ToString());
                        //for (int i = 0; i < data.Values.Length; i++)
                        //    Debug.Write("{0} ", data.Values[i].ToString());
                        //Debug.WriteLine("");
                        if (doCallback)
                            if (CallMeBack != null)
                                CallMeBack(data);
                    }
                }
            }

            internal SensorData NotFound_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                throw new NotImplementedException();
            }

            internal SensorData Movement_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                SensorData values = null;

                Int16 dataGyroX = (Int16)(((UInt16)bArray[1] << 8) + (UInt16)bArray[0]);
                Int16 dataGyroY = (Int16)(((UInt16)bArray[3] << 8) + (UInt16)bArray[2]);
                Int16 dataGyroZ = (Int16)(((UInt16)bArray[5] << 8) + (UInt16)bArray[4]);

                Int16 dataAccX = (Int16)(((UInt16)bArray[7] << 8) + (UInt16)bArray[6]);
                Int16 dataAccY = (Int16)(((UInt16)bArray[9] << 8) + (UInt16)bArray[8]);
                Int16 dataAccZ = (Int16)(((UInt16)bArray[11] << 8) + (UInt16)bArray[10]);


                Int16 dataMagX = (Int16)(256 * ((UInt16)bArray[13]) + (UInt16)bArray[12]);
                Int16 dataMagY = (Int16)(256 * ((UInt16)bArray[15]) + (UInt16)bArray[14]);
                Int16 dataMagZ = (Int16)(256 * ((UInt16)bArray[17]) + (UInt16)bArray[16]);


                values = new SensorData
                {
                    Sensor_Index = sensor,
                    Values = new double[] {
                    sensorMpu9250GyroConvert(dataGyroX),
                    sensorMpu9250GyroConvert(dataGyroY),
                    sensorMpu9250GyroConvert(dataGyroZ),
                    sensorMpu9250AccConvert(dataAccX),
                    sensorMpu9250AccConvert(dataAccY),
                    sensorMpu9250AccConvert(dataAccZ),
                    sensorMpu9250MagConvert(dataMagX),
                    sensorMpu9250MagConvert(dataMagY),
                    sensorMpu9250MagConvert(dataMagZ)

                    },
                    Raw = bArray
                };

                return values;
            }

            double sensorMpu9250GyroConvert(Int16 data)
            {
                //-- calculate rotation, unit deg/s, range -250, +250
                return (data * 1.0) / (65536 / 500);
            }


            // Accelerometer ranges
            const int ACC_RANGE_2G = 0;
            const int ACC_RANGE_4G = 1;
            const int ACC_RANGE_8G = 2;
            const int ACC_RANGE_16G = 3;


            int accRange { get; set; } = ACC_RANGE_16G;
            public int UpdatePeriod { get; set; } = 1000;

            double sensorMpu9250AccConvert(Int16 rawData)
            {
                double v = 0;

                switch (accRange)
                {
                    case ACC_RANGE_2G:
                        //-- calculate acceleration, unit G, range -2, +2
                        v = (rawData * 1.0) / (32768 / 2);
                        break;

                    case ACC_RANGE_4G:
                        //-- calculate acceleration, unit G, range -4, +4
                        v = (rawData * 1.0) / (32768 / 4);
                        break;

                    case ACC_RANGE_8G:
                        //-- calculate acceleration, unit G, range -8, +8
                        v = (rawData * 1.0) / (32768 / 8);
                        break;

                    case ACC_RANGE_16G:
                        //-- calculate acceleration, unit G, range -16, +16
                        v = (rawData * 1.0) / (32768 / 16);
                        break;
                }

                return v;
            }

            double sensorMpu9250MagConvert(Int16 data)
            {
                //-- calculate magnetism, unit uT, range +-4900
                return 1.0 * data;
            }

            internal SensorData IO_Sensor_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                //throw new NotImplementedException();
                return null;
            }

            internal SensorData Optical_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                SensorData values = null;

                double lumo = sensorOpt3001Convert(bArray);
                values = new SensorData { Sensor_Index = sensor, Values = new double[] { lumo }, Raw = bArray };

                return values;
            }

            double sensorOpt3001Convert(byte[] bArray)
            {
                Int32 rawData = bArray[1] * 256 + bArray[0];
                Int32 e, m;

                m = rawData & 0x0FFF;
                e = (rawData & 0xF000) >> 12;

                return m * (0.01 * Math.Pow(2.0, e));
            }

            internal SensorData Pressure_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                SensorData values = null;

                Int32 t = bArray[2] * 256;
                Int32 tempT = bArray[1] + t;
                t = tempT * 256 + bArray[0];
                double tempr = (double)t / 100;


                Int32 p = bArray[5] * 256;
                Int32 tempP = bArray[4] + p;
                p = tempP * 256 + bArray[3];
                double pres = (double)p / 100;

                values = new SensorData { Sensor_Index = sensor, Values = new double[] { pres, tempr }, Raw = bArray };

                return values;
            }

            internal SensorData Humidity_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                SensorData values = null;

                ushort upperNum = ((ushort)(bArray[3] * 256 + (ushort)bArray[2]));
                ushort lowerNum = ((ushort)(bArray[1] * 256 + (ushort)bArray[0]));

                double temp = 165 * ((double)(lowerNum)) / 65536.0 - 40.00;
                double humidity = 100 * ((double)(upperNum)) / 65536.0;

                //double humidity = (double)((((UInt16)bArray[1] << 8) + (UInt16)bArray[0]) & ~0x0003);
                //humidity = (-6.0 + 125.0 / 65536 * humidity); // RH= -6 + 125 * SRH/2^16
                values = new SensorData { Sensor_Index = sensor, Values = new double[] { humidity, temp }, Raw = bArray };

                return values;
            }

            internal SensorData IR_Sensor_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                SensorData values = null;

                ushort upperNum = ((ushort)(bArray[3] * 256 + (ushort)bArray[2]));
                ushort lowerNum = ((ushort)(bArray[1] * 256 + (ushort)bArray[0]));


                double SCALE_LSB = 0.03125;
                double t;
                ushort it;

                it = (ushort)((lowerNum) >> 2);
                t = ((double)(it));
                double temp2 = t * SCALE_LSB; ;

                it = (ushort)((upperNum) >> 2);
                t = (double)it;
                double ambient2 = t * SCALE_LSB;

                values = new SensorData { Sensor_Index = sensor, Values = new double[] { ambient2, temp2 }, Raw = bArray }; //AmbTemp, tObj

                return values;
            }

            internal SensorData Registers_Handler(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                //throw new NotImplementedException();
                return null;
            }

            internal SensorData Keys(CC2650SensorTag.SensorIndexes sensor, byte[] bArray)
            {
                byte data = bArray[0];

                double left;
                double right;
                double reed;

                if ((data & 0x01) == 0x01)
                    right = 1;
                else
                    right = 0;

                if ((data & 0x02) == 0x02)
                    left = 1;
                else
                    left = 0;

                if ((data & 0x04) == 0x04)
                    reed = 1;
                else
                    reed = 0;

                var values = new SensorData { Sensor_Index = sensor, Values = new double[] { right, left, reed }, Raw = bArray };
                Debug.WriteLine("Sensor: {0} {1} {2} {3}", sensor, left, right, reed);
                return values;
            }

            public static async Task TurnOnSensor(SensorChars sensorCharacteristics)
            {
                CC2650SensorTag.SensorIndexes sensor = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin turn on sensor: " + sensor.ToString());
                // Turn on sensor
                try
                {
                    if (sensor >= 0 && sensor != SensorIndexes.KEYS && sensor != SensorIndexes.IO_SENSOR && sensor != SensorIndexes.REGISTERS)
                    {
                        if (sensorCharacteristics.Charcteristics.Keys.Contains(CharacteristicTypes.Enable))
                        {
                            GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[CharacteristicTypes.Enable];
                            if (characteristic != null)
                                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                                {
                                    var writer = new Windows.Storage.Streams.DataWriter();
                                    if (sensor == SensorIndexes.MOVEMENT)
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
                CC2650SensorTag.SensorIndexes SensorIndex = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin turn off sensor: " + SensorIndex.ToString());

                try
                {
                    // Turn on sensor
                    if (SensorIndex >= 0 && SensorIndex != SensorIndexes.KEYS && SensorIndex != SensorIndexes.IO_SENSOR && SensorIndex != SensorIndexes.REGISTERS)
                    {
                        if (sensorCharacteristics.Charcteristics.Keys.Contains(CharacteristicTypes.Enable))
                        {
                            GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[CharacteristicTypes.Enable];
                            if (characteristic != null)
                                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
                                {
                                    var writer = new Windows.Storage.Streams.DataWriter();
                                    if (SensorIndex == SensorIndexes.MOVEMENT)
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

                CC2650SensorTag.SensorIndexes SensorIndex = sensorCharacteristics.Sensor_Index;
                Debug.WriteLine("Begin SetSensorPeriod(): " + SensorIndex.ToString());


                try
                {
                    if (sensorCharacteristics.Charcteristics.Keys.Contains(CharacteristicTypes.Period))
                    {
                        GattCharacteristic characteristic = sensorCharacteristics.Charcteristics[CharacteristicTypes.Period];
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


    
   
    public static class Logging
    {
        public static bool KeepCounting = false;
        public static string LogMsg = "";

        static long PeriodCounter = 0;
        static long LastEventCount = 0;
        static Timer EventTimer = null;
        public static long EventCount;

        static int UpdatePeriod = 15000; //15s

        static private async void EventTimerCallback(object state)
        {
            PeriodCounter++;
            long currentCount;
            long diff;
            if (KeepCounting)
            {
                currentCount = System.Threading.Interlocked.Read(ref EventCount);
                diff = currentCount - LastEventCount;
                LastEventCount = currentCount;
            }
            else
            {
                diff = 0;
                diff = System.Threading.Interlocked.Exchange(ref EventCount, diff);
            }

                Debug.WriteLine(PeriodCounter);
            string logMsg = sensorCntr.ToString()+ "-" + PeriodCounter.ToString() + "-" + diff.ToString() ;
            await MainPage.PrependTextStatic(logMsg);
            LogMsg += logMsg + "\r\n"; ;


            if ((PeriodCounter % SensorPeriod) == (SensorPeriod - 1))
            {
                PeriodCounter = 0;

                StorageFolder storageFolder = KnownFolders.DocumentsLibrary; 
                var sampleFile =  await storageFolder.GetFileAsync("sensors.log");
                await Windows.Storage.FileIO.AppendTextAsync(sampleFile, LogMsg);
                

                LogMsg = "";

                await RotateEnableSensors();

            }
        }

        private  const long SensorPeriod = 4; //Switch sensor every 5 minutes.
        private static  byte sensorCntr = 0;

        public static bool GetBit(this byte b, int bitNumber)
        {
            System.Collections.BitArray ba = new BitArray(new byte[] { b });
            return ba.Get(bitNumber);
        }

        private static Dictionary<CC2650SensorTag.SensorIndexes, bool> SensorIsOn;


        private static async Task RotateEnableSensors()
        {
            StopLogging();
            sensorCntr++;
            //Skip any cntr which would have IO on.
            while (GetBit(sensorCntr, (int)CC2650SensorTag.SensorIndexes.IO_SENSOR))
                sensorCntr++;
            string maxSensor = "";
            for (CC2650SensorTag.SensorIndexes sensor= CC2650SensorTag.SensorIndexes.IR_SENSOR; sensor < (CC2650SensorTag.SensorIndexes.REGISTERS); sensor++)
            {
                if (sensor == CC2650SensorTag.SensorIndexes.IO_SENSOR)
                    continue;
                //Skip IO

                if (!SensorIsOn.Keys.Contains(sensor))
                    SensorIsOn.Add(sensor, false);

                
                //else if (sensor == SensorUUIDs.SensorIndexes.KEYS)
                //    sen++;
                bool isToBeOn = GetBit(sensorCntr, (int)sensor); //Yes sensor here IS correct as its the sequential counter
                if (isToBeOn != SensorIsOn[sensor])
                {
                    if (SensorIsOn[sensor])
                    {
                        if (CC2650SensorTag.TagSensorServices.Sensors.Keys.Contains(sensor))
                            if (CC2650SensorTag.TagSensorServices.Sensors[sensor] != null)
                                await CC2650SensorTag.TagSensorServices.TurnOffSensor(CC2650SensorTag.TagSensorServices.Sensors[sensor]);
                    }
                    else
                    {
                        if (CC2650SensorTag.TagSensorServices.Sensors.Keys.Contains(sensor))
                            if (CC2650SensorTag.TagSensorServices.Sensors[sensor] != null)
                                await CC2650SensorTag.TagSensorServices.TurnOnSensor(CC2650SensorTag.TagSensorServices.Sensors[sensor]);
                    }

                    SensorIsOn[sensor] = isToBeOn;
                }
                if (SensorIsOn[sensor])
                    maxSensor = sensor.ToString() + "," + maxSensor;
                else
                    maxSensor =  "," + maxSensor;

            }

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            var sampleFile = await storageFolder.GetFileAsync("sensors.log");

            string hdr = string.Format("{0}---{1}\r\n", sensorCntr, maxSensor );
            Debug.WriteLine("{0}-{1}\r\n", sensorCntr,hdr);
            await Windows.Storage.FileIO.AppendTextAsync(sampleFile, hdr);

            await MainPage.PrependTextStatic(hdr);
            ContinueLogging();

        }

        public static  async Task StartLogging()
        {
            await MainPage.PrependTextStatic("clr");
            LogMsg = "";
            sensorCntr = 0;
            SensorIsOn = new Dictionary<CC2650SensorTag.SensorIndexes, bool>();
            PeriodCounter = 0;
            LastEventCount = 0;

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary;;
            var sampleFile = await storageFolder.CreateFileAsync("sensors.log",
                    CreationCollisionOption.ReplaceExisting);
            await RotateEnableSensors();

            System.Threading.Interlocked.Exchange(ref EventCount, 0);
            ContinueLogging();
        }

        public static void ContinueLogging()
        {
            EventTimer = new Timer(EventTimerCallback, null, 0, (int)UpdatePeriod);
        }

        public static void StopLogging()
        {
            if (EventTimer != null)
                EventTimer.Dispose();
        }

    }
}
