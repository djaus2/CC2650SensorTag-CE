using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC2650SenorTagCreators
{
    public sealed partial class CC2650SensorTag
    {
        internal const int SENSOR_MAX = (int)SensorIndexes.REGISTERS;
        public static int NUM_SENSORS { get; set; } = SENSOR_MAX;
        public const int NUM_SENSORS_ALL = SENSOR_MAX + 1;
        public static int NUM_SENSORS_TO_TEST { get; set; } = NUM_SENSORS;
        public static int FIRST_SENSOR { get; set; } = (int)SensorIndexes.IR_SENSOR;



        /// <summary>
        /// List of sensors
        /// </summary>
        public enum SensorIndexes
        {
            IR_SENSOR,
            HUMIDITY,
            BAROMETRIC_PRESSURE,
            IO_SENSOR,
            KEYS,
            OPTICAL,
            MOVEMENT,
            REGISTERS,
            NOTFOUND
        }

        /// <summary>
        /// The number of bytes in for each sensor's Data characteristic that are used
        /// </summary>
        internal static readonly List<int> DataLength = new List<int>(){
            4,
            4,
            6,
            1,
            1,
            2,
            18,
            -1, //Can be 1 to 4 for Registers
            1,
        };

        internal static readonly List<int> DataLengthUsed = new List<int>(){
            4,
            2,
            6,
            1,
            1,
            2,
            18,
            -1, //Can be 1 to 4 for Registers
            1,
        };

        internal static int BATT_INDX = 8; //Num Bytes for Battery Level is 1


        /// <summary>
        /// The prefix for sensor Guids. Keys, IO_SENSOR and REGISTERS excluded as these are specifically defined.
        /// </summary>
        internal static readonly Dictionary<SensorIndexes, string> SensorsUUIDsTable
            = new Dictionary<SensorIndexes, string>()
        {
            {SensorIndexes.IR_SENSOR, "F000AA00-0451-4000-B000-000000000000"},
            {SensorIndexes.HUMIDITY,"F000AA20-0451-4000-B000-000000000000"},
            {SensorIndexes.BAROMETRIC_PRESSURE,"F000AA40-0451-4000-B000-000000000000"},
            {SensorIndexes.IO_SENSOR,"F000AA64-0451-4000-B000-000000000000"},
            {SensorIndexes.KEYS,"0000FFE0-0000-1000-8000-00805F9B34FB"},
            {SensorIndexes.OPTICAL,"F000AA70-0451-4000-B000-000000000000"},
            {SensorIndexes.MOVEMENT,"F000AA80-0451-4000-B000-000000000000" },
            {SensorIndexes.REGISTERS,"F000AC00-0451-4000-B000-000000000000"},
            {SensorIndexes.NOTFOUND,""},

        };

        //The reverse of table  SensorsUUIDs
        internal static  Dictionary<string, SensorIndexes> UUIDsSensorsTable =null;

        /// <summary>
        /// Generate UUIDsSensorsTable from SensorsUUIDsTable
        /// Swap keys with values
        /// </summary>
        public static void InitSensorIndexUUIDs()
        {
            UUIDsSensorsTable  =  new Dictionary<string, SensorIndexes>();
            foreach (var x in SensorsUUIDsTable)
                if (!UUIDsSensorsTable .Keys.Contains(x.Value.ToUpper()))
                    UUIDsSensorsTable .Add(x.Value.ToUpper(), x.Key);
                else
                {
                    //Errant condition
                    //Each value and each key shuld be unique in the original table
                    //Note ToUpper so all searches based on UUIDs use UC of strings
                }
        }

        internal static SensorIndexes GetSensor(string uuid)
        {
            if (UUIDsSensorsTable ==null)
                InitSensorIndexUUIDs();
            SensorIndexes sensor = SensorIndexes.NOTFOUND;
            if (UUIDsSensorsTable .Keys.Contains(uuid.ToUpper()))
            {
                sensor = UUIDsSensorsTable [uuid.ToUpper()];
            }
            return sensor;
        }

        internal static SensorTagProperties GetProperty(string uuid)
        {
            if (UUIDsPropertyTable == null)
                InitPropertyUUIds();
            SensorTagProperties property = SensorTagProperties.NOTFOUND;
            if (UUIDsPropertyTable.Keys.Contains(uuid.ToUpper()))
            {
                property = UUIDsPropertyTable[uuid.ToUpper()];
            }
            return property;
        }


        //Characteristics lookup table
        internal static Dictionary<string, Tuple<SensorIndexes, CharacteristicTypes>> Characters = null;

        /// <summary>
        /// Generate the Characteristics lookup table
        /// Rather than "Linqing" to find info on a service characteristic create a lookup table of all
        ///   valid Characteristic UUIds wit their types.
        ///   Table has UUID string as key and (sensor,characteristic type) as values
        /// </summary>
        public static void Init()
        {
            if (UUIDsSensorsTable  == null)
                InitSensorIndexUUIDs();
            if(UUIDsPropertyTable==null)
                InitPropertyUUIds();



            Characters = new Dictionary<string, Tuple<SensorIndexes, CharacteristicTypes>>();
            Dictionary<CharacteristicTypes, string> Masks;
            for (SensorIndexes sensor = SensorIndexes.IR_SENSOR; sensor< SensorIndexes.NOTFOUND; sensor++ )
            {
                switch (sensor)
                {
                    case SensorIndexes.IO_SENSOR:
                        Masks = MasksIO;
                        break;
                    case SensorIndexes.REGISTERS:
                        Masks = MasksRegisters;
                        break;
                    case SensorIndexes.BAROMETRIC_PRESSURE:
                        Masks = MasksBaro;
                        break;
                    default:
                        Masks = MasksSensors;
                        break;
                }

                foreach (var mask in Masks)
                {
                    //Add the 7th digit in the mask to the 7th digit in the UUID
                    string uuid = SensorsUUIDsTable[sensor];
                    char[] uuidArray = uuid.ToCharArray();
                    char[] maskArray = mask.Value.ToCharArray();
                    char[] baseArray = Masks[0].ToCharArray();
                    uuidArray[7] =  (char) ( (int) uuidArray[7] +   ( (int) maskArray[7] -(int)baseArray[7]));
                    uuid = new string(uuidArray);

                    Tuple<SensorIndexes, CharacteristicTypes> tpl = new Tuple<SensorIndexes, CharacteristicTypes>(sensor, mask.Key);
                    Characters.Add(uuid.ToUpper(), tpl);
                    //System.Diagnostics.Debug.WriteLine("{0} {1} {2}",uuid, sensor, mask.Key);
                }
            }
        }

        /// <summary>
        /// Use the lookup table
        /// </summary>
        internal static  CharacteristicTypes GetSensorCharacteristicType(string uuid)
        {
            if (Characters == null)
                Init();
            CharacteristicTypes res = CharacteristicTypes.NOTFOUND;
            if (Characters.Keys.Contains(uuid.ToUpper()))
            {
                var rt = Characters[uuid.ToUpper()];
                res = rt.Item2;
            }
            //Could also pass the sensor as its known when this is called and validate against rt.Item1
            return res;
        }

        internal static SensorTagProperties GetPropertyCharacteristicType(string uuid)
        {
            if (UUIDsPropertyTable == null)
                InitPropertyUUIds();
            SensorTagProperties res = SensorTagProperties.NOTFOUND;
            if (UUIDsPropertyTable.Keys.Contains(uuid.ToUpper()))
                res = UUIDsPropertyTable[uuid.ToUpper()];
            return res;
        }

        //The characteristics types
        public enum CharacteristicTypes
        { Base, Data, Notify, Enable, Period, Configuration, Registers_Address, Registers_Device_Id, NOTFOUND };

        private static readonly Dictionary<CharacteristicTypes, string> MasksSensors
        = new Dictionary<CharacteristicTypes, string>()
        {
            { CharacteristicTypes.Base,"00000000-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Notify,"00000001-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Enable,"00000002-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Period,"00000003-0000-0000-0000-000000000000" }
        };

        private static readonly Dictionary<CharacteristicTypes, string> MasksBaro
        = new Dictionary<CharacteristicTypes, string>()
        {
            { CharacteristicTypes.Base,"00000000-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Notify,"00000001-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Enable,"00000002-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Period,"00000004-0000-0000-0000-000000000000" }  //Baro is exception uses 4 not 3
        };

        private static readonly Dictionary<CharacteristicTypes, string> MasksIO
        = new Dictionary<CharacteristicTypes, string>()
        {
            { CharacteristicTypes.Base,"00000000-0000-0000-0000-000000000000" },
            { CharacteristicTypes.Data,"00000001-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Configuration,"00000002-0000-0000-0000-000000000000" }  
        };

        private static readonly Dictionary<CharacteristicTypes, string> MasksRegisters
        = new Dictionary<CharacteristicTypes, string>()
        {
            { CharacteristicTypes.Base,"00000000-0000-0000-0000-000000000000" },
            { CharacteristicTypes.Data,"00000001-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Registers_Address,"00000002-0000-0000-0000-000000000000" }  ,
            { CharacteristicTypes.Registers_Device_Id,"00000003-0000-0000-0000-000000000000" },
        };

    }
}
