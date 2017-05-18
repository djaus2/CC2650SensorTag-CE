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
    /// <summary>
    /// NB: Must be a top level class.
    /// </summary>
    public static class Run
    {
        public static bool KeepCounting = false;
        public static string LogMsg = "";

        public static CC2650SensorTag.CC2650SensorTagUnpairedBTConnectivity Connectivity { get; set; } = null;

        static long PeriodCounter = 0;
        static long LastEventCount = 0;
        static Timer EventTimer = null;
        public static long EventCount;

        static long UpdatePeriod = 15000; //15s

        static private async void EventTimerCallback(object state)
        {
            //PeriodCounter++;

            ////Log battery level
            //byte batteryLevel = 0xff;
            //var res = await Connectivity.TagServices.PropertyServices.GetBatteryLevel();
            //if (res != null)
            //    if (res.Length > 0)
            //        batteryLevel = res[0];
            //string strnBatteryLevel = "";
            //if (batteryLevel != 0xff)
            //    strnBatteryLevel = "[" + batteryLevel.ToString() + "] ";

            //long currentCount;
            //long diff;
            //if (KeepCounting)
            //{
            //    currentCount = System.Threading.Interlocked.Read(ref EventCount);
            //    diff = currentCount - LastEventCount;
            //    LastEventCount = currentCount;
            //}
            //else
            //{
            //    diff = 0;
            //    diff = System.Threading.Interlocked.Exchange(ref EventCount, diff);
            //}

            //Debug.WriteLine(PeriodCounter);

            ////Write log to UX
            //string logMsg = SensorCntr.ToString() + " " + PeriodCounter.ToString() + " " + strnBatteryLevel + diff.ToString();
            //await CC2650SensorTag.PrependTextStatic(logMsg);

        }

        private static long SensorPeriod = 4; //Switch sensor every 5 minutes.
        private static byte SensorCntr = 0;

        public static bool GetBit(this byte b, int bitNumber)
        {
            System.Collections.BitArray ba = new BitArray(new byte[] { b });
            return ba.Get(bitNumber);
        }

        private static Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool> SensorIsOn = null;


        private static async Task EnableDisableSensors()
        {

            //sensorCntr++;
            //Skip any cntr which would have IO on.
            //while (GetBit(sensorCntr, (int)CC2650SensorTag.SensorServicesCls.SensorIndexes.IO_SENSOR))
            //    sensorCntr++;
            string listActiveSensors = "";
            for (CC2650SensorTag.SensorServicesCls.SensorIndexes sensor = CC2650SensorTag.SensorServicesCls.SensorIndexes.IR_SENSOR; sensor < (CC2650SensorTag.SensorServicesCls.SensorIndexes.REGISTERS); sensor++)
            {
                //IO is an actuator (LEDs and Buzzer) not sensor
                if (sensor == CC2650SensorTag.SensorServicesCls.SensorIndexes.IO_SENSOR)
                    continue;
                //Skip IO

                if (!SensorIsOn.Keys.Contains(sensor))
                    SensorIsOn.Add(sensor, false);


                //else if (sensor == SensorUUIDs.SensorIndexes.KEYS)
                //    sen++;
                bool isToBeOn = GetBit(SensorCntr, (int)sensor); //Yes sensor here IS correct as its the sequential counter
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
                {
                    if (listActiveSensors == "")
                        listActiveSensors = sensor.ToString();
                    else
                        listActiveSensors = sensor.ToString() + "," + listActiveSensors;
                }
                //else
                //    maxSensor = " " + maxSensor;

            }

            //StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            //var sampleFile = await storageFolder.GetFileAsync("sensors.log");

            //string hdr = string.Format("{0}   {1}\r\n", sensorCntr, listActiveSensors);
            //Debug.WriteLine("{0}   {1}\r\n", sensorCntr, hdr);
            //await Windows.Storage.FileIO.AppendTextAsync(sampleFile, hdr);

            //await CC2650SensorTag.PrependTextStatic(hdr);


        }

        public static async Task StartRunning(long numLoops, long period, byte sensorCntr)
        {
            await CC2650SensorTag.PrependTextStatic("cls");
            LogMsg = "";
            SensorCntr = sensorCntr;
            if (SensorIsOn == null)
            {
                SensorIsOn = new Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool>();

                for (CC2650SensorTag.SensorServicesCls.SensorIndexes sensor = CC2650SensorTag.SensorServicesCls.SensorIndexes.IR_SENSOR; sensor < (CC2650SensorTag.SensorServicesCls.SensorIndexes.NOTFOUND); sensor++)
                {
                    SensorIsOn.Add(sensor, false);
                }
            }
            PeriodCounter = 0;
            LastEventCount = 0;

            SensorPeriod = numLoops;
            UpdatePeriod = 1000 * period;

            CC2650SensorTag.TagSensorEvents.CallMeBack = UpdateSensorData;
            CC2650SensorTag.TagSensorEvents.doCallback = true;

            //StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            //var sampleFile = await storageFolder.CreateFileAsync("sensors.log",
            //        CreationCollisionOption.ReplaceExisting);
            await EnableDisableSensors();

            System.Threading.Interlocked.Exchange(ref EventCount, 0);
            ContinueRunning();
        }

        public static void ContinueRunning()
        {
            EventTimer = new Timer(EventTimerCallback, null, (int)UpdatePeriod, (int)UpdatePeriod);
        }

        public static async Task StopRunning()
        {
            if (EventTimer != null)
                EventTimer.Dispose();
            SensorCntr = 0;
            await EnableDisableSensors();
            CC2650SensorTag.TagSensorEvents.doCallback = false;
            CC2650SensorTag.TagSensorEvents.CallMeBack = null; ;
        }




        public async static Task UpdateSensorData(SensorData data)
        {
            //Clear the textbox buffer when has MAX LINES (MAX_LINES). 


            string dataStr = "";
            for (int i = 0; i < data.Values.Length; i++)
            {
                if (i==0)
                    dataStr = data.Values[i].ToString();
                else
                    dataStr += " ," + data.Values[i].ToString();
            }
            string fmt;
            if (data.Sensor_Index != CC2650SensorTag.SensorServicesCls.SensorIndexes.BAROMETRIC_PRESSURE)
                fmt = string.Format("{0}\t\t\t[ {1} ]", data.Sensor_Index, dataStr);
            else
                fmt = string.Format("{0}\t[ {1} ]", data.Sensor_Index, dataStr);
            await CC2650SensorTag.PrependTextStatic(fmt);
        }
    }
}


