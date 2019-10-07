using Newtonsoft.Json;

namespace DigitalOceanAPI
{
    public class RequestStatus
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("request_vpn_status")]
        public bool RequestVpnStatus { get; set; }
        [JsonProperty("remote_server_connected")]
        public bool RemoteServerConnected { get; set; }
    }
}
