using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string actualVersion = "";
        public string repositoryUrl = "";

        public string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public bool isLatest = false;
        public string zipUrl = "";
        public string zipName = "";
        public string versionString = "";
        private string assemblyString = "";


        protected override void OnStartup(StartupEventArgs e)
        {
            /*actualVersion = e.Args[0].ToString();
            repositoryUrl = e.Args[1].ToString();*/
            checkForUpdates("https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases", "1.0.0");

            base.OnStartup(e);
        }

        private void checkForUpdates(string repository_url = "", string assemblyVersion = "0.0.0")
        {
            assemblyString = string.Join("", new Regex("[0-9]").Matches(assemblyVersion));
            int assemblyNumber = Int32.Parse(assemblyString);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

                string stringTask = client.GetStringAsync(repository_url).Result;

                JsonArray msg = JsonSerializer.Deserialize<JsonArray>(stringTask);
                for (int i = 0; i < msg.Count(); i++)
                {
                    versionString = string.Join("", new Regex("[0-9]").Matches(msg[i].AsObject()["tag_name"].ToString()));
                    int versionNumber = Int32.Parse(versionString);

                    if (assemblyNumber < versionNumber)
                    {
                        isLatest = false;
                        zipUrl = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["browser_download_url"].ToString();
                        zipName = msg[i].AsObject()["assets"].AsArray()[0].AsObject()["name"].ToString();
                        break;
                    }
                }
            }
        }
    }
}
