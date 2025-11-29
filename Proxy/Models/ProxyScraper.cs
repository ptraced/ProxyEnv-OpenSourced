namespace Proxy.Models
{
    public class Datum
    {
        public string _id { get; set; }
        public string ip { get; set; }
        public string anonymityLevel { get; set; }
        public string asn { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public DateTime created_at { get; set; }
        public bool google { get; set; }
        public string isp { get; set; }
        public int lastChecked { get; set; }
        public double latency { get; set; }
        public string org { get; set; }
        public string port { get; set; }
        public List<string> protocols { get; set; }
        public object region { get; set; }
        public int responseTime { get; set; }
        public int speed { get; set; }
        public DateTime updated_at { get; set; }
        public object workingPercent { get; set; }
        public double upTime { get; set; }
        public int upTimeSuccessCount { get; set; }
        public int upTimeTryCount { get; set; }
    }

    public class ProxyScraper
    {
        public List<Datum> data { get; set; }
        public int total { get; set; }
        public int page { get; set; }
        public int limit { get; set; }
    }
}
