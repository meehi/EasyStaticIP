using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StaticIpAPI
{
    public class Client
    {
        #region Private variables

        private DispatcherTimer _requestStatusChecker;
        private CancellationTokenSource _cts;
        private Stopwatch _stopper;
        private static TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);
        private bool _requestStatusResult;
        private string _mode;

        #endregion

        #region Properties

        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string LastError { get; private set; }

        #endregion

        #region Constructors

        public Client(string host, string username, string password)
        {
            Host = host;
            Username = username;
            Password = password;
        }

        #endregion

        #region Public methods

        public bool RequestVpnOn()
        {
            string url = string.Format("{0}?method={1}", Host, "request_vpn_on");
            return TryGet(url, out _);
        }

        public bool RequestVpnOff()
        {
            string url = string.Format("{0}?method={1}", Host, "request_vpn_off");
            return TryGet(url, out _);
        }

        public bool SetRemoteServerStatus(string value)
        {
            string url = string.Format("{0}?method={1}&remote_value={2}", Host, "set_remote_server_status", value);
            return TryGet(url, out _);
        }

        public RequestStatus CheckRequestStatus()
        {
            string url = string.Format("{0}?method={1}", Host, "check_request_status");
            if (TryGet(url, out string responseString))
            {
                if (!string.IsNullOrEmpty(responseString))
                    return JsonConvert.DeserializeObject<RequestStatus>(responseString);
            }

            return default;
        }

        public async Task<bool> CheckRequestStatusContinuouslyAsync(string mode)
        {
            _mode = mode;
            _requestStatusResult = false;
            _cts = new CancellationTokenSource();
            _requestStatusChecker = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _requestStatusChecker.Tick += RequestStatusChecker_Tick;
            _stopper = new Stopwatch();
            _stopper.Start();
            _requestStatusChecker.Start();
            var task = Task.Run(() =>
            {
                _cts.Token.WaitHandle.WaitOne();
            }, _cts.Token);
            await task.ConfigureAwait(false);
            task.Wait();

            return _requestStatusResult;
        }

        #endregion

        #region Private methods

        private bool TryGet(string url, out string responseString)
        {
            responseString = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Credentials = new NetworkCredential(Username, Password);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                    responseString = reader.ReadToEnd();

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        private void RequestStatusChecker_Tick(object sender, EventArgs e)
        {
            if (_stopper.Elapsed >= TIMEOUT)
            {
                _requestStatusResult = false;
                LastError = "VPN connection timeout. The remote server was not responding in the given time";
                _requestStatusChecker.Stop();
                _stopper.Stop();
                _cts.Cancel();
            }
            else
            {
                RequestStatus result = CheckRequestStatus();
                switch (_mode.ToUpper())
                {
                    case "ON":
                        if (result.RemoteServerConnected)
                            _requestStatusResult = true;
                        break;
                    case "OFF":
                        if (!result.RemoteServerConnected)
                            _requestStatusResult = true;
                        break;
                }
                if (_requestStatusResult)
                {
                    _requestStatusChecker.Stop();
                    _stopper.Stop();
                    _cts.Cancel();
                }
            }
        }

        #endregion
    }
}
