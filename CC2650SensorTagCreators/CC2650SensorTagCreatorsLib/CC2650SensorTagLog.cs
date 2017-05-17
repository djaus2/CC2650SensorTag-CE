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
            string logMsg = sensorCntr.ToString() + "-" + PeriodCounter.ToString() + "-" + diff.ToString();
            await CC2650SensorTag.PrependTextStatic(logMsg);
            LogMsg += logMsg + "\r\n"; ;


            if ((PeriodCounter % SensorPeriod) == (SensorPeriod - 1))
            {
                PeriodCounter = 0;

                StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
                var sampleFile = await storageFolder.GetFileAsync("sensors.log");
                await Windows.Storage.FileIO.AppendTextAsync(sampleFile, LogMsg);


                LogMsg = "";

                await RotateEnableSensors();

            }
        }

        private const long SensorPeriod = 4; //Switch sensor every 5 minutes.
        private static byte sensorCntr = 0;

        public static bool GetBit(this byte b, int bitNumber)
        {
            System.Collections.BitArray ba = new BitArray(new byte[] { b });
            return ba.Get(bitNumber);
        }

        private static Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool> SensorIsOn;


        private static async Task RotateEnableSensors()
        {
            StopLogging();
            sensorCntr++;
            //Skip any cntr which would have IO on.
            while (GetBit(sensorCntr, (int)CC2650SensorTag.SensorServicesCls.SensorIndexes.IO_SENSOR))
                sensorCntr++;
            string maxSensor = "";
            for (CC2650SensorTag.SensorServicesCls.SensorIndexes sensor = CC2650SensorTag.SensorServicesCls.SensorIndexes.IR_SENSOR; sensor < (CC2650SensorTag.SensorServicesCls.SensorIndexes.REGISTERS); sensor++)
            {
                if (sensor == CC2650SensorTag.SensorServicesCls.SensorIndexes.IO_SENSOR)
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
                    maxSensor = "," + maxSensor;

            }

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
            var sampleFile = await storageFolder.GetFileAsync("sensors.log");

            string hdr = string.Format("{0}---{1}\r\n", sensorCntr, maxSensor);
            Debug.WriteLine("{0}-{1}\r\n", sensorCntr, hdr);
            await Windows.Storage.FileIO.AppendTextAsync(sampleFile, hdr);

            await CC2650SensorTag.PrependTextStatic(hdr);
            ContinueLogging();

        }

        public static async Task StartLogging()
        {
            await CC2650SensorTag.PrependTextStatic("cls");
            LogMsg = "";
            sensorCntr = 0;
            SensorIsOn = new Dictionary<CC2650SensorTag.SensorServicesCls.SensorIndexes, bool>();
            PeriodCounter = 0;
            LastEventCount = 0;

            StorageFolder storageFolder = KnownFolders.DocumentsLibrary; ;
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


