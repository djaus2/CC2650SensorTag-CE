using System;
using System.Collections.Generic;
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




    /// <summary>
        /// An empty page that can be used on its own or navigated to within a Frame.
        /// </summary>
    public sealed partial class MainPage : Page
    {
        CC2650SensorTag.CC2650SensorTagUnpairedBTConnectivity Connectivity = null;

        public MainPage()
        {
            this.InitializeComponent();

            Connectivity = new CC2650SensorTag.CC2650SensorTagUnpairedBTConnectivity();

            MP = this;
        }

        //public IReadOnlyList<GattCharacteristic> sensorCharacteristicList = null;
    
        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Connectivity.Start();
        }

        private void Button_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            Connectivity.Stop();
        }

        private async void Button_Tapped_2(object sender, TappedRoutedEventArgs e)
        {
            await Logging.StartLogging();
        }

        private void Button_Tapped_3(object sender, TappedRoutedEventArgs e)
        {
            Logging.StopLogging();
        }


        static MainPage MP;

        public static async Task PrependTextStatic(string str)
        {
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

        private async void Button_Tapped_5(object sender, TappedRoutedEventArgs e)
        {
            var res = await Connectivity.TagServices.PropertyServices.GetBatteryLevel();
            if (res != null)
                if (res.Length > 0)
                    await PrependTextStatic(string.Format("Battery Level: {0}\r\n", res[0]));
        }

        private async void Button_Tapped_6(object sender, TappedRoutedEventArgs e)
        {
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
    }
    
}
