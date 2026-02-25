using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MarketData.Adapter.Shared.Util;

public static class GuidUtility
{
    // RFC 4122 namespace UUID for URL (well-known & stable)
    private static readonly Guid Namespace = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    public static Guid Create(params object[] parts)
    {
        if (parts == null || parts.Length == 0)
            throw new ArgumentException("At least one value is required", nameof(parts));

        var name = string.Join("|", parts.Select(Normalize));

        return Create(Namespace, name);
    }

    private static Guid Create(Guid namespaceId, string name)
    {
        // Convert namespace UUID to network order
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // Compute SHA1 hash of namespace + name
        using var sha1 = SHA1.Create();
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var hash = sha1.ComputeHash(namespaceBytes.Concat(nameBytes).ToArray());

        // Build UUID from first 16 bytes of hash
        var guidBytes = hash[..16];

        // Set UUID version to 5
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);

        // Set variant to RFC 4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        SwapByteOrder(guidBytes);
        return new Guid(guidBytes);
    }

    private static string Normalize(object value) =>
        value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToUniversalTime().ToString("O"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
            decimal d => d.ToString("G", CultureInfo.InvariantCulture),
            double d => d.ToString("G", CultureInfo.InvariantCulture),
            float f => f.ToString("G", CultureInfo.InvariantCulture),
            _ => value.ToString()!.Trim()
        };

    private static void SwapByteOrder(byte[] guid)
    {
        void Swap(int a, int b) => (guid[a], guid[b]) = (guid[b], guid[a]);

        Swap(0, 3);
        Swap(1, 2);
        Swap(4, 5);
        Swap(6, 7);
    }
}
