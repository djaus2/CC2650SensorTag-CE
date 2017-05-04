# CC2650SensorTag-CS-Creators
A **C#** **UWP** (Universal Windows Platform) App for the Texas Instruments **CC2650SensorTag** CC2650STK implementing **Unpaired** **BLE**  Bluetooth Low Energy  connectivity as available with **Windows 10 Creators Edition**. ***Targets:*** *IoT-Core, Phone, Desktop*

Preamble
========
In the previous version of this project, on GitHub [djaus2/CC2650SensorTag-CS](https://github.com/djaus2/CC2650SensorTag-CS), the tag was required to be paired over Bluetooth with the app's host. [Bluetooth SIG does not require participants in a BLE exchange to be paired.]() Windows 10 previously has required devices to be paired in UWP code. You could either pair the devices outside of the app (eg in Settings or Device Portal) or in-app. 

The original Bluetooth BLE UWP sample from Microsoft on GitHub [ms-iot/samples/.../BluetoothGATT/CS](https://github.com/ms-iot/samples/tree/develop/BluetoothGATT/CS) that connected to an (obsolete) [TI CC2541 SensorTag](http://www.ti.com/tool/CC2541DK-SENSOR?keyMatch=cc2541) implemented in-app pairing. The *CC2650SensorTag-CS* project is a port (and extension) of the Microsoft sample and so originally used this in-app pairing. This was found to be problematic with IoT-Core as in-app pairing requires a PopUp dialog as the user has to confirm acceptance of pairing and of use of Bluetooth in the app. The PopUp API is not supported with IoT-Core. Whilst there was an XAML UI workaround with the Microsoft code for this, the workaround stopped working (at least in the *CC2650SensorTag-CS* app) on IoT-Core circa the Anniversary edition of Windows 10. At this point the *CC2650SensorTag-CS* had pairing removed from the app, requiring the user to do the pairing in IoT-Core using Device Portal.

Both apps made use of known service Guids as documented by TI: [CC2650 SensorTag User's Guide](http://processors.wiki.ti.com/index.php/CC2650_SensorTag_User's_Guide). The *CC2650SensorTag-CS* app also makes use of some standard BLE services as documented by Bluetooth SIG to get Battery level and the SensorTag system information (eg Manufacturer). The SensorTag is an amalgum of various sensors. It can be addressed via its Bluetooth address or through BLE queries searching for its class. Each sensor implements a BLE Service that supports a number of BLE Characteristics. Connectivity with a service is established through a [DeviceWatcher](https://docs.microsoft.com/en-us/uwp/api/Windows.Devices.Enumeration.DeviceWatcher) class that has been queried for its service based upon its (known) Guid. *For this to work the tag has to be paired wuth the app host for these two apps.* Once this service is found, it can be queried to obtain the relevant characteristics using their (known) Guids. The characteristics Guids are typically a fixed offset from the Service Guid. The service characteristics with the SensorTag are:
- Data, read sensor values, write actuator values
- Notification, when enable is written, when sensor values are determine, availability is notified
- Configuration, when enable is written, sensor performs periodic measurements. Also used for other configuration settings as required.
- Period, set the period between sensor readings.

***The take home with these apps is:***   
(a) They require the SensorTag to be paired with the app's host    
(b) You are not discovering services and characteristics; you are requesting access to known services and their known characteristics.

Unpaired BLE Connectvity
========================
The most recent update for Windows 10, Creator edition (version 1503), including for IoT-Core has implemented unpaired BLE connectivity. Rather that running an Watcher with an AQS filter for the known services (through their Guids), the app looks for Advertisements from the SensorTag using a [BluetoothLEAdvertisementWatcher](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementwatcher). These advertisments can be noticed when the tag is Advertisment/Pairing mode which is entered by a short press on tag's power button (on the left). When an advertisment us received it can be interogated for services which can then be interogated for characteristics.
