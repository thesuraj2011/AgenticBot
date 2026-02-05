using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace AgenticBot.Plugins;

/// <summary>
/// Plugin providing web search and information retrieval capabilities.
/// Uses free APIs for demonstration purposes.
/// </summary>
public class WebSearchPlugin
{
    private readonly HttpClient _httpClient;

    public WebSearchPlugin(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [KernelFunction, Description("Gets current weather information for a city using free wttr.in API")]
    public async Task<string> GetWeatherAsync(
    [Description("City name to get weather for")] string city)
    {
  try
  {
      // Using wttr.in free weather API
    var response = await _httpClient.GetStringAsync($"https://wttr.in/{Uri.EscapeDataString(city)}?format=3");
            return response.Trim();
        }
   catch (Exception ex)
 {
            return $"Unable to fetch weather for {city}: {ex.Message}";
        }
    }

 [KernelFunction, Description("Gets a random interesting fact")]
    public async Task<string> GetRandomFactAsync()
    {
        try
        {
  // Using free uselessfacts API
      var response = await _httpClient.GetStringAsync("https://uselessfacts.jsph.pl/api/v2/facts/random?language=en");
        var json = JsonDocument.Parse(response);
            return json.RootElement.GetProperty("text").GetString() ?? "No fact available";
        }
   catch (Exception ex)
     {
            return $"Unable to fetch fact: {ex.Message}";
   }
 }

    [KernelFunction, Description("Gets a random joke")]
    public async Task<string> GetJokeAsync()
    {
    try
        {
    // Using free joke API
   var request = new HttpRequestMessage(HttpMethod.Get, "https://icanhazdadjoke.com/");
            request.Headers.Add("Accept", "application/json");
        var response = await _httpClient.SendAsync(request);
          var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
  return json.RootElement.GetProperty("joke").GetString() ?? "No joke available";
        }
        catch (Exception ex)
     {
            return $"Unable to fetch joke: {ex.Message}";
        }
    }

 [KernelFunction, Description("Gets information about a country")]
    public async Task<string> GetCountryInfoAsync(
        [Description("Country name to get information about")] string country)
    {
   try
        {
   var response = await _httpClient.GetStringAsync($"https://restcountries.com/v3.1/name/{Uri.EscapeDataString(country)}?fields=name,capital,population,region,languages,currencies");
         var json = JsonDocument.Parse(response);
     var countryData = json.RootElement[0];

            var name = countryData.GetProperty("name").GetProperty("common").GetString();
        var capital = countryData.GetProperty("capital")[0].GetString();
            var population = countryData.GetProperty("population").GetInt64();
            var region = countryData.GetProperty("region").GetString();

        return $"Country: {name}\nCapital: {capital}\nPopulation: {population:N0}\nRegion: {region}";
        }
        catch (Exception ex)
   {
            return $"Unable to fetch country info for {country}: {ex.Message}";
        }
    }
}
