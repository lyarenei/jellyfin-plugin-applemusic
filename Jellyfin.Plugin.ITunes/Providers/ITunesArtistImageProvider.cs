using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.ExternalIds;
using Jellyfin.Plugin.ITunes.MetadataServices;
using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using IMetadataService = Jellyfin.Plugin.ITunes.MetadataServices.IMetadataService;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes artist image provider.
/// </summary>
public class ITunesArtistImageProvider : IRemoteImageProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistImageProvider> _logger;
    private readonly IMetadataService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="service">Metadata service provider. If null, a default instance will be used.</param>
    public ITunesArtistImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataService? service = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesArtistImageProvider>();
        _service = service ?? new AppleMusicMetadataService(loggerFactory);
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
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicArtist artist)
        {
            _logger.LogDebug("Provided item is not an artist, cannot continue");
            return new List<RemoteImageInfo>();
        }

        var results = await GetUrlsForScraping(artist, cancellationToken).ConfigureAwait(false);
        var infos = new List<RemoteImageInfo>();
        foreach (var url in results)
        {
            var data = await _service.Scrape(url, ItemType.Artist).ConfigureAwait(false);
            if (data?.ImageUrl is null)
            {
                _logger.LogDebug("Failed to scrape artist on {Url}", url);
                continue;
            }

            var info = new RemoteImageInfo
            {
                Height = 1200,
                Width = 1200,
                ProviderName = Name,
                ThumbnailUrl = PluginUtils.ModifyImageUrlSize(data.ImageUrl, "1200x630cw", "100x100cc"),
                Type = ImageType.Primary,
                Url = PluginUtils.ModifyImageUrlSize(data.ImageUrl, "1200x630cw", "1400x1400cc")
            };

            infos.Add(info);
        }

        return infos;
    }

    private async Task<ICollection<string>> GetUrlsForScraping(MusicArtist artist, CancellationToken cancellationToken)
    {
        var providerUrl = PluginUtils.GetProviderUrl(artist, ProviderKey.ITunesArtist);
        if (string.IsNullOrEmpty(providerUrl))
        {
            _logger.LogDebug("Provider URL is empty, falling back to search");
            return await _service.Search(artist.Name, ItemType.Artist, cancellationToken).ConfigureAwait(false);
        }

        return new List<string> { providerUrl };
    }
}
