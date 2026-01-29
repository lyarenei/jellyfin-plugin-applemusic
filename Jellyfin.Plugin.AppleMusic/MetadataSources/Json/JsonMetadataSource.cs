using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Requests;
using Jellyfin.Plugin.AppleMusic.Utils;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json;

/// <summary>
/// Apple Music JSON metadata source.
/// This source retrieves metadata from Apple Music's JSON API.
/// </summary>
public class JsonMetadataSource : IMetadataSource
{
    private readonly DefaultApiClient _apiClient;
    private readonly ILogger<JsonMetadataSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMetadataSource"/> class.
    /// </summary>
    /// <param name="apiClient">API client instance.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public JsonMetadataSource(DefaultApiClient apiClient, ILoggerFactory loggerFactory)
    {
        _apiClient = apiClient;
        _logger = loggerFactory.CreateLogger<JsonMetadataSource>();
    }

    /// <inheritdoc />
    public async Task<List<IITunesItem>> SearchAsync(string searchTerm, ItemType itemType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for {ItemType} with term: {SearchTerm}", itemType, searchTerm);

        var request = new SearchRequest
        {
            Term = searchTerm,
            Limit = 25,
            Types = itemType switch
            {
                ItemType.Album => new[] { "albums" },
                ItemType.Artist => new[] { "artists" },
                _ => new[] { "albums", "artists" },
            },
        };

        var response = await _apiClient.SearchAsync(request, cancellationToken);

        return itemType switch
        {
            ItemType.Album => ConvertAlbums(response.Albums).Cast<IITunesItem>().ToList(),
            ItemType.Artist => ConvertArtists(response.Artists).Cast<IITunesItem>().ToList(),
            _ => new List<IITunesItem>(),
        };
    }

    /// <inheritdoc />
    public async Task<ITunesAlbum?> GetAlbumAsync(string albumId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching album with ID: {AlbumId}", albumId);

        var result = await _apiClient.GetAlbumAsync(albumId, cancellationToken);
        if (result is null)
        {
            _logger.LogWarning("Album not found: {AlbumId}", albumId);
            return null;
        }

        return ConvertAlbums(new[] { result }).FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<ITunesArtist?> GetArtistAsync(string artistId, CancellationToken cancellationToken)
    {
        // Artist data from search results is already complete enough for metadata
        // Full implementation would call: /v1/catalog/us/artists/{artistId}
        throw new NotImplementedException("Use SearchAsync for artist discovery");
    }

    private List<ITunesAlbum> ConvertAlbums(IEnumerable<SearchResult> results)
    {
        return results.Select(r => new ITunesAlbum
        {
            Id = r.Id,
            Name = r.Attributes.Name,
            Url = r.Attributes.Url,
            ImageUrl = ResolveArtworkUrl(r.Attributes.Artwork?.Url),
            About = r.Attributes.EditorialNotes?.Standard ?? r.Attributes.EditorialNotes?.Short,
            ReleaseDate = ParseReleaseDate(r.Attributes.ReleaseDate),
            Artists = ConvertArtistsFromRelationships(r.Relationships),
        }).ToList();
    }

    private List<ITunesArtist> ConvertArtists(IEnumerable<SearchResult> results)
    {
        return results.Select(r => new ITunesArtist
        {
            Id = r.Id,
            Name = r.Attributes.Name,
            Url = r.Attributes.Url,
            ImageUrl = ResolveArtworkUrl(r.Attributes.Artwork?.Url),
            About = r.Attributes.EditorialNotes?.Standard ?? r.Attributes.EditorialNotes?.Short,
        }).ToList();
    }

    private List<ITunesArtist> ConvertArtistsFromRelationships(Relationships? relationships)
    {
        if (relationships?.Artists?.Data is null)
        {
            return new List<ITunesArtist>();
        }

        return ConvertArtists(relationships.Artists.Data);
    }

    private static string? ResolveArtworkUrl(string? templateUrl)
    {
        return string.IsNullOrEmpty(templateUrl) ? null : PluginUtils.ResolveArtworkUrl(templateUrl);
    }

    private static DateTime? ParseReleaseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        return DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
    }
}
