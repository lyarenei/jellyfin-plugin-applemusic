using System;
using System.Collections.Generic;
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
/// The iTunes artist metadata provider.
/// </summary>
public class ArtistMetadataProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArtistMetadataProvider> _logger;
    private readonly IMetadataSource _metadataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="source">Metadata source. If null, a default source will be used.</param>
    public ArtistMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<ArtistMetadataProvider>();
        _metadataSource = source ?? new WebMetadataSource(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        var appleMusicId = searchInfo.GetProviderId(nameof(ProviderKey.ITunesArtist));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogInformation("Using ID {Id} for artist lookup", appleMusicId);
            var results = await GetArtistById(appleMusicId, cancellationToken);
            _logger.LogInformation("Found {Count} results for ID {Id}", results.Count, appleMusicId);
            return results;
        }

        _logger.LogInformation("Apple Music artist ID was not provided, using search");

        var searchTerm = searchInfo.Name;
        var searchResults = await _metadataSource.SearchAsync(searchTerm, ItemType.Artist, cancellationToken);

        _logger.LogInformation("Found {Count} search results using term {SearchTerm}", searchResults.Count, searchTerm);

        var allResults = new List<RemoteSearchResult>();
        foreach (var result in searchResults)
        {
            _logger.LogDebug("Processing search result: {ResultName}", result.Name);
            if (result is not ITunesArtist artist)
            {
                _logger.LogDebug("Search result is not artist, ignoring");
                continue;
            }

            allResults.Add(artist.ToRemoteSearchResult());
        }

        _logger.LogInformation("Total search results after processing: {Count}", searchResults.Count);
        return allResults;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        ITunesArtist? artistData;
        var appleMusicId = info.GetProviderId(nameof(ProviderKey.ITunesArtist));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogDebug("Using ID {Id} for artist metadata lookup", appleMusicId);
            artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
            if (artistData is null)
            {
                _logger.LogDebug("No artist data found using ID {Id}", appleMusicId);
                return EmptyMetadataResult();
            }
        }
        else
        {
            _logger.LogDebug("Apple Music artist ID is not available, cannot continue");
            return EmptyMetadataResult();
        }

        var metadataResult = new MetadataResult<MusicArtist>
        {
            Item = new MusicArtist
            {
                Name = artistData.Name,
                Overview = artistData.About,
            },
            HasMetadata = artistData.HasMetadata(),
        };

        if (artistData.ImageUrl is not null)
        {
            _logger.LogTrace("Adding image for artist {ArtistName}", artistData.Name);
            metadataResult.RemoteImages.Add((artistData.ImageUrl, ImageType.Primary));
        }

        _logger.LogDebug("Setting provider ID {Id} for artist {ArtistName}", artistData.Id, artistData.Name);
        metadataResult.Item.SetProviderId(nameof(ProviderKey.ITunesArtist), artistData.Id);
        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    private static MetadataResult<MusicArtist> EmptyMetadataResult()
    {
        return new MetadataResult<MusicArtist> { HasMetadata = false };
    }

    private async Task<List<RemoteSearchResult>> GetArtistById(string appleMusicId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting artist by ID {Id}", appleMusicId);
        var artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
        if (artistData is not null)
        {
            _logger.LogDebug("Found artist by ID {Id}", appleMusicId);
            return new List<RemoteSearchResult> { artistData.ToRemoteSearchResult() };
        }

        _logger.LogDebug("No artist found for ID {Id}", appleMusicId);
        return new List<RemoteSearchResult>();
    }
}
