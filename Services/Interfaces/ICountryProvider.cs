namespace Services.Interfaces;

public interface ICountryProvider
{
    Task<Dictionary<string,string>>LoadCountriesAsync();
}