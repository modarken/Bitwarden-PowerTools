using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitwarden.Core.Models;
using System.Text.Json.Serialization;

namespace Bitwarden.Core
{

    // TODO remove ProtectedDataConverter from this library, needs to go to Bitwarden.AutoType.Desktop
    public class DefaultBitwardenClientConfiguration : IBitwardenClientConfiguration
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
        private readonly IBitwardenClientConfiguration _configuration;

        public BitwardenClient(IBitwardenClientConfiguration configuration)
        {
            _configuration = configuration;
        }

    }
}
