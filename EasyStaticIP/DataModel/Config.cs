using Helper;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace DataModel
{
    public class Config : INotifyPropertyChanged
    {
        #region Contants

        private static string SETTINGS_PATH = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName + @"\Local\EasyStaticIP\setting.json";
        private static string MAGIC_KEY = "kjklsd324cxiow";

        #endregion

        #region Properties

        private string _host;
        public string Host { get { return _host; } set { _host = value; NotifyPropertyChanged(); } }
        private string _username;
        public string Username { get { return _username; } set { _username = value; NotifyPropertyChanged(); } }
        private string _password;
        public string Password { get { return _password; } set { _password = value; NotifyPropertyChanged(); } }
        private string _selectedVpnFriendlyName;
        public string SelectedVpnFriendlyName { get { return _selectedVpnFriendlyName; } set { _selectedVpnFriendlyName = value; NotifyPropertyChanged(); } }
        private string _vpnUsername;
        public string VpnUsername { get { return _vpnUsername; } set { _vpnUsername = value; NotifyPropertyChanged(); } }
        private string _vpnPassword;
        public string VpnPassword { get { return _vpnPassword; } set { _vpnPassword = value; NotifyPropertyChanged(); } }
        private bool _autoStartWithWindows;
        public bool AutoStartWithWindows { get { return _autoStartWithWindows; } set { _autoStartWithWindows = value; NotifyPropertyChanged(); } }
        private bool _serverMode;
        public bool ServerMode { get { return _serverMode; } set { _serverMode = value; NotifyPropertyChanged(); NotifyPropertyChanged("ComputersEnabled"); } }

        private ObservableCollection<string> _vpnConnections;
        [JsonIgnore]
        public ObservableCollection<string> VpnConnections { get { return _vpnConnections; } set { _vpnConnections = value; NotifyPropertyChanged(); } }

        #endregion

        #region Contructors

        public Config()
        {
            this.VpnConnections = new ObservableCollection<string>
            {
                ""
            };
        }

        #endregion

        #region Public methods

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void ReadConfig()
        {
            if (!Directory.Exists(Path.GetDirectoryName(SETTINGS_PATH)))
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
            if (!File.Exists(SETTINGS_PATH))
            {
                Globals.Settings = new Config() { AutoStartWithWindows = true };
                File.WriteAllText(SETTINGS_PATH, JsonConvert.SerializeObject(Globals.Settings));
            }
            else
                Globals.Settings = JsonConvert.DeserializeObject<Config>(File.ReadAllText(SETTINGS_PATH));

            UnprotectFields();
            SetAutoStart();
        }

        public static void WriteConfig()
        {
            if (!Directory.Exists(Path.GetDirectoryName(SETTINGS_PATH)))
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));

            ProtectFields();
            File.WriteAllText(SETTINGS_PATH, JsonConvert.SerializeObject(Globals.Settings));
            UnprotectFields();
            SetAutoStart();
        }

        #endregion

        #region Private methods

        private static void UnprotectFields()
        {
            Globals.Settings.Password = Unprotect(Globals.Settings.Password);
            Globals.Settings.VpnPassword = Unprotect(Globals.Settings.VpnPassword);
        }

        private static void ProtectFields()
        {
            Globals.Settings.Password = Protect(Globals.Settings.Password);
            Globals.Settings.VpnPassword = Protect(Globals.Settings.VpnPassword);
        }

        private static string Unprotect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return Encoding.Default.GetString(ProtectedData.Unprotect(Convert.FromBase64String(text),
                                                                      Encoding.Default.GetBytes(MAGIC_KEY),
                                                                      DataProtectionScope.CurrentUser));
        }

        private static string Protect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return Convert.ToBase64String(ProtectedData.Protect(Encoding.Default.GetBytes(text),
                                                                Encoding.Default.GetBytes(MAGIC_KEY),
                                                                DataProtectionScope.CurrentUser));
        }

        private static void SetAutoStart()
        {
            string startupFolder = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName + @"\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\";
            if (Globals.Settings.AutoStartWithWindows)
            {
                string[] lines = new string[3];
                lines[0] = "Set WshShell = CreateObject(\"WScript.Shell\")";
                lines[1] = string.Format("WshShell.Run chr(34) & \"{0}\" & Chr(34), 0", System.Reflection.Assembly.GetExecutingAssembly().Location);
                lines[2] = "Set WshShell = Nothing";
                File.WriteAllLines(startupFolder + "EasyStaticIP.vbs", lines);
            }
            else
            {
                if (File.Exists(startupFolder + "EasyStaticIP.vbs"))
                    File.Delete(startupFolder + "EasyStaticIP.vbs");
            }
        }

        #endregion
    }
}
