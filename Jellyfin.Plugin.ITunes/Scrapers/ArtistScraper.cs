using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Apple Music artist metadata scraper.
/// </summary>
public class ArtistScraper : IScraper<ArtistScraper>
{
    private const string ImageXPath = "//meta[@property='og:image']/@content";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArtistScraper> _logger;
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistScraper"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ArtistScraper(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ArtistScraper>();
        _config = AngleSharp.Configuration.Default.WithDefaultLoader();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(string searchUrl, CancellationToken cancellationToken)
    {
        var imageList = new List<RemoteImageInfo>();
        var searchData = await Search(searchUrl, cancellationToken).ConfigureAwait(false);
        if (searchData is null || searchData.ResultCount < 1)
        {
            _logger.LogInformation("No results found for url: {Url}", searchUrl);
            return imageList;
        }

        var artistResult = searchData.Results.FirstOrDefault();
        if (artistResult is null)
        {
            _logger.LogError("No artists found even though reported count is {Count}", searchData.ResultCount);
            return imageList;
        }

        if (artistResult.ArtistLinkUrl is null)
        {
            _logger.LogDebug("No artist link found");
            return imageList;
        }

        _logger.LogDebug("Using {Url} to get artist images", artistResult.ArtistLinkUrl);
        var page = await OpenPage(artistResult.ArtistLinkUrl).ConfigureAwait(false);
        var imageUrl = page.Head.SelectSingleNode(ImageXPath)?.TextContent;
        if (imageUrl is null)
        {
            _logger.LogError("No image URL found in {Url}", artistResult.ArtistLinkUrl);
            return imageList;
        }

        _logger.LogDebug("Using {Url} to get image URLs", imageUrl);

        // The artwork size can vary quite a bit, but for our uses, 1400x1400 should be plenty.
        // https://artists.apple.com/support/88-artist-image-guidelines
        var thumbImageUrl = imageUrl.Replace("1200x630cw", "100x100cc", StringComparison.OrdinalIgnoreCase);
        var primaryImageUrl = imageUrl.Replace("1200x630cw", "1400x1400cc", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Primary image URL: {Url}", primaryImageUrl);
        _logger.LogDebug("Thumb image URL: {Url}", thumbImageUrl);

        imageList.Add(new RemoteImageInfo
        {
            ProviderName = "Apple Music",
            Url = primaryImageUrl,
            Type = ImageType.Primary,
            ThumbnailUrl = thumbImageUrl,
            Height = 1400,
            Width = 1400
        });

        return imageList;
    }

    private async Task<ITunesArtistDto?> Search(string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Using {Url} for image search", url);
        return await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesArtistDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IDocument> OpenPage(string url)
    {
        var context = BrowsingContext.New(_config);
        return await context.OpenAsync(url).ConfigureAwait(false);
    }
}
