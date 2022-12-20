namespace Bitwarden.Core;

public class BitwardenClient
{
    private readonly IBitwardenClientConfiguration _configuration;

    public BitwardenClient(IBitwardenClientConfiguration configuration)
    {
        _configuration = configuration;
    }
}