using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.MetadataServices;

/// <summary>
/// Service wrapper for interacting with Apple Music website.
/// </summary>
public class AppleMusicMetadataService : IMetadataService
{
    private const string ImageXPath = "//meta[@property='og:image']/@content";
    private const string InfoXPath = "//p[@data-testid='truncate-text']";

    private readonly ILogger<AppleMusicMetadataService> _logger;
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleMusicMetadataService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AppleMusicMetadataService(ILogger<AppleMusicMetadataService> logger)
    {
        _logger = logger;
        _config = AngleSharp.Configuration.Default.WithDefaultLoader();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> Search(string searchTerm, ItemType type, CancellationToken cancellationToken)
    {
        var encodedTerm = Uri.EscapeDataString(searchTerm);
        var searchUrl = $"http://music.apple.com/us/search?term={encodedTerm}";

        _logger.LogDebug("Using {Url} to get data", searchUrl);

        var document = await OpenPage(searchUrl).ConfigureAwait(false);
        var categoryLabel = GetCategoryLabel(type);
        var nodes = document.Body.SelectNodes(SearchResultXPath(categoryLabel));
        return from IHtmlAnchorElement elemNode in nodes select elemNode.Href;
    }

    private async Task<IDocument> OpenPage(string url)
    {
        var context = BrowsingContext.New(_config);
        return await context.OpenAsync(url).ConfigureAwait(false);
    }

    private static string SearchResultXPath(string label) => $"//div[@data-testid='section-container'][@aria-label='{label}']//li//a";

    private static string GetCategoryLabel(ItemType type)
    {
        return type switch
        {
            ItemType.Album => "Albums",
            ItemType.Artist => "Artists",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
