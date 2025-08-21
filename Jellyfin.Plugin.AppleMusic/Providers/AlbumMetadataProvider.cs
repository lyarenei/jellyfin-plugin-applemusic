using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.ExternalIds;
using Jellyfin.Plugin.AppleMusic.MetadataSources;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.Providers;

/// <summary>
/// Apple Music album metadata provider.
/// </summary>
public class AlbumMetadataProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlbumMetadataProvider> _logger;
    private readonly IMetadataSource _metadataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="source">Metadata source. If null, a default source will be used.</param>
    public AlbumMetadataProvider(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<AlbumMetadataProvider>();
        _metadataSource = source ?? new WebMetadataSource(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var appleMusicId = searchInfo.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogInformation("Using ID {Id} for album lookup", appleMusicId);
            var results = await GetAlbumById(appleMusicId, cancellationToken);
            _logger.LogInformation("Found {Count} results for ID {Id}", results.Count, appleMusicId);
            return results;
        }

        _logger.LogInformation("Apple Music album ID was not provided, using search");

        var searchTerm = searchInfo.Name;
        var searchResults = await _metadataSource.SearchAsync(searchTerm, ItemType.Album, cancellationToken);

        _logger.LogInformation("Found {Count} search results using term {SearchTerm}", searchResults.Count, searchTerm);

        var allResults = new List<RemoteSearchResult>();
        foreach (var result in searchResults)
        {
            _logger.LogDebug("Processing search result: {ResultName}", result.Name);
            if (result is not ITunesAlbum album)
            {
                _logger.LogDebug("Search result is not an album, ignoring");
                continue;
            }

            // Check year only if the year was specified in the search form
            if (searchInfo.Year is not null && searchInfo.Year != album.ReleaseDate?.Year)
            {
                _logger.LogDebug("Album {AlbumName} does not match specified year, ignoring", album.Name);
                continue;
            }

            allResults.Add(album.ToRemoteSearchResult());
        }

        _logger.LogInformation("Total search results after processing: {Count}", searchResults.Count);
        return allResults;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        ITunesAlbum? albumData;
        var appleMusicId = info.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogDebug("Using ID {Id} for album metadata lookup", appleMusicId);
            albumData = await _metadataSource.GetAlbumAsync(appleMusicId, cancellationToken);
            if (albumData is null)
            {
                _logger.LogDebug("No album data found using ID {Id}", appleMusicId);
                return EmptyMetadataResult();
            }
        }
        else
        {
            _logger.LogDebug("Apple Music album ID is not available, cannot continue");
            return EmptyMetadataResult();
        }

        var artistNames = albumData.Artists.Select(ad => ad.Name).ToList();
        var metadataResult = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum
            {
                Name = albumData.Name,
                Overview = albumData.About,
                ProductionYear = albumData.ReleaseDate?.Year,
                Artists = artistNames,
                AlbumArtists = artistNames.Count != 0 ? new List<string> { artistNames.First() } : new List<string>(),
            },
            HasMetadata = albumData.HasMetadata(),
        };

        if (albumData.ImageUrl is not null)
        {
            _logger.LogTrace("Adding image for album {AlbumName}", albumData.Name);
            metadataResult.RemoteImages.Add((albumData.ImageUrl, ImageType.Primary));
        }

        var albumArtist = albumData.Artists.FirstOrDefault();
        if (albumArtist is not null)
        {
            _logger.LogDebug("Setting provider ID for album artist {ArtistName}", albumArtist.Name);
            metadataResult.Item.SetProviderId(nameof(ProviderKey.ITunesAlbumArtist), albumArtist.Id);
        }

        _logger.LogDebug("Setting provider ID for album {AlbumName}", albumData.Name);
        metadataResult.Item.SetProviderId(nameof(ProviderKey.ITunesAlbum), albumData.Id);
        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    private static MetadataResult<MusicAlbum> EmptyMetadataResult()
    {
        return new MetadataResult<MusicAlbum> { HasMetadata = false };
    }

    private async Task<List<RemoteSearchResult>> GetAlbumById(string appleMusicId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting album by ID {Id}", appleMusicId);
        var albumData = await _metadataSource.GetAlbumAsync(appleMusicId, cancellationToken);
        if (albumData is not null)
        {
            _logger.LogDebug("Found album by ID {Id}", appleMusicId);
            return new List<RemoteSearchResult> { albumData.ToRemoteSearchResult() };
        }

        _logger.LogDebug("No album found for ID {Id}", appleMusicId);
        return new List<RemoteSearchResult>();
    }
}
