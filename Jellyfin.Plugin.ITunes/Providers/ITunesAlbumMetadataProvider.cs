using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes album metadata provider.
/// </summary>
public class ITunesAlbumMetadataProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesAlbumMetadataProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ITunesAlbumMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesAlbumMetadataProvider>();
    }

    /// <inheritdoc />
    public string Name => "Apple Music";

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var artistName = searchInfo.AlbumArtists.FirstOrDefault(string.Empty);
        var url = GetSearchUrl(searchInfo.Name, artistName);
        var iTunesAlbumDto = await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesAlbumDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);

        if (iTunesAlbumDto is null || iTunesAlbumDto.ResultCount < 1)
        {
            _logger.LogDebug("No results for url {Url}", url);
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var results = new List<RemoteSearchResult>();
        foreach (var albumDto in iTunesAlbumDto.Results)
        {
            if (albumDto.CollectionViewUrl is null)
            {
                continue;
            }

            var albumImages = GetAlbumImages(albumDto);
            results.Add(new RemoteSearchResult
            {
                Name = albumDto.CollectionName,
                ImageUrl = albumImages.FirstOrDefault((string.Empty, ImageType.Thumb)).Item1
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(info.Name))
        {
            _logger.LogDebug("Empty album name, skipping");
            return EmptyResult();
        }

        var albumArtist = info.AlbumArtists.FirstOrDefault(string.Empty);
        return await GetMetadataInternal(GetSearchUrl(info.Name, albumArtist), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private static MetadataResult<MusicAlbum> EmptyResult()
    {
        return new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum(),
            HasMetadata = false
        };
    }

    private static string GetSearchUrl(string albumName, string artistName)
    {
        var encodedName = Uri.EscapeDataString($"{artistName} {albumName}");
        return $"https://itunes.apple.com/search?term={encodedName}&media=music&entity=album&attribute=albumTerm";
    }

    private async Task<MetadataResult<MusicAlbum>> GetMetadataInternal(string url, CancellationToken cancellationToken)
    {
        var iTunesAlbumDto = await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesAlbumDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);

        if (iTunesAlbumDto is null || iTunesAlbumDto.ResultCount < 1)
        {
            _logger.LogDebug("No results for url {Url}", url);
            return EmptyResult();
        }

        var result = iTunesAlbumDto.Results.First();
        if (result.CollectionViewUrl is null)
        {
            _logger.LogDebug("No album links found in results");
            return EmptyResult();
        }

        _logger.LogDebug("Using {Url} to fetch album data", result.CollectionViewUrl);

        var musicAlbum = new MusicAlbum
        {
            Name = result.CollectionName,
            MusicArtist = { Name = result.ArtistName }
        };

        return new MetadataResult<MusicAlbum>
        {
            Item = musicAlbum,
            HasMetadata = true,
            RemoteImages = GetAlbumImages(result)
        };
    }

    private static List<(string Url, ImageType Type)> GetAlbumImages(AlbumResult album)
    {
        if (album.ArtworkUrl100 is null)
        {
            return new List<(string Url, ImageType Type)>();
        }

        var primaryImageUrl = album.ArtworkUrl100.Replace("100x100bb", "1400x1400bb", StringComparison.OrdinalIgnoreCase);
        return new List<(string Url, ImageType Type)>
        {
            (primaryImageUrl, ImageType.Primary),
            (album.ArtworkUrl100, ImageType.Thumb)
        };
    }
}
