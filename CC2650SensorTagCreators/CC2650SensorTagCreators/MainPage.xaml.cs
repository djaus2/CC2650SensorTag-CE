using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CC2650SenorTagCreators

{


    public sealed partial class MainPage : Page
    {
        CC2650SensorTag.CC2650SensorTagUnpairedBTConnectivity Connectivity = null;

        public MainPage()
        {
            this.InitializeComponent();

            Connectivity = new CC2650SensorTag.CC2650SensorTagUnpairedBTConnectivity();
            CC2650SensorTag.PrependTextStatic = PrependTextStatic;
            msgCounter = 0;
            MP = this;
        }

        //public IReadOnlyList<GattCharacteristic> sensorCharacteristicList = null;
    
        private void Button_StartWatcher(object sender, TappedRoutedEventArgs e)
        {
            msgCounter = 0;
            Connectivity.Start();
        }

        private void Button_Tapped_StopWatcher(object sender, TappedRoutedEventArgs e)
        {
            msgCounter = 0;
            Connectivity.Stop();
        }

        private async void Button_Tapped_StartLogging(object sender, TappedRoutedEventArgs e)
        {
            await PrependTextStatic("cls");
            bool iterate = (chkIterateThruAllSensors.IsChecked == true);
            msgCounter = 0;
            string strnNumLoops = TxtNumLoops.Text;
            long numLoops = 4;
            bool res = long.TryParse(strnNumLoops, out numLoops);

            string strnPeriod = TxtPeriod.Text;
            long period = 15;
            res = long.TryParse(strnPeriod, out period);

            string strnConfig = TxtConfig.Text;
            byte config = 0xff;
            res = byte.TryParse(strnConfig, NumberStyles.HexNumber, null as IFormatProvider, out config);

            await Logging.StartLogging(numLoops, period, config, iterate);
        }

        private async void Button_Tapped_StopLogging(object sender, TappedRoutedEventArgs e)
        {
            msgCounter = 0;
            await Logging.StopLogging();
        }


        static MainPage MP;

        private static long msgCounter = 0;

        public static async Task PrependTextStatic(string str)
        {
            if (msgCounter++ > CC2650SensorTag.MAX_LINES)
            {
                msgCounter = 0;
                await CC2650SensorTag.PrependTextStatic("cls");
            }
            await MP.PrependText(str); 
        }

        public async Task PrependText(string str)
        {
            if (str.ToLower() == "cls")
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TxtOutput.Text = "";
                });
            }
            else
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
               {
                   TxtOutput.Text = str + "\r\n" + TxtOutput.Text;
               });
            }
        }

        private async void Button_Tapped_GetBatteryLevel(object sender, TappedRoutedEventArgs e)
        {
            await PrependTextStatic("cls");
            msgCounter = 0;
            var res = await Connectivity.TagServices.PropertyServices.GetBatteryLevel();
            if (res != null)
                if (res.Length > 0)
                    await PrependTextStatic(string.Format("Battery Level: {0}\r\n", res[0]));
        }

        private async void Button_Tapped_GetProperties(object sender, TappedRoutedEventArgs e)
        {
            msgCounter = 0;
            var res = await Connectivity.TagServices.PropertyServices.GetProperties(true);
            if (res != null)
                if (res.Count > 0)
                {
                    await PrependTextStatic("cls");
                    await PrependText("SensorTag Properties: ");
                    foreach (var x in res)
                    {
                        byte[] bytes = x.Value;
                        if (bytes != null)
                        {
                            string strn = "";
                            if (!CC2650SensorTag.PropertyServiceCls.showbytes.Contains(x.Key))
                            {
                                strn = System.Text.Encoding.UTF8.GetString(bytes);
                                if (strn != null)
                                {
                                    if (strn != "")
                                    {
                                        await PrependTextStatic(string.Format("{0} [{1}]: {2}", x.Key, strn.Length, strn));
                                    }
                                }
                            }
                            else
                            {
                                strn = "";
                                for (int i = 0; i < bytes.Length; i++)
                                {
                                    strn += " " + bytes[i].ToString("X2");
                                }
                                if (strn != "")
                                {
                                    await PrependTextStatic(string.Format("{0} [{1}]: [{2} ]", x.Key, bytes.Length, strn));
                                }
                            }
                            //NB:
                            //    Re: PNP_ID App got: pnp_id[7] { 01 0D 00 00 00 10 01 }
                            //    From:
                            //    https://e2e.ti.com/support/wireless_connectivity/bluetooth_low_energy/f/538/p/434053/1556237
                            //
                            //    In devinfoservice.c, you can find vendor ID and product ID information below where TI's vendor ID is 0x000D. 
                            //    static uint8 devInfoPnpId[DEVINFO_PNP_ID_LEN] ={ 
                            //    1, // Vendor ID source (1=Bluetooth SIG) 
                            //    LO_UINT16(0x000D), HI_UINT16(0x000D), // Vendor ID (Texas Instruments) 
                            //    LO_UINT16(0x0000), HI_UINT16(0x0000), // Product ID (vendor-specific) 
                            //    LO_UINT16(0x0110), HI_UINT16(0x0110) // Product version (JJ.M.N)};  
                            //
                        }
                    }
                }
        }

        private async void Button_Tapped_StartRunning(object sender, TappedRoutedEventArgs e)
        {
            await PrependTextStatic("cls");
            msgCounter = 0;
            string strnNumLoops = TxtNumLoops.Text;
            long numLoops = 4;
            bool res = long.TryParse(strnNumLoops, out numLoops);

            string strnPeriod = TxtPeriod.Text;
            long period = 15;
            res = long.TryParse(strnPeriod, out period);

            string strnConfig = TxtConfig.Text;
            byte config = 0xff;
            res = byte.TryParse(strnConfig, NumberStyles.HexNumber, null as IFormatProvider, out config  );

            await Run.StartRunning(numLoops, period, config);
        }

        private async void Button_Tapped_StopRunning(object sender, TappedRoutedEventArgs e)
        {
            msgCounter = 0;
            await Run.StopRunning();
        }

        private void Button_Tapped_Exit(object sender, TappedRoutedEventArgs e)
        {
            Connectivity = null;
            App.Current.Exit();
        }

        private void chkIterateThruAllSensors_Checked(object sender, RoutedEventArgs e)
        {

        }

    }
    
}
