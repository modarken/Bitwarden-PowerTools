using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitwarden.Core.Models;
using System.Text.Json.Serialization;

namespace Bitwarden.Core
{
    public class BitwardenClientConfiguration
    {
        [JsonConverter(typeof(ProtectedDataConverter))] public string? base_address { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? email { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? master_key { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? client_id { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? client_secret { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? refresh_token { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? device_name { get; set; }
        [JsonConverter(typeof(ProtectedDataConverter))] public string? device_identifier { get; set; }
    }

    public class BitwardenClient
    {
        private readonly BitwardenClientConfiguration _configuration;

        public BitwardenClient(BitwardenClientConfiguration configuration)
        {
            _configuration = configuration;
        }

    }
}
