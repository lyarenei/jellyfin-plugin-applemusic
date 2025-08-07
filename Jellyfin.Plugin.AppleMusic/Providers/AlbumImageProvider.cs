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
/// Apple Music album image provider.
/// </summary>
public class AlbumImageProvider : IRemoteImageProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlbumImageProvider> _logger;
    private readonly IMetadataSource _metadataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="source">Metadata service provider. If null, a default instance will be used.</param>
    public AlbumImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<AlbumImageProvider>();
        _metadataSource = source ?? new WebMetadataSource(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicAlbum;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType> { ImageType.Primary };
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicAlbum album)
        {
            _logger.LogDebug("Provided item is not an album, cannot continue");
            return new List<RemoteImageInfo>();
        }

        var appleMusicId = album.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            var albumData = await _metadataSource.GetAlbumAsync(appleMusicId, cancellationToken);
            if (albumData?.ImageUrl is not null)
            {
                _logger.LogDebug("Found album by ID {Id}", appleMusicId);
                return new List<RemoteImageInfo>
                {
                    new()
                    {
                        Height = 1400,
                        Width = 1400,
                        ProviderName = Name,
                        ThumbnailUrl = PluginUtils.UpdateImageSize(albumData.ImageUrl, "100x100cc"),
                        Type = ImageType.Primary,
                        Url = PluginUtils.UpdateImageSize(albumData.ImageUrl, "1400x1400cc"),
                    },
                };
            }
        }

        _logger.LogDebug("Could not obtain Apple Music ID, falling back to search");
        var term = GetSearchTerm(album);
        var searchResults = await _metadataSource.SearchAsync(term, cancellationToken);
        if (searchResults.Count == 0)
        {
            _logger.LogDebug("No search results found for term {SearchTerm}", term);
            return new List<RemoteImageInfo>();
        }

        _logger.LogDebug("Found {Count} search results for term {SearchTerm}", searchResults.Count, term);

        return searchResults
            .Where(sr => sr is ITunesAlbum amAlbum && !string.IsNullOrEmpty(amAlbum.ImageUrl))
            .Select(amAlbum => new RemoteImageInfo
            {
                Height = 1400,
                Width = 1400,
                ProviderName = Name,
                ThumbnailUrl = PluginUtils.UpdateImageSize(amAlbum.ImageUrl!, "100x100cc"),
                Type = ImageType.Primary,
                Url = PluginUtils.UpdateImageSize(amAlbum.ImageUrl!, "1400x1400cc"),
            });
    }

    private static string GetSearchTerm(MusicAlbum album)
    {
        var albumArtist = album.AlbumArtists.FirstOrDefault(string.Empty);
        return $"{albumArtist} {album.Name}";
    }
}
