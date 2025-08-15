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
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.Providers;

/// <summary>
/// Apple Music artist image provider.
/// </summary>
public class ArtistImageProvider : IRemoteImageProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArtistImageProvider> _logger;
    private readonly IMetadataSource _metadataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="source">Metadata source. If null, a default source will be used.</param>
    public ArtistImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<ArtistImageProvider>();
        _metadataSource = source ?? new WebMetadataSource(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType> { ImageType.Primary };
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicArtist;

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicArtist artist)
        {
            _logger.LogDebug("Provided item is not an artist, cannot continue");
            return new List<RemoteImageInfo>();
        }

        var appleMusicId = artist.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            var artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
            if (artistData?.ImageUrl is not null)
            {
                _logger.LogDebug("Found artist image by ID {Id}", appleMusicId);
                return new List<RemoteImageInfo>
                {
                    new()
                    {
                        Height = 1400,
                        Width = 1400,
                        ProviderName = Name,
                        ThumbnailUrl = PluginUtils.UpdateImageSize(artistData.ImageUrl, "100x100cc"),
                        Type = ImageType.Primary,
                        Url = PluginUtils.UpdateImageSize(artistData.ImageUrl, "1400x1400cc"),
                    },
                };
            }
        }

        _logger.LogDebug("Could not obtain Apple Music artist ID, falling back to search");
        var searchResults = await _metadataSource.SearchAsync(artist.Name, ItemType.Artist, cancellationToken);
        if (searchResults.Count == 0)
        {
            _logger.LogDebug("No search results found for term {SearchTerm}", artist.Name);
            return new List<RemoteImageInfo>();
        }

        _logger.LogDebug("Found {Count} search results for term {SearchTerm}", searchResults.Count, artist.Name);

        return searchResults
            .Where(sr => sr is ITunesArtist amArtist && !string.IsNullOrEmpty(amArtist.ImageUrl))
            .Select(amArtist => new RemoteImageInfo
            {
                Height = 1400,
                Width = 1400,
                ProviderName = Name,
                ThumbnailUrl = PluginUtils.UpdateImageSize(amArtist.ImageUrl!, "100x100cc"),
                Type = ImageType.Primary,
                Url = PluginUtils.UpdateImageSize(amArtist.ImageUrl!, "1400x1400cc"),
            });
    }
}
