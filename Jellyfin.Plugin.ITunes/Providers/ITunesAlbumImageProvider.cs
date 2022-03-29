using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunesArt.Providers
{
    /// <summary>
    /// The ITunes album image provider.
    /// </summary>
    public class ITunesAlbumImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ITunesAlbumImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ITunesAlbumImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public ITunesAlbumImageProvider(IHttpClientFactory httpClientFactory, ILogger<ITunesAlbumImageProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "Apple Music";

        /// <summary>
        /// Gets the plugin order.
        /// </summary>
        public int Order => 1; // After embedded provider

        /// <summary>
        /// Gets if <see cref="BaseItem"/> is supported.
        /// </summary>
        /// <param name="item">Object of the <see cref="BaseItem"/> class.</param>
        /// <returns>IF <see cref="BaseItem"/> is supported.</returns>
        public bool Supports(BaseItem item)
            => item is MusicAlbum;

        /// <summary>
        /// Gets the supported <see cref="ImageType"/> to a <see cref="BaseItem"/>.
        /// </summary>
        /// <param name="item">Object of the <see cref="BaseItem"/> class.</param>
        /// <returns>Supported <see cref="ImageType"/>.</returns>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        /// <summary>
        /// Gets the image from an URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds ITunes images to the current remote images of a <see cref="BaseItem"/>.
        /// </summary>
        /// <param name="item">Object of the <see cref="BaseItem"/> class.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of <see cref="RemoteImageInfo"/>.</returns>
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrEmpty(album.Name))
            {
                var searchQuery = album.Name;

                if (album.AlbumArtists.Count > 0)
                {
                    string[] terms =
                    {
                        album.AlbumArtists[0],
                        album.Name
                    };
                    searchQuery = string.Join(' ', terms);
                }

                var encodedName = Uri.EscapeDataString(searchQuery);
                var remoteImages = await GetImagesInternal($"https://itunes.apple.com/search?term={encodedName}&media=music&entity=album", cancellationToken).ConfigureAwait(false);

                if (remoteImages != null)
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
                .GetFromJsonAsync<ITunesAlbumDto>(new Uri(url), cancellationToken)
                .ConfigureAwait(false);

            if (iTunesAlbumDto != null && iTunesAlbumDto.Results != null)
            {
                foreach (Result result in iTunesAlbumDto.Results)
                {
                    if (result.ArtworkUrl100 != null)
                    {
                        // The artwork size can vary quite a bit, but for our uses, 1400x1400 should be plenty.
                        // https://artists.apple.com/support/88-artist-image-guidelines
                        var image1400 = result.ArtworkUrl100.Replace("100x100bb", "1400x1400bb", StringComparison.OrdinalIgnoreCase);

                        list.Add(
                            new RemoteImageInfo
                            {
                                ProviderName = Name,
                                Url = image1400,
                                Type = ImageType.Primary,
                                ThumbnailUrl = result?.ArtworkUrl100,
                                Height = 1400,
                                Width = 1400
                            });
                    }
                }
            }
            else
            {
                return Array.Empty<RemoteImageInfo>();
            }

            return list;
        }
    }
}
