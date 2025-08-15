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
        var searchResults = new List<RemoteSearchResult>();

        var appleMusicId = searchInfo.GetProviderId(nameof(ProviderKey.ITunesArtist));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogDebug("Found artist by ID {Id}", appleMusicId);
            var artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
            if (artistData is not null)
            {
                _logger.LogDebug("Found artist by ID {Id}", appleMusicId);
                searchResults.Add(artistData.ToRemoteSearchResult());
            }
        }

        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            _logger.LogInformation("Artist name is empty, cannot search");
            return searchResults;
        }

        _logger.LogInformation("Searching for artist {Name}", searchInfo.Name);

        var results = await _metadataSource.SearchAsync(searchInfo.Name, ItemType.Artist, cancellationToken);
        if (results.Count == 0)
        {
            _logger.LogInformation("No search results found for artist {Name}", searchInfo.Name);
            return searchResults;
        }

        _logger.LogDebug("Found {Count} search results for artist {Name}", results.Count, searchInfo.Name);

        foreach (var result in results)
        {
            _logger.LogDebug("Processing search result: {ResultName}", result.Name);
            if (result is not ITunesArtist artist)
            {
                _logger.LogWarning("Search result is not artist, ignoring");
                continue;
            }

            searchResults.Add(artist.ToRemoteSearchResult());
        }

        _logger.LogInformation("Total search results for artist {Name}: {Count}", searchInfo.Name, searchResults.Count);
        return searchResults;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        ITunesArtist? artistData = null;
        var appleMusicId = info.GetProviderId(nameof(ProviderKey.ITunesArtist));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
        }

        if (artistData is null)
        {
            _logger.LogDebug("No artist data using ID {Id}", appleMusicId);
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
            metadataResult.RemoteImages.Add((artistData.ImageUrl, ImageType.Primary));
        }

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
}
