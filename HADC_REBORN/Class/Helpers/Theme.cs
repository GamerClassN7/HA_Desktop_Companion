using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace HADC_REBORN.Class.Helpers
{
    class Theme
    {
        public static bool isLightTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i > 0;
        }

        public static bool isColorLight(Windows.UI.Color clr)
        {
            return ((5 * clr.G) + (2 * clr.R) + clr.B) > (8 * 128);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        public static void setTheme(bool isLight = true)
        {
            Icon iconResource = HADC_REBORN.Resource.ha_icon;
            if (isLight)
            {
                iconResource = HADC_REBORN.Resource.ha_icon_dark;
            }

            setIcons(iconResource);
        }

        private static void setIcons(Icon iconResource)
        {
            App.icon.Icon = iconResource;
            MainWindow main = App.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (main != null)
            {
                main.Icon = Imaging.CreateBitmapSourceFromHBitmap(iconResource.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }
    }
}
