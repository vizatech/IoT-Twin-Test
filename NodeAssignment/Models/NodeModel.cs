
namespace NodeAssignment.Models
{
    public class ReportedNode
    {
        public string addr { get; set; }
        public int status { get; set; }
        public int rssi { get; set; }
        public int snr { get; set; }
        public dynamic fw_version { get; set; }
        public dynamic pwd_info { get; set; }
        public string hw_id { get; set; }
        public int sensors { get; set; }
        public int dropped_packets { get; set; }
    }

    public class DesiredNode
    {
        public DesiredNode()
        {
            this.dev_eui = "0000000000000000";
            this.activation_type = "A";
            this.class_ = "C";
            this.node_config_etc = "";
            this.device_id = "";
        }
        public string dev_eui { get; set; }
        public string activation_type { get; set; }
        public string class_ { get; set; }
        public string node_config_etc { get; set; }
        public string device_id { get; set; }
    }
}