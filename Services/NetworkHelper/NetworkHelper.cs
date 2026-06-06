using System.Net;
using Services.Interfaces;

namespace Services.NetworkHelper;

public class NetworkHelper: INetworkHelper
{
    public (string from, string to) GetIpRange(string cidr)
    {
        try
        {
            ValidateCidrFormat(cidr);
            var (ipString, prefixLength) = ParseCidr(cidr);
            ValidatePrefixLength(prefixLength);
            var parts=cidr.Split("/");
            var ip=IPAddress.Parse(parts[0]);
            int prefix=int.Parse(parts[1]);

            uint ipUint = BitConverter.ToUInt32(ip.GetAddressBytes().Reverse().ToArray(), 0);
            
            uint mask=prefix==0?0:uint.MaxValue<<(32-prefix);
            uint network=ipUint&mask;
            uint broadcast=network| ~mask;

            return (
                new IPAddress(BitConverter.GetBytes(network).Reverse().ToArray()).ToString(),
                new IPAddress(BitConverter.GetBytes(broadcast).Reverse().ToArray()).ToString()
                );
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)when(ex is FormatException || ex is OverflowException)
        {
            throw new ArgumentException("Invalid CIDR format",ex);
        }
    }
    private static string UintToIp(uint ip)
    {
        byte[]bytes=BitConverter.GetBytes(ip);
        if(BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes).ToString();
    }

    private static uint IpToUint(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        if(BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static uint CalculateMask(int prefixLength)
    {
        return prefixLength == 0 ? 0 : uint.MaxValue << (32-prefixLength);
    }

    private static void ValidatePrefixLength(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentException("Invalid CIDR prefix length. Must be between 0 and 32.");
    }

    private static (string ipString,int prefixLength) ParseCidr(string cidr)
    {
        var parts=cidr.Split('/');
        string ipString = parts[0];

        if (!int.TryParse(parts[1], out int prefixLength))
            throw new ArgumentException("Invalid CIDR prefix length. Must be between 0 and 32.");
        return (ipString,prefixLength);
    }
    private static void ValidateCidrFormat (string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
           throw new ArgumentException("CIDR cannot be null or empty.");
        var parts=cidr.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid CIDR format.Expected format: 'xxx.xxx.xxx/xx'");
    }

   
}