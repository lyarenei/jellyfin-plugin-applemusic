using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes artist image provider.
/// </summary>
public class ITunesArtistImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public ITunesArtistImageProvider(IHttpClientFactory httpClientFactory, ILogger<ITunesArtistImageProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string Name => "Apple Music";

    /// <summary>
    /// Gets the provider order.
    /// </summary>
    // After fanart
    public int Order => 1;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary
        };
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var artist = (MusicArtist)item;
        var list = new List<RemoteImageInfo>();

        if (!string.IsNullOrEmpty(artist.Name))
        {
            var searchQuery = artist.Name;

            var encodedName = Uri.EscapeDataString(searchQuery);

            var remoteImages = await GetImagesInternal($"https://itunes.apple.com/search?term=${encodedName}&media=music&entity=musicArtist&attribute=artistTerm", cancellationToken).ConfigureAwait(false);

            if (remoteImages is not null)
            {
                list.AddRange(remoteImages);
            }
        }

        return list;
    }

    private async Task<IEnumerable<RemoteImageInfo>> GetImagesInternal(string url, CancellationToken cancellationToken)
    {
        List<RemoteImageInfo> list = new List<RemoteImageInfo>();

        var iTunesAlbumDto = await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesArtistDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);

        if (iTunesAlbumDto is not null && iTunesAlbumDto.ResultCount > 0)
        {
            var result = iTunesAlbumDto.Results.First();
            if (result.ArtistLinkUrl is not null)
            {
                _logger.LogDebug("URL: {0}", result.ArtistLinkUrl);
                HtmlWeb web = new HtmlWeb();
                var doc = web.Load(new Uri(result.ArtistLinkUrl));
                var navigator = (HtmlAgilityPack.HtmlNodeNavigator)doc.CreateNavigator();

                var metaOgImage = navigator.SelectSingleNode("/html/head/meta[@property='og:image']/@content");

                if (metaOgImage != null)
                {
                    _logger.LogDebug("Node: {0} | {1}", metaOgImage.NodeType, metaOgImage.Value);

                    // The artwork size can vary quite a bit, but for our uses, 1400x1400 should be plenty.
                    // https://artists.apple.com/support/88-artist-image-guidelines
                    var image100 = metaOgImage.Value.Replace("1200x630cw", "100x100cc", StringComparison.OrdinalIgnoreCase);
                    var image1400 = metaOgImage.Value.Replace("1200x630cw", "1400x1400cc", StringComparison.OrdinalIgnoreCase);

                    list.Add(
                        new RemoteImageInfo
                        {
                            ProviderName = Name,
                            Url = image1400,
                            Type = ImageType.Primary,
                            ThumbnailUrl = image100
                        });
                }
            }
        }
        else
        {
            return list;
        }

        return list;
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item)
        => item is MusicArtist;
}
