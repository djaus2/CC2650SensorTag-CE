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
    public static class Logging
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
            PeriodCounter++;

            //Log battery level
            byte batteryLevel = 0xff;
            var res = await Connectivity.TagServices.PropertyServices.GetBatteryLevel();
            if (res != null)
                if (res.Length > 0)
                    batteryLevel = res[0];
            string strnBatteryLevel = "";
            if (batteryLevel != 0xff)
                strnBatteryLevel = "[" + batteryLevel.ToString() + "] ";

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

            //Write log to UX
            string logMsg = SensorCntr.ToString() + " " + PeriodCounter.ToString() + " "  + strnBatteryLevel + diff.ToString();
            await CC2650SensorTag.PrependTextStatic(logMsg);

            //Append log to Log
            LogMsg += logMsg + "\r\n"; ;

            //Write Log every SensorPeriod
            if ((PeriodCounter % SensorPeriod) == (SensorPeriod - 1))
            {
                PeriodCounter = 0;

                StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
                var sampleFile = await storageFolder.GetFileAsync("sensors.log");
                await Windows.Storage.FileIO.AppendTextAsync(sampleFile, LogMsg);

                if (Iterate)
                {
                    PauseLogging();
                    LogMsg = "";

                    await IterateEnableDisableSensors();
                    ContinueLogging();
                }

            }
        }

        private static long SensorPeriod = 4; //Switch sensor every 5 minutes.
        private static byte SensorCntr = 0;
        private static bool Iterate = true;

        public static bool GetBit(this byte b, int bitNumber)
        {
            System.Collections.BitArray ba = new BitArray(new byte[] { b });
            return ba.Get(bitNumber);
        }

        private static Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool> SensorIsOn;


        private static async Task IterateEnableDisableSensors()
        {
           
            SensorCntr++;
            //Skip any cntr which would have IO on.
            while (GetBit(SensorCntr, (int)CC2650SensorTag.SensorServicesCls.SensorIndexes.IO_SENSOR))
                SensorCntr++;
            string listActiveSensors = "";
            for (CC2650SensorTag.SensorServicesCls.SensorIndexes sensor = CC2650SensorTag.SensorServicesCls.SensorIndexes.IR_SENSOR; sensor < (CC2650SensorTag.SensorServicesCls.SensorIndexes.REGISTERS); sensor++)
            {
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

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            var sampleFile = await storageFolder.GetFileAsync("sensors.log");

            string hdr = string.Format("{0}   {1}\r\n", SensorCntr, listActiveSensors);
            Debug.WriteLine("{0}   {1}\r\n", SensorCntr, hdr);
            await Windows.Storage.FileIO.AppendTextAsync(sampleFile, hdr);

            await CC2650SensorTag.PrependTextStatic(hdr);
            

        }

        public static async Task StartLogging(long numLoops, long period, byte config , bool iterate)
        {
            await CC2650SensorTag.PrependTextStatic("cls");
            LogMsg = "";
            SensorCntr = config;
            if (SensorCntr != 0)
                SensorCntr--; //It gets incremented when first (and every time) ity is used.
            Iterate = iterate;
            SensorIsOn = new Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool>();
            PeriodCounter = 0;
            LastEventCount = 0;

            SensorPeriod = numLoops;
            UpdatePeriod = 1000 * period;

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            var sampleFile = await storageFolder.CreateFileAsync("sensors.log",
                    CreationCollisionOption.ReplaceExisting);
            await IterateEnableDisableSensors();

            System.Threading.Interlocked.Exchange(ref EventCount, 0);
            ContinueLogging();
        }

        public static void ContinueLogging()
        {
            //Ignore counts whilst timer is disabled
            LastEventCount = System.Threading.Interlocked.Read(ref EventCount);
            EventTimer = new Timer(EventTimerCallback, null, (int)UpdatePeriod, (int)UpdatePeriod);
        }

        public static async Task StopLogging()
        {
            PauseLogging();

            SensorCntr = 0;
            await IterateEnableDisableSensors();
            SensorCntr = 0;
        }

        public static void PauseLogging()
        {
            if (EventTimer != null)
                EventTimer.Dispose();
        }

    }
}


