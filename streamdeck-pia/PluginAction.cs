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
using System.Timers;

namespace streamdeck_pia
{
    [PluginActionId("streamdeck-pia.pluginaction")]
    public class PluginAction : PluginBase
    {
        Process piactl = new Process();
        string vpnState = string.Empty;

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

        private void MonitorPIAVPNState()
        {

            ProcessStartInfo piasi = new ProcessStartInfo();
            piasi.WindowStyle = ProcessWindowStyle.Hidden;
            piasi.FileName = settings.piactlLocation;
            piasi.Arguments = "monitor connectionstate";
            piasi.RedirectStandardOutput = true;
            piasi.UseShellExecute = false;
            piactl.StartInfo = piasi;
            piactl.OutputDataReceived += Piactl_OutputDataReceived;
            piactl.Start();
            piactl.BeginOutputReadLine();
        }

        private void Piactl_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string newstate = e.Data.Trim('\r', '\n');
            vpnState = newstate;
            switch (newstate)
            {
                case "Connected":        
                    Connection.SetStateAsync(1);
                    break;
                case "Disconnected":
                    Connection.SetStateAsync(0);
                    break;
                default:
                    break;
            }
        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Debugger.Launch();

            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            MonitorPIAVPNState();
        }


        private void SetVPNState()
        {
            switch(vpnState)
            {
                case "Connected":
                    RunPIACommand("disconnect");
                    break;
                case "Disconnected":
                    RunPIACommand("connect");
                    break;
                default:
                    break;

            }
        }

        public override void Dispose()
        {
            piactl.WaitForExit();
            piactl.Close();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            SetVPNState();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
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
            result = result.Trim('\r', '\n');
            return result;
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() 
        {

        }

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