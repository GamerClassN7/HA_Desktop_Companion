using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml.Linq;
using System.IO;

namespace GH_Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private static string appDir = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
            checkForUpdates("https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", "0.0.0.7");
        }

        internal async void checkForUpdates(string repository_url = "https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", string assemblyVersion = "0.0.0.7")
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            string stringTask = await client.GetStringAsync(repository_url);
            //MessageBox.Show(stringTask);

            JsonArray msg = JsonSerializer.Deserialize<JsonArray>(stringTask);
            for (int i = 0; i < msg.Count(); i++)
            {
                string versionString = string.Join("", new Regex("[0-9]").Matches(msg[i].AsObject()["tag_name"].ToString()));
                int versionNumber = Int32.Parse(versionString);
                
                //MessageBox.Show(msg[i].AsObject()["tag_name"].ToString());
                //MessageBox.Show(versionNumber.ToString());
                //MessageBox.Show(msg[i].AsObject()["assets"].AsArray()[0].AsObject()["browser_download_url"].ToString());

                string zipUrl = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["browser_download_url"].ToString();
                string zipName = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["name"].ToString();

                if (!Directory.Exists(appDir + "/cache/"))
                    Directory.CreateDirectory(appDir + "/cache/");

                using (var client = new WebClient())
                {
                    client.DownloadFile(zipUrl, appDir + "/cache/" + versionString+ "_"  + zipName);
                }
            }



            throw new NotImplementedException();
        }
    }
}
