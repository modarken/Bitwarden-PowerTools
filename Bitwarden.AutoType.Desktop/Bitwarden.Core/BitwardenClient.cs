using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Bitwarden.Core.Models;

namespace Bitwarden.Core
{

    public class BitwardenClientConfiguration
    {
        public string? base_address { get; set; }
        public string? email { get; set; }
        public string? master_key { get; set; }
        public string? client_id { get; set; }
        public string? client_secret { get; set; }
        public string? refresh_token { get; set; }
        public string? device_name { get; set; }
        public string? device_identifier { get; set; }
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
