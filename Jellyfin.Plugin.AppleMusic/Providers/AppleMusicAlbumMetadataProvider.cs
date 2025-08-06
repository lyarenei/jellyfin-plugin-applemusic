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
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.Providers;

/// <summary>
/// Apple Music album metadata provider.
/// </summary>
public class AppleMusicAlbumMetadataProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ITunesAlbumMetadataProvider> _logger;
    private readonly IMetadataSource _metadataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleMusicAlbumMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="source">Metadata source instance. If null, a default instance will be used.</param>
    public AppleMusicAlbumMetadataProvider(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IMetadataSource? source = null)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _logger = loggerFactory.CreateLogger<ITunesAlbumMetadataProvider>();
        _metadataSource = source ?? new WebMetadataSource(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var searchResults = new List<RemoteSearchResult>();

        var appleMusicId = searchInfo.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            var albumData = await _metadataSource.GetAlbumAsync(appleMusicId, cancellationToken);
            if (albumData is not null)
            {
                _logger.LogDebug("Found album by ID {Id}", appleMusicId);
                searchResults.Add(albumData.ToRemoteSearchResult());
            }
        }

        // TODO: maybe add an option to continue with search even if album ID is available?

        var searchTerm = GetSearchTerm(searchInfo);
        if (string.IsNullOrEmpty(searchTerm))
        {
            _logger.LogInformation("Album name or artist name could not be obtained, giving up search due to poor accuracy");
            return searchResults;
        }

        _logger.LogInformation("Using search term {SearchTerm} for album {AlbumName}", searchTerm, searchInfo.Name);

        var results = await _metadataSource.SearchAsync(searchTerm, cancellationToken);
        if (results.Count == 0)
        {
            _logger.LogInformation("No search results found for term {SearchTerm}", searchTerm);
            return searchResults;
        }

        _logger.LogInformation("Found {Count} search results for term {SearchTerm}", results.Count, searchTerm);

        foreach (var result in results)
        {
            _logger.LogDebug("Processing search result: {ResultName}", result.Name);
            if (result is not ITunesAlbum album)
            {
                _logger.LogDebug("Search result is not an album, ignoring");
                continue;
            }

            if (searchInfo.Year is null ||
                album.ReleaseDate?.Year is null ||
                searchInfo.Year != album.ReleaseDate?.Year)
            {
                _logger.LogDebug("Album {AlbumName} does not match specified year, ignoring", album.Name);
                continue;
            }

            searchResults.Add(album.ToRemoteSearchResult());
        }

        _logger.LogInformation("Total search results after processing: {Count}", searchResults.Count);
        return searchResults;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        ITunesAlbum? albumData = null;
        var appleMusicId = info.GetProviderId(nameof(ProviderKey.ITunesAlbum));
        if (!string.IsNullOrEmpty(appleMusicId))
        {
            albumData = await _metadataSource.GetAlbumAsync(appleMusicId, cancellationToken);
        }

        if (albumData is null)
        {
            _logger.LogDebug("No album data found using ID {Id}", appleMusicId);
            return PluginUtils.EmptyResult<MusicAlbum>();
        }

        var artistNames = albumData.Artists.Select(ad => ad.Name).ToList();
        var metadataResult = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum
            {
                Name = albumData.Name,
                Overview = albumData.About,
                ProductionYear = albumData.ReleaseDate?.Year,
                Artists = artistNames,
                AlbumArtists = artistNames.Count != 0 ? new List<string> { artistNames.First() } : new List<string>(),
            },
            HasMetadata = albumData.HasMetadata(),
        };

        if (albumData.ImageUrl is not null)
        {
            metadataResult.RemoteImages.Add((albumData.ImageUrl, ImageType.Primary));
        }

        if (albumData.Artists.Any())
        {
            metadataResult.Item.SetProviderId(nameof(ProviderKey.ITunesAlbumArtist), albumData.Artists.First().Id);
        }

        metadataResult.Item.SetProviderId(nameof(ProviderKey.ITunesAlbum), albumData.Id);
        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync(new Uri(url), cancellationToken);
    }

    private string GetSearchTerm(AlbumInfo info)
    {
        var albumName = GetAlbumName(info);
        var albumArtist = GetArtistName(info);
        if (string.IsNullOrEmpty(albumArtist))
        {
            _logger.LogDebug("Album artist name is not available");
            return string.Empty;
        }

        return $"{albumArtist} {albumName}";
    }

    private string? GetAlbumName(AlbumInfo info)
    {
        var albumName = info.Name;
        var albumArtist = GetArtistName(info);
        if (string.Equals(albumName, albumArtist, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Album name is the same as album artist name, trying song info");
            return info.SongInfos.FirstOrDefault()?.Album ?? info.Name;
        }

        return albumName;
    }

    private string? GetArtistName(AlbumInfo info)
    {
        var albumArtist = info.AlbumArtists.Any() ? info.AlbumArtists[0] : null;
        if (!string.IsNullOrEmpty(albumArtist))
        {
            return albumArtist;
        }

        _logger.LogDebug("No artist name found in album artists, trying song info");
        var albumArtists = info.SongInfos.FirstOrDefault()?.AlbumArtists;
        return albumArtists is not null && albumArtists.Any() ? albumArtists[0] : null;
    }
}
