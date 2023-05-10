using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.Dtos;
using Jellyfin.Plugin.ITunes.ExternalIds;
using Jellyfin.Plugin.ITunes.MetadataServices;
using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using IMetadataService = Jellyfin.Plugin.ITunes.MetadataServices.IMetadataService;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes album metadata provider.
/// </summary>
public class ITunesAlbumMetadataProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesAlbumMetadataProvider> _logger;
    private readonly IMetadataService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="service">Metadata service instance. If null, a default instance will be used.</param>
    public ITunesAlbumMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataService? service = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesAlbumMetadataProvider>();
        _service = service ?? new AppleMusicMetadataService(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = await GetUrlsForScraping(searchInfo, cancellationToken).ConfigureAwait(false);
        var searchResults = new List<RemoteSearchResult>();
        foreach (var result in results)
        {
            var scrapeResult = await _service.Scrape(result, ItemType.Album).ConfigureAwait(false);
            if (scrapeResult is not ITunesAlbum album)
            {
                _logger.LogDebug("Scrape result is not an album, ignoring");
                continue;
            }

            if (searchInfo.Year is not null &&
                album.ReleaseDate?.Year is not null &&
                searchInfo.Year != album.ReleaseDate?.Year)
            {
                _logger.LogDebug("Album does not match specified year, ignoring");
                continue;
            }

            searchResults.Add(album.ToRemoteSearchResult());
        }

        return searchResults;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        var results = await GetUrlsForScraping(info, cancellationToken).ConfigureAwait(false);
        if (!results.Any())
        {
            return EmptyResult();
        }

        var result = results.First();
        var scrapedAlbum = await _service.Scrape(result, ItemType.Album).ConfigureAwait(false);
        if (scrapedAlbum is not ITunesAlbum album)
        {
            _logger.LogDebug("Failed to scrape data from {Url}", result);
            return EmptyResult();
        }

        var artistNames = (from artist in album.Artists select artist.Name).ToList();
        var metadataResult = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum
            {
                Name = album.Name,
                Overview = album.About,
                ProductionYear = album.ReleaseDate?.Year,
                Artists = artistNames,
                AlbumArtists = artistNames.Any() ? new List<string> { artistNames.First() } : new List<string>()
            },
            HasMetadata = album.HasMetadata()
        };

        if (album.ImageUrl is not null)
        {
            metadataResult.RemoteImages.Add((album.ImageUrl, ImageType.Primary));
        }

        if (album.Artists.Any())
        {
            metadataResult.Item.SetProviderId(ProviderKey.ITunesAlbumArtist.ToString(), album.Artists.First().Id);
        }

        metadataResult.Item.SetProviderId(ProviderKey.ITunesAlbum.ToString(), album.Id);
        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private static MetadataResult<MusicAlbum> EmptyResult()
    {
        return new MetadataResult<MusicAlbum> { HasMetadata = false };
    }

    private string GetSearchTerm(AlbumInfo info)
    {
        var albumName = info.Name;
        var albumArtist = info.AlbumArtists.Any() ? info.AlbumArtists[0] : string.Empty;
        if (string.IsNullOrEmpty(albumArtist))
        {
            _logger.LogDebug("Album artist name is not available, giving up");
            return string.Empty;
        }

        if (string.Equals(info.Name, albumArtist, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Album name is the same as album artist name, trying album name from song");
            albumName = info.SongInfos.FirstOrDefault()?.Album ?? info.Name;
        }

        return $"{albumArtist} {albumName}";
    }

    private async Task<ICollection<string>> GetUrlsForScraping(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var providerUrl = PluginUtils.GetProviderUrl(searchInfo, ProviderKey.ITunesAlbum);
        if (!string.IsNullOrEmpty(providerUrl))
        {
            return new List<string> { providerUrl };
        }

        var searchTerm = GetSearchTerm(searchInfo);
        if (string.IsNullOrEmpty(searchTerm))
        {
            _logger.LogInformation("Could not get search term for {Album}", searchInfo.Name);
            return new List<string>();
        }

        _logger.LogDebug("Could not get a provider URL, falling back to search");
        return await _service.Search(searchTerm, ItemType.Album, cancellationToken).ConfigureAwait(false);
    }
}
