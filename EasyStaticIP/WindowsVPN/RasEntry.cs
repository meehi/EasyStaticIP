using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace WindowsVPN
{
    public class RasEntry
    {
        #region Constants

        public static string UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public const string AllUserFolder = @"c:\ProgramData\Microsoft\Network\Connections\Pbk\";
        public const string AppDataFolder = @"\Microsoft\Network\Connections\Pbk\";
        public const string PhoneBookFileName = "rasphone.pbk";

        #endregion

        #region Constructors

        public RasEntry()
        {
        }

        #endregion

        #region Properties

        public string FriendlyName { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }

        public bool Connected
        {
            get
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in interfaces)
                        if (adapter.Name.ToUpper() == this.FriendlyName.ToUpper())
                            return (adapter.OperationalStatus == OperationalStatus.Up);
                }
                return false;
            }
        }

        #endregion

        #region Public methods

        public bool Connect()
        {
            if (this.Connected)
                return true;

            Process proc = new Process();
            proc.StartInfo.FileName = "rasdial.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Arguments = "\"" + this.FriendlyName + "\" \"" + this.UserName + "\" \"" + this.Password + "\"";
            if (this.Domain != string.Empty)
                proc.StartInfo.Arguments += " /DOMAIN:" + this.Domain;
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.ErrorDataReceived += Proc_ErrorDataReceived;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return this.Connected;
        }

        public bool Disconnect()
        {
            if (!this.Connected)
                return true;

            Process proc = new Process();
            proc.StartInfo.FileName = "rasdial.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Arguments = "\"" + this.FriendlyName + "\" /DISCONNECT";
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.ErrorDataReceived += Proc_ErrorDataReceived;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return !this.Connected;
        }


        public static ObservableCollection<RasEntry> LoadPhoneBookEntries()
        {
            ObservableCollection<RasEntry> result = new ObservableCollection<RasEntry>();
            ReadPhoneBook(UserFolder + AppDataFolder + PhoneBookFileName, result);
            ReadPhoneBook(AllUserFolder + PhoneBookFileName, result);
            return result;
        }

        #endregion

        #region Private methods

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
        }

        private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
        }

        private static void ReadPhoneBook(string path, ObservableCollection<RasEntry> entries)
        {
            if (File.Exists(path))
            {
                IniFile rasPhoneBook = new IniFile(path);
                List<string> sections = rasPhoneBook.IniReadSections();
                foreach (string section in sections)
                {
                    IniFile vpn_settings = new IniFile(path);
                    RasEntry phoneBook = new RasEntry() { FriendlyName = section, Host = vpn_settings.IniReadValue(section, "PhoneNumber") };
                    if (!entries.Contains(phoneBook))
                        entries.Add(phoneBook);
                }
            }
        }

        #endregion
    }
}
