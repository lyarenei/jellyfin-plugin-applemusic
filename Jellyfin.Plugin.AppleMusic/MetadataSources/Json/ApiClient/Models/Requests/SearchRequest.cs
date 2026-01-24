using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Requests;

/// <summary>
/// Apple Music JSON API search request.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Gets album fields to include in response.
    /// </summary>
    public IEnumerable<string> AlbumFields => new List<string>([
        "artistName", "artistUrl", "artwork", "contentRating", "editorialNotes", "name", "url",
    ]);

    /// <summary>
    /// Gets artist fields to include in response.
    /// </summary>
    public IEnumerable<string> ArtistFields => new List<string>([
        "artwork", "editorialNotes", "name", "url",
    ]);

    /// <summary>
    /// Gets entities to include in albums.
    /// </summary>
    public IEnumerable<string> IncludeInAlbums => new List<string>([
        "artists",
    ]);

    /// <summary>
    /// Gets language.
    /// </summary>
    public string Language => "en-US";

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets resources to omit from the response.
    /// </summary>
    public IEnumerable<string> OmitResources => new List<string>([
        "autos",
    ]);

    /// <summary>
    /// Gets platform.
    /// </summary>
    public string Platform => "web";

    /// <summary>
    /// Gets relations to include in results for albums.
    /// </summary>
    public IEnumerable<string> RelateInAlbums => new List<string>([
        "artists",
    ]);

    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    public required string Term { get; set; }

    /// <summary>
    /// Gets or sets types to search for.
    /// </summary>
    public required IEnumerable<string> Types { get; set; }

    /// <summary>
    /// Builds the query string for this search request.
    /// </summary>
    /// <returns>Query string without leading '?'.</returns>
    public string ToQueryString()
    {
        var parameters = new List<string>
        {
            $"term={Uri.EscapeDataString(Term)}",
            $"types={string.Join(",", Types)}",
            $"limit={Limit}",
            $"l={Language}",
            $"platform={Platform}",
            $"fields[albums]={string.Join(",", AlbumFields)}",
            $"fields[artists]={string.Join(",", ArtistFields)}",
            $"include[albums]={string.Join(",", IncludeInAlbums)}",
            $"relate[albums]={string.Join(",", RelateInAlbums)}",
            $"omit[resource]={string.Join(",", OmitResources)}",
        };

        return string.Join("&", parameters);
    }
}
