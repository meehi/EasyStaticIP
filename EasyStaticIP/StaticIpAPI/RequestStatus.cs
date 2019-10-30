using Newtonsoft.Json;

namespace StaticIpAPI
{
    public class RequestStatus
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("request_vpn_status"), JsonConverter(typeof(BoolConverter))]
        public bool RequestVpnStatus { get; set; }
        [JsonProperty("remote_server_connected"), JsonConverter(typeof(BoolConverter))]
        public bool RemoteServerConnected { get; set; }
        [JsonProperty("request_ip_camera"), JsonConverter(typeof(BoolConverter))]
        public bool RequestIPCamera { get; set; }
    }
}
