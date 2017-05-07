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
        public SensorUUIDs.SensorIndexes Sensor_Index;
        public double[] Values;
        public byte[] Raw;


    }

    public class Sensor
    {
        public SensorUUIDs.SensorIndexes Sensor_Index;
        public Dictionary<SensorUUIDs.CharacteristicTypes, GattCharacteristic> Charcteristics;

        public Sensor(SensorUUIDs.SensorIndexes Sensor)
        {
            Sensor_Index = Sensor;
            Charcteristics = new Dictionary<SensorUUIDs.CharacteristicTypes, GattCharacteristic>();
        }
    }
}
