using DataModel;
using DigitalOceanAPI;
using Helper;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WindowsVPN;

namespace EasyStaticIP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _close;
        private bool _requestInProgress;
        private ContextMenu _contextMenu;
        private DispatcherTimer _serverTimer;

        public Config ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            Config.ReadConfig();
            ViewModel = Globals.Settings;
            SetPasswordBoxes();
            ObservableCollection<RasEntry> vpnConnections = RasEntry.LoadPhoneBookEntries();
            int i = 0;
            foreach (RasEntry entry in vpnConnections)
            {
                i++;
                ViewModel.VpnConnections.Add(entry.FriendlyName);
                if (!string.IsNullOrEmpty(ViewModel.SelectedVpnFriendlyName))
                    if (entry.FriendlyName.ToUpper() == ViewModel.SelectedVpnFriendlyName.ToUpper())
                        comboVpnConnections.SelectedIndex = i;
            }
            Title = string.Format(Title, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            InitContextMenu();
            Hide();
            if (ViewModel.ServerMode)
                StartListening();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_close)
            {
                Hide();
                WindowState = WindowState.Minimized;
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            GetPasswordBoxes();
            Config.WriteConfig();
            Hide();
            WindowState = WindowState.Minimized;
            InitContextMenu();
            if (ViewModel.ServerMode)
            {
                StartListening();
                taskbarIcon.ShowBalloonTip("Saved", "Configuration saved, the server is now listening for VPN request", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            else
            {
                StopListening();
                taskbarIcon.ShowBalloonTip("Saved", "Configuration saved, right click on the tray icon if you want to go to VPN mode", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        }

        private void SetPasswordBoxes()
        {
            txtPassword.Password = ViewModel.Password;
            txtVpnPassword.Password = ViewModel.VpnPassword;
        }

        private void GetPasswordBoxes()
        {
            ViewModel.Password = txtPassword.Password;
            ViewModel.VpnPassword = txtVpnPassword.Password;
        }

        private void VpnConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
                ViewModel.SelectedVpnFriendlyName = e.AddedItems[0] as string;
        }

        private void Tray_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Topmost = true;
            Topmost = false;
        }

        private void InitContextMenu()
        {
            _contextMenu = new ContextMenu();
            if (!ViewModel.ServerMode)
            {
                MenuItem requestVpnOn = new MenuItem()
                {
                    Header = "Initialize VPN network",
                    CommandParameter = "ON"
                };
                requestVpnOn.Click += RequestVpn_OnOrOff_Click;
                _contextMenu.Items.Add(requestVpnOn);
                MenuItem requestVpnOff = new MenuItem()
                {
                    Header = "Close VPN network",
                    CommandParameter = "OFF"
                };
                requestVpnOff.Click += RequestVpn_OnOrOff_Click;
                _contextMenu.Items.Add(requestVpnOff);
                _contextMenu.Items.Add(new Separator());
            }
            MenuItem exitMenuItem = new MenuItem()
            {
                Header = "Exit"
            };
            exitMenuItem.Click += ExitMenuItem_Click;
            _contextMenu.Items.Add(exitMenuItem);
            taskbarIcon.ContextMenu = _contextMenu;
        }

        private async void RequestVpn_OnOrOff_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.SelectedVpnFriendlyName) ||
                string.IsNullOrEmpty(ViewModel.VpnUsername) ||
                string.IsNullOrEmpty(ViewModel.VpnPassword))
            {
                taskbarIcon.ShowBalloonTip("Error", "Missing VPN configuration", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                return;
            }

            if (_requestInProgress)
                return;

            Client client = new Client(ViewModel.Host, ViewModel.Username, ViewModel.Password);
            string mode = ((MenuItem)sender).CommandParameter as string;
            bool requestResult = false;
            string title = "";
            string message = "";
            string warning = "";
            switch (mode.ToUpper())
            {
                case "ON":
                    title = "Connected";
                    message = "VPN network initialized";
                    warning = "VPN network initialized but unable to connect to {0} VPN on this pc";
                    requestResult = client.RequestVpnOn();
                    break;
                case "OFF":
                    title = "Disconnected";
                    message = "VPN network closed";
                    warning = "VPN network closed but unable to disconnect from {0} VPN on this pc";
                    requestResult = client.RequestVpnOff();
                    break;
            }            

            if (requestResult)
            {
                _requestInProgress = true;
                taskbarIcon.ShowBalloonTip("Request sent", "Request has been sent to server", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                if (await client.CheckRequestStatusContinuouslyAsync(mode))
                {
                    RasEntry entry = new RasEntry()
                    {
                        FriendlyName = ViewModel.SelectedVpnFriendlyName,
                        UserName = ViewModel.VpnUsername,
                        Password = ViewModel.VpnPassword
                    };
                    bool vpnResult = false;
                    switch (mode.ToUpper())
                    {
                        case "ON":
                            vpnResult = entry.Connect();
                            break;
                        case "OFF":
                            vpnResult = entry.Disconnect();
                            break;
                    }
                    if (vpnResult)
                        taskbarIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    else
                        taskbarIcon.ShowBalloonTip("Warning", string.Format(warning, ViewModel.SelectedVpnFriendlyName), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                    _requestInProgress = false;
                }
                else
                {
                    _requestInProgress = false;
                    taskbarIcon.ShowBalloonTip("Error", client.LastError, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                }
            }
            else
            {
                _requestInProgress = false;
                taskbarIcon.ShowBalloonTip("Error", client.LastError, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
        }

        private void StartListening()
        {
            _serverTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(20)
            };
            _serverTimer.Tick += ServerTimer_Tick;
            _serverTimer.Start();
        }

        private void ServerTimer_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.SelectedVpnFriendlyName) || 
                string.IsNullOrEmpty(ViewModel.VpnUsername) ||
                string.IsNullOrEmpty(ViewModel.VpnPassword))
                return;

            try
            {
                RasEntry entry = new RasEntry()
                {
                    FriendlyName = ViewModel.SelectedVpnFriendlyName,
                    UserName = ViewModel.VpnUsername,
                    Password = ViewModel.VpnPassword
                };
                Client client = new Client(ViewModel.Host, ViewModel.Username, ViewModel.Password);
                RequestStatus result = client.CheckRequestStatus();
                if (result.RequestVpnStatus)
                {
                    if (!entry.Connected)
                        if (entry.Connect())
                            if (!result.RemoteServerConnected)
                                client.SetRemoteServerStatus("1");
                }
                else
                {
                    if (entry.Connected)
                        if (entry.Disconnect())
                            if (result.RemoteServerConnected)
                                client.SetRemoteServerStatus("0");
                }
            }
            catch
            {
            }
        }

        private void StopListening()
        {
            _serverTimer?.Stop();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _close = true;
            Close();
        }
    }
}
