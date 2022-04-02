using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace streamdeck_pia
{
    [PluginActionId("streamdeck-pia.pluginaction")]
    public class PluginAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.piactlLocation = "C:\\Program Files\\Private Internet Access\\piactl.exe";
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "piactlLocation")]
            public string piactlLocation { get; set; }

        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            if(VPNIsRunning())
            {
                Connection.SetStateAsync(1);
            }
            else
            {
                Connection.SetStateAsync(0);
            }
            
        }

        private void SetVPNState()
        {
            if (VPNIsRunning())
            {
                RunPIACommand("disconnect");
                Connection.SetStateAsync(0);
            }
            else
            {
                RunPIACommand("connect");
                Connection.SetStateAsync(1);
            }
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            SetVPNState();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        private bool VPNIsRunning()
        {
            return RunPIACommand("get connectionstate") == "Connected";
        }

        private string RunPIACommand(string arguments)
        {
            Process piactl = new Process();
            ProcessStartInfo piasi = new ProcessStartInfo();
            piasi.WindowStyle = ProcessWindowStyle.Hidden;
            piasi.FileName = settings.piactlLocation;
            piasi.Arguments = arguments;
            piasi.RedirectStandardOutput = true;
            piasi.UseShellExecute = false;
            piactl.StartInfo = piasi;
            piactl.Start();
            string result = piactl.StandardOutput.ReadToEnd();
            return result.Trim('\r','\n');
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {

            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}