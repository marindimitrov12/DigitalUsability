namespace Services.Interfaces;

public interface INetworkHelper
{
    (string from, string to) GetIpRange(string cidr);
}