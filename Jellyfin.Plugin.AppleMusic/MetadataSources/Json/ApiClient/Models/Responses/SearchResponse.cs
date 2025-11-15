using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;

/// <summary>
/// Search response from Apple Music JSON API.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchResponse"/> class.
    /// </summary>
    public SearchResponse()
    {
        Albums = new List<SearchResult>();
        Artists = new List<SearchResult>();
        ResponseData = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets or sets found albums.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<SearchResult> Albums { get; set; }

    /// <summary>
    /// Gets or sets found artists.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<SearchResult> Artists { get; set; }

    // Don't want to create a million useless classes, so
    // using extension parsing feature for custom JSON deserialization.
    // There is definitely a better way to do this. :shrug:

    private static JsonSerializerSettings SerializerSettings => new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
        ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
    };

    [JsonExtensionData]
    private Dictionary<string, object> ResponseData { get; set; }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        ResponseData.TryGetValue("albums", out var albumsObject);
        if (albumsObject is not null)
        {
            Albums = DeserializeData(albumsObject);
        }

        ResponseData.TryGetValue("artists", out var artistsObject);
        if (artistsObject is not null)
        {
            Artists = DeserializeData(artistsObject);
        }
    }

    private static List<SearchResult> DeserializeData(object albumsObject)
    {
        var jsonString = albumsObject.ToString() ?? string.Empty;
        var rawItems = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString, settings: SerializerSettings);
        if (rawItems is null)
        {
            return [];
        }

        var rawData = rawItems.GetValueOrDefault("data");
        if (rawData is null)
        {
            return [];
        }

        var rawDataJson = rawData.ToString() ?? string.Empty;
        return JsonConvert.DeserializeObject<List<SearchResult>>(rawDataJson, settings: SerializerSettings) ?? [];
    }
}
