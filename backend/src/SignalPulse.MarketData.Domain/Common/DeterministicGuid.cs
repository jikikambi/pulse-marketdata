using System.Security.Cryptography;
using System.Text;

namespace SignalPulse.MarketData.Domain.Common;

public static class DeterministicGuid
{
    public static Guid From(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}