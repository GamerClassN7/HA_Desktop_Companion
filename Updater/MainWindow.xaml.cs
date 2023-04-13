using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
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

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private static string appDir = Directory.GetCurrentDirectory();

        private bool isLatest = true;
        private string zipUrl = "";
        private string zipName = "";
        private string versionString = "";
        private string assemblyString = "";




        internal async void checkForUpdates(string repository_url = "https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", string assemblyVersion = "0.0.0")
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            string stringTask = await client.GetStringAsync(repository_url);
            MessageBox.Show(stringTask);

            JsonArray msg = JsonSerializer.Deserialize<JsonArray>(stringTask);
            for (int i = 0; i < msg.Count(); i++)
            {

                versionString = string.Join("", new Regex("[0-9]").Matches(msg[i].AsObject()["tag_name"].ToString()));
                int versionNumber = Int32.Parse(versionString);
                assemblyString = string.Join("", new Regex("[0-9]").Matches(assemblyVersion));
                int assemblyNumber = Int32.Parse(assemblyString);

                if (assemblyNumber < versionNumber)
                {
                    isLatest = false;
                    zipUrl = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["browser_download_url"].ToString();
                    zipName = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["name"].ToString();
                    MessageBox.Show(zipUrl);
                    break;
                }
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
          
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(appDir + "/cache/"))
                Directory.CreateDirectory(appDir + "/cache/");

            using (var client = new WebClient())
            {
                client.DownloadFile(zipUrl, appDir + "/cache/" + versionString + "_" + zipName);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            checkForUpdates("https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", "0.0.1");
            if (isLatest)
            {
                //System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
