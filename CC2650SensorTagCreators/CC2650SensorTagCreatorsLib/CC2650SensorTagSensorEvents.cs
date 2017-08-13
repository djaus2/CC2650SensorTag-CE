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
    public delegate Task SensorDataDelegate(SensorData data);
    public sealed partial class CC2650SensorTag
    {
       
        public class TagSensorEvents
        {
            bool chkIgnoreZeros = true;
            bool SetSensorsManualMode = false;
            bool PeriodicUpdatesOnly = false;

            public static bool doCallback = false;
            
            public static SensorDataDelegate CallMeBack { get; set; } = null;

            private bool checkArray(SensorServicesCls.SensorIndexes SensorIndex, byte[] bArray)
            {
                bool ret = false;
                if (bArray != null)
                {
                    if (bArray.Length == SensorServicesCls.DataLength[(int)SensorIndex])
                    {
                        int count = 0;
                        for (int i = 0; i < bArray.Length; i++)
                        {
                            count += (int)bArray[i];
                        }
                        if ((count == 0) && (chkIgnoreZeros))
                        {
                            //Only optical or keys can be all zeros
                            if (SensorIndex == SensorServicesCls.SensorIndexes.OPTICAL)
                                ret = true;
                            else if (SensorIndex == SensorServicesCls.SensorIndexes.KEYS)
                                ret = true;
                            else
                                Debug.WriteLine("Invalid byte[] recvd: All zeros " + SensorIndex.ToString());
                        }
                        else if (SensorServicesCls.DataLength[(int)SensorIndex] != SensorServicesCls.DataLengthUsed[(int)SensorIndex])
                        {
                            //Eg Humidity uses 2 out 4 bytes
                            count = 0;
                            for (int i = 0; i < SensorServicesCls.DataLengthUsed[(int)SensorIndex]; i++)
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
                    if ((SensorIndex != SensorServicesCls.SensorIndexes.MOVEMENT) && (SensorIndex != SensorServicesCls.SensorIndexes.OPTICAL))
                    {
                        ////////if ((SetSensorsManualMode) && (PeriodicUpdatesOnly) && (this.NotificationState == NotificationStates.on))
                        ////////    Task.Run(() => this.DisableNotify()).Wait();
                    }
                    System.Threading.Interlocked.Increment(ref Logging.EventCount);
                }

                return ret;
            }


            private long CalculatePower(SensorServicesCls.SensorIndexes sensor)
            {
                long Result = 1;
                int PowerOf = (int)sensor;
                for (int i = 0; i< PowerOf; i++)
                {
                    Result = (Result * 100);
                }
                return Result;
            }

            private Dictionary<SensorServicesCls.SensorIndexes, long> increments = null;

            public void GetIncrements()
            {

                increments = new Dictionary<SensorServicesCls.SensorIndexes, long>();
                for (SensorServicesCls.SensorIndexes sensor = SensorServicesCls.SensorIndexes.IR_SENSOR;
                    sensor < SensorServicesCls.SensorIndexes.NOTFOUND; sensor++)
                {
                    long incr = CalculatePower(sensor);
                    increments.Add(sensor, incr);
                }
            }


            public async void Notification_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
            {
                var uuid = sender.Service.Uuid;
                string st = uuid.ToString();
                SensorData data = null;
                long incr = 0;

                SensorServicesCls.SensorIndexes sensor = SensorServicesCls.GetSensor(st);

                if (sensor != SensorServicesCls.SensorIndexes.NOTFOUND)
                {
                    byte[] bArray = new byte[eventArgs.CharacteristicValue.Length];
                    DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(bArray);
                    if ((bArray.Length == SensorServicesCls.DataLength[(int)sensor]) ||
                        ((sensor == SensorServicesCls.SensorIndexes.REGISTERS) && ((bArray.Length > 0) && (bArray.Length < 5)))) //Can be 1 to 4 for Registers
                    {
                        //if (sensor != SensorUUIDs.SensorIndexes.REGISTERS)
                        //    System.Diagnostics.Debug.WriteLine(st);


                        long nm = 32;
                        incr = increments[sensor];

                        switch (sensor)
                        {
                            case SensorServicesCls.SensorIndexes.REGISTERS:
                                data = Registers_Handler(sensor, bArray);
                                //incr = 1;
                                break;
                            case SensorServicesCls.SensorIndexes.IR_SENSOR:
                                data = IR_Sensor_Handler(sensor, bArray);
                                //incr = 100;
                                break;
                            case SensorServicesCls.SensorIndexes.HUMIDITY:
                                data = Humidity_Handler(sensor, bArray);
                                //incr = 10000;
                                break;
                            case SensorServicesCls.SensorIndexes.BAROMETRIC_PRESSURE:
                                data = Pressure_Handler(sensor, bArray);
                                //incr = 1000000;
                                break;
                            case SensorServicesCls.SensorIndexes.OPTICAL:
                                data = Optical_Handler(sensor, bArray);
                                //incr = 100000000;
                                break;
                            case SensorServicesCls.SensorIndexes.IO_SENSOR:
                                data = IO_Sensor_Handler(sensor, bArray);
                                //incr = 10000000000;
                                break;
                            case SensorServicesCls.SensorIndexes.KEYS:
                                data = Keys(sensor, bArray);
                                //incr = 1000000000000;
                                break;
                            case SensorServicesCls.SensorIndexes.MOVEMENT:
                                data = Movement_Handler(sensor, bArray);
                                //incr = 100000000000000;
                                break;
                            case SensorServicesCls.SensorIndexes.NOTFOUND:
                                data = this.NotFound_Handler(sensor, bArray);
                                //incr = 10000000000000000;
                                //9223372036854775807
                                break;
                        }
                    }
                }
                else
                {
                    //PropertyServiceCls.SensorTagProperties property = PropertyServices.GetProperty(st);
                    //if (property != PropertyServiceCls.SensorTagProperties.NOTFOUND)
                    //{
                    //    byte[] bArray2 = new byte[eventArgs.CharacteristicValue.Length];
                    //    DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(bArray2);
                    //    if (true)
                    //    //((bArray.Length == CC2650SensorTag.DataLength[(int)sensor]) ||
                    //    //((sensor == SensorIndexes.REGISTERS) && ((bArray.Length > 0) && (bArray.Length < 5)))) //Can be 1 to 4 for Registers
                    //    {
                    //        //if (sensor != SensorUUIDs.SensorIndexes.REGISTERS)
                    //        //    System.Diagnostics.Debug.WriteLine(st);

                    //        data = null;
                    //        long nm = 32;

                    //        switch (property)
                    //        {
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    data = NotFound_Handler(sensor, bArray);
                    //    incr = 10000000000000000;
                    //}
                }
                long res;
                
                res = System.Threading.Interlocked.Increment(ref Logging.AllEventCount);
                if (!Logging.KeepCounting)
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
                            await CallMeBack(data);
                }
            }



            internal SensorData NotFound_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
            {
                throw new NotImplementedException();
            }

            SensorData Movement_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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

            internal SensorData IO_Sensor_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
            {
                //throw new NotImplementedException();
                return null;
            }

            internal SensorData Optical_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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

            internal SensorData Pressure_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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

            internal SensorData Humidity_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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

            internal SensorData IR_Sensor_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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

            internal SensorData Registers_Handler(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
            {
                //throw new NotImplementedException();
                return null;
            }

            internal SensorData Keys(SensorServicesCls.SensorIndexes sensor, byte[] bArray)
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


        }
    }
}
