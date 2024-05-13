using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class
{
    class Keyboard
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

        public static void SendKey(string Key)
        {
            if (!App.yamlLoader.getConfigurationData().ContainsKey("keys"))
            {
                return;
            }

            try
            {
                App.log.writeLine("Typing key code: " + Key);
                uint ukey = (uint)System.Convert.ToUInt32(Key);
                keybd_event(ukey, 0, 0, 0);
                keybd_event(ukey, 0, 2, 0);

            }
            catch (Exception)
            {
                App.log.writeLine("ERROR Type Key");
            }
        }
    }
}
