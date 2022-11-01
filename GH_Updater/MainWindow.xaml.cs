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
using static System.Net.Mime.MediaTypeNames;
using Windows.Media.Protection.PlayReady;
using System.IO.Compression;

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
            //Environment.Exit(0);
        }

        public void checkForUpdates(string repository_url = "https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", string assemblyVersion = "0.0.1.9")
        {
            int assembylVersionNumber = Int32.Parse(string.Join("", new Regex("[0-9]").Matches(assemblyVersion)));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            string stringTask = client.GetStringAsync(repository_url).Result;
            JsonArray msg = JsonSerializer.Deserialize<JsonArray>(stringTask);
 
            for (int i = 0; i < msg.Count(); i++)
            {
                string versionString = string.Join("", new Regex("[0-9]").Matches(msg[i].AsObject()["tag_name"].ToString()));
                int versionNumber = Int32.Parse(versionString);

                string zipUrl = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["browser_download_url"].ToString();
                string zipName = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["name"].ToString();

                if (!Directory.Exists(appDir + "/cache/"))
                    Directory.CreateDirectory(appDir + "/cache/");

                if (assembylVersionNumber < versionNumber)
                {

                    MessageBoxResult result = MessageBox.Show("update Found v"+ versionString, "Do you wish ti perform update ? ", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(zipUrl, appDir + "/cache/" + versionString + "_" + zipName);
                        }

                        string zipPath = appDir + "/cache/" + versionString + "_" + zipName;
                        string extractPath = @"..\exxtract\";

        
                        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                        break;
                    }
                }
            }
        }
    }
}
