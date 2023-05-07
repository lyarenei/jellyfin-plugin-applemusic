using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Apple Music album metadata scraper.
/// </summary>
public class AlbumScraper : IScraper<MusicAlbum>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlbumScraper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumScraper"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public AlbumScraper(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<AlbumScraper>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(string searchTerm, CancellationToken cancellationToken)
    {
        var imageList = new List<RemoteImageInfo>();
        var searchData = await Search(searchTerm, cancellationToken).ConfigureAwait(false);
        if (searchData is null || searchData.ResultCount < 1)
        {
            _logger.LogInformation("No results found for url: {Url}", searchTerm);
            return imageList;
        }

        foreach (var result in searchData.Results)
        {
            if (result.ArtworkUrl100 is null)
            {
                _logger.LogDebug("No album image URL");
                continue;
            }

            var image = GetRemoteImage(result.ArtworkUrl100);
            imageList.Add(image);
        }

        return imageList;
    }

    /// <inheritdoc />
    public Task<MetadataResult<MusicAlbum>?> GetMetadata(string searchTerm, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<ITunesAlbumDto?> Search(string searchTerm, CancellationToken cancellationToken)
    {
        var encodedTerm = Uri.EscapeDataString(searchTerm);
        var searchUrl = $"https://itunes.apple.com/search?term={encodedTerm}&media=music&entity=album&attribute=albumTerm";
        _logger.LogInformation("Using {Url} for image search", searchUrl);
        return await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesAlbumDto>(new Uri(searchUrl), cancellationToken)
            .ConfigureAwait(false);
    }

    private RemoteImageInfo GetRemoteImage(string url)
    {
        _logger.LogDebug("Using {Url} to get image URLs", url);

        // The artwork size can vary quite a bit, but for our uses, 1400x1400 should be plenty.
        // https://artists.apple.com/support/88-artist-image-guidelines
        var primaryImageUrl = url.Replace("100x100bb", "1400x1400cc", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Primary image URL: {Url}", primaryImageUrl);
        _logger.LogDebug("Thumb image URL: {Url}", url);

        return new RemoteImageInfo
        {
            ProviderName = "Apple Music",
            Url = primaryImageUrl,
            Type = ImageType.Primary,
            ThumbnailUrl = url,
            Height = 1400,
            Width = 1400
        };
    }
}
