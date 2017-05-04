# CC2650SensorTag-CS-Creators
A **C#** **UWP** (Universal Windows Platform) App for the Texas Instruments **CC2650SensorTag** CC2650STK implementing **Unpaired** **BLE**  Bluetooth Low Energy  connectivity as available with **Windows 10 Creators Edition**. ***Targets:*** *IoT-Core, Phone, Desktop*

Preamble
========
In the previous version of this project, on GitHub [djaus2/CC2650SensorTag-CS](https://github.com/djaus2/CC2650SensorTag-CS), the tag was required to be paired over Bluetooth with the app's host. Bluetooth SIG does not require participants in a BLE exchange to be paired. Windows 10 previously has required devices to be paired in UWP code. You could either pair the devices outside of the app (eg in Settings or Device Portal) or in-app. The original Bluetooth BLE UWP sample from Microsoft on GitHub [ms-iot/samples/tree/developBluetoothGATT/CS](https://github.com/ms-iot/samples/tree/develop/BluetoothGATT/CS) that connected to an (obsolete) [TI CC2541 SensorTag](http://www.ti.com/tool/CC2541DK-SENSOR?keyMatch=cc2541) implemented in-app pairing. The *CC2650SensorTag-CS* is a port (and extension) of the Microsoft sample and so originally used this in-app pairing. This was found to be problematic with IoT-Core as in-app pairing requires a PopUp dialog as the user has to confirm acceptance of pairing and of use of Bluetooth in the app. The PopUp API is not supported with IoT-Core. Whilst there was an XAML UI workaround with the Microsoft code for this, the workaround stopped working (at least in the *CC2650SensorTag-CS* app) on IoT-Core circa the Anniversary edition of Windows 10. At this point the *CC2650SensorTag-CS* had pairing removed from the app, requiring the user to do the pairing in IoT-Core using Device Portal.
