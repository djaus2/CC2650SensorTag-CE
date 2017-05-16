using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CC2650SenorTagCreators
{
    public class SensorData
    {
        public CC2650SensorTag.SensorIndexes Sensor_Index;
        public double[] Values;
        public byte[] Raw;


    }

    public enum CharacteristicsTypes { sensor, property}

    public class SensorChars
    {
        public CC2650SensorTag.SensorIndexes Sensor_Index;
        public CC2650SensorTag.PropertyClass.SensorTagProperties Property_Index;
        public Dictionary<CC2650SensorTag.CharacteristicTypes, GattCharacteristic> Charcteristics;
        public Dictionary<CC2650SensorTag.PropertyClass.SensorTagProperties, GattCharacteristic> CharcteristicsP;
        public CharacteristicsTypes Type;

        public SensorChars(CC2650SensorTag.SensorIndexes Sensor )
        {
            Sensor_Index = Sensor;
            Type = CharacteristicsTypes.sensor;
            Charcteristics = new Dictionary<CC2650SensorTag.CharacteristicTypes, GattCharacteristic>();
        }

        public SensorChars(CC2650SensorTag.PropertyClass.SensorTagProperties Property)
        {
            Property_Index = Property;
            Type = CharacteristicsTypes.property;
            CharcteristicsP = new Dictionary<CC2650SensorTag.PropertyClass.SensorTagProperties, GattCharacteristic>();
        }

    }

    public class SensorCharacteristics
    {
        public CC2650SensorTag.SensorIndexes Sensor;
        public Dictionary<CC2650SensorTag.SensorIndexes, GattCharacteristic> Charcteristics;

        public SensorCharacteristics(CC2650SensorTag.SensorIndexes sensor)
        {
            Sensor = sensor;
            Charcteristics = new Dictionary<CC2650SensorTag.SensorIndexes, GattCharacteristic>();
        }

    }

    public class PropertyCharacteristics
    {
        public CC2650SensorTag.PropertyClass.SensorTagProperties Property;
        public Dictionary<CC2650SensorTag.PropertyClass.SensorTagProperties, GattCharacteristic> Charcteristics;

        public PropertyCharacteristics(CC2650SensorTag.PropertyClass.SensorTagProperties property)
        {
            Property = property;
            Charcteristics = new Dictionary<CC2650SensorTag.PropertyClass.SensorTagProperties, GattCharacteristic>();
        }
    }
}
