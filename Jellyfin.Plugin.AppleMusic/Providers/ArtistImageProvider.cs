using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.ExternalIds;
using Jellyfin.Plugin.AppleMusic.MetadataSources;
using Jellyfin.Plugin.AppleMusic.MusicBrainz;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
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
    private readonly AppleMusicIdResolver _idResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="source">Metadata source. If null, a default source will be used.</param>
    public ArtistImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<ArtistImageProvider>();
        _metadataSource = source ?? MetadataSourceFactory.Create(httpClientFactory, loggerFactory);
        _idResolver = new AppleMusicIdResolver(httpClientFactory, libraryManager, loggerFactory.CreateLogger<AppleMusicIdResolver>());
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType> { ImageType.Primary, ImageType.Backdrop };
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

        var appleMusicId = artist.GetProviderId(nameof(ProviderKey.ITunesArtist));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            _logger.LogInformation("Using ID {Id} for artist lookup", appleMusicId);
            var results = await GetImageById(appleMusicId, cancellationToken);
            _logger.LogInformation("Found {Count} images for artist ID {Id}", results.Count, appleMusicId);
            return results;
        }

        var match = await _idResolver.ResolveArtistAsync(artist, cancellationToken);
        if (match.AppleMusicId is not null)
        {
            _logger.LogInformation("Resolved Apple Music ID {Id} for artist {Name} via {Source}", match.AppleMusicId, artist.Name, match.Source);
            var results = await GetImageById(match.AppleMusicId, cancellationToken);
            if (results.Count > 0)
            {
                return results;
            }
        }

        // The MusicBrainz name is a better search term than the item name, which may be a folder name.
        var searchTerm = match.IsCollaboration ? artist.Name : match.MusicBrainzName ?? artist.Name;
        _logger.LogInformation("Apple Music artist ID is not available ({Reason}), using search with term {SearchTerm}", match.Source, searchTerm);

        var searchResults = await _metadataSource.SearchAsync(searchTerm, ItemType.Artist, cancellationToken);

        _logger.LogInformation("Found {Count} search results using term {SearchTerm}", searchResults.Count, searchTerm);

        return searchResults
            .OfType<ITunesArtist>()
            .SelectMany(GetRemoteImages);
    }

    private async Task<List<RemoteImageInfo>> GetImageById(string appleMusicId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Looking up artist by ID {Id}", appleMusicId);
        var artistData = await _metadataSource.GetArtistAsync(appleMusicId, cancellationToken);
        var images = artistData is null ? new List<RemoteImageInfo>() : GetRemoteImages(artistData).ToList();
        _logger.LogDebug("Found {Count} images for artist ID {Id}", images.Count, appleMusicId);
        return images;
    }

    private IEnumerable<RemoteImageInfo> GetRemoteImages(ITunesArtist artist)
    {
        if (!string.IsNullOrEmpty(artist.ImageUrl))
        {
            yield return new RemoteImageInfo
            {
                Height = 1400,
                Width = 1400,
                ProviderName = Name,
                ThumbnailUrl = PluginUtils.UpdateImageSize(artist.ImageUrl, "100x100cc"),
                Type = ImageType.Primary,
                Url = PluginUtils.UpdateImageSize(artist.ImageUrl, "1400x1400cc"),
            };
        }

        if (!string.IsNullOrEmpty(artist.BackdropImageUrl))
        {
            yield return new RemoteImageInfo
            {
                Height = artist.BackdropHeight,
                Width = artist.BackdropWidth,
                ProviderName = Name,
                ThumbnailUrl = artist.BackdropImageUrl.Replace($"/{artist.BackdropWidth}x{artist.BackdropHeight}sr.", "/400x200sr.", StringComparison.Ordinal),
                Type = ImageType.Backdrop,
                Url = artist.BackdropImageUrl,
            };
        }
    }
}
