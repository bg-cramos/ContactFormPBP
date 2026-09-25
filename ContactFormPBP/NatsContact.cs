using Newtonsoft.Json;

namespace ContactFormPBP
{
    public class NatsContact
    {
        [JsonProperty("CNAME")]
        public string CNAME { get; set; }

        [JsonProperty("FNAME")]
        public string FNAME { get; set; }

        [JsonProperty("LNAME")]
        public string LNAME { get; set; }

        [JsonProperty("EMAIL")]
        public string EMAIL { get; set; }

        [JsonProperty("PHONE")]
        public string PHONE { get; set; }

        [JsonProperty("ACTI")]
        public string ACTI { get; set; }
    }
}
