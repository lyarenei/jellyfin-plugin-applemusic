using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MusicBrainz;

/// <summary>
/// Finds the Apple Music ID of an artist through MusicBrainz instead of a name search.
/// The artist's MusicBrainz ID is taken from its tracks' tags (or Jellyfin's stored ID), then the
/// Apple Music link on the MusicBrainz artist page is used. If the artist page has none, the
/// artist's albums are tried: their Apple Music album link or barcode is looked up on iTunes.
/// </summary>
public partial class AppleMusicIdResolver
{
    private const string MusicBrainzBaseUrl = "https://musicbrainz.org/ws/2";
    private const string ITunesLookupUrl = "https://itunes.apple.com/lookup";
    private const string UserAgent = "Jellyfin-Plugin-AppleMusic ( https://github.com/lyarenei/jellyfin-plugin-applemusic )";
    private const int MaxAlbumsToCheck = 3;

    private static readonly string[] _barcodeStorefronts = ["us", "gb", "de"];
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    // MusicBrainz allows one request per second, the iTunes lookup API about 20 per minute.
    private static readonly Throttle _musicBrainzThrottle = new(TimeSpan.FromMilliseconds(1100));
    private static readonly Throttle _iTunesThrottle = new(TimeSpan.FromMilliseconds(3100));
    private static readonly ConcurrentDictionary<string, (DateTime Expires, JsonElement? Data)> _cache = new();

    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleMusicIdResolver"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager, used to find the artist's tracks and albums.</param>
    /// <param name="logger">Logger.</param>
    public AppleMusicIdResolver(IHttpClientFactory httpClientFactory, ILibraryManager libraryManager, ILogger logger)
    {
        _httpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Resolve the Apple Music ID of an artist in the library.
    /// </summary>
    /// <param name="artist">The artist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The match. <see cref="AppleMusicArtistMatch.AppleMusicId"/> is null if none was found.</returns>
    public async Task<AppleMusicArtistMatch> ResolveArtistAsync(MusicArtist artist, CancellationToken cancellationToken)
    {
        // Prefer the MBID from the artist's own tracks: Jellyfin's stored artist MBID may come from a
        // MusicBrainz name search, which has the same ambiguity problem as an Apple Music name search.
        var musicBrainzId = GetMusicBrainzIdFromTracks(artist);
        var source = "track tags";
        if (musicBrainzId is null && artist.TryGetProviderId(MetadataProvider.MusicBrainzArtist, out var storedId))
        {
            musicBrainzId = storedId;
            source = "Jellyfin artist metadata";
        }

        var match = await ResolveMusicBrainzArtistAsync(musicBrainzId, source, artist.Name, cancellationToken);
        if (match.AppleMusicId is not null || match.IsCollaboration)
        {
            return match;
        }

        var albumMatch = await ResolveFromAlbumsAsync(artist, musicBrainzId, cancellationToken);
        return albumMatch is null ? match : albumMatch with { MusicBrainzName = match.MusicBrainzName };
    }

    /// <summary>
    /// Resolve the Apple Music ID for an artist lookup (e.g. the identify dialog).
    /// </summary>
    /// <param name="info">Artist lookup info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The match. <see cref="AppleMusicArtistMatch.AppleMusicId"/> is null if none was found.</returns>
    public async Task<AppleMusicArtistMatch> ResolveArtistAsync(ArtistInfo info, CancellationToken cancellationToken)
    {
        var artist = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.MusicArtist],
                Name = info.Name,
            })
            .OfType<MusicArtist>()
            .FirstOrDefault(a => string.Equals(a.Name, info.Name, StringComparison.OrdinalIgnoreCase));
        if (artist is not null)
        {
            return await ResolveArtistAsync(artist, cancellationToken);
        }

        var musicBrainzId = info.SongInfos
            .Select(song => song.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist))
            .Where(id => !string.IsNullOrEmpty(id))
            .GroupBy(id => id)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? info.GetProviderId(MetadataProvider.MusicBrainzArtist);
        return await ResolveMusicBrainzArtistAsync(musicBrainzId, "lookup info", info.Name, cancellationToken);
    }

    /// <summary>
    /// Get the Apple Music artist ID linked to a MusicBrainz artist.
    /// </summary>
    /// <param name="musicBrainzId">MusicBrainz artist ID. Null returns an empty match.</param>
    /// <param name="source">Where the MusicBrainz ID came from, for logging.</param>
    /// <param name="artistName">Name of the artist in Jellyfin, used to detect collaboration artists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The match.</returns>
    public async Task<AppleMusicArtistMatch> ResolveMusicBrainzArtistAsync(string? musicBrainzId, string source, string? artistName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(musicBrainzId) || !Guid.TryParse(musicBrainzId, out _))
        {
            return new AppleMusicArtistMatch(null, null, null, "no MusicBrainz ID");
        }

        var artist = await GetJsonAsync($"{MusicBrainzBaseUrl}/artist/{musicBrainzId}?inc=url-rels&fmt=json", _musicBrainzThrottle, cancellationToken);
        if (artist is null)
        {
            return new AppleMusicArtistMatch(null, musicBrainzId, null, $"MusicBrainz artist {musicBrainzId} not found");
        }

        var musicBrainzName = GetString(artist.Value, "name");

        // A collaboration artist like "A, B & C" carries A's MBID in its tags; A's image would be wrong for it.
        if (IsCollaborationOf(artistName, musicBrainzName))
        {
            _logger.LogInformation("Artist {Name} looks like a collaboration of MusicBrainz artist {MusicBrainzName}, not using its Apple Music ID", artistName, musicBrainzName);
            return new AppleMusicArtistMatch(null, musicBrainzId, musicBrainzName, "collaboration", IsCollaboration: true);
        }

        var appleIds = GetAppleIds(artist.Value, "artist");
        if (appleIds.Count > 1)
        {
            _logger.LogDebug("MusicBrainz artist {Id} links several Apple Music artists ({AppleIds}), using {Chosen}", musicBrainzId, string.Join(", ", appleIds), appleIds[0]);
        }

        return new AppleMusicArtistMatch(appleIds.FirstOrDefault(), musicBrainzId, musicBrainzName, $"MusicBrainz artist {musicBrainzId} (MBID from {source})");
    }

    private static bool IsCollaborationOf(string? artistName, string? musicBrainzName)
    {
        var name = Normalize(artistName);
        var mbName = Normalize(musicBrainzName);
        return mbName.Length > 0
               && name.StartsWith(mbName + " ", StringComparison.Ordinal)
               && CollaborationSeparatorRegex().IsMatch(artistName!);
    }

    private static string Normalize(string? name)
    {
        return NonAlphanumericRegex().Replace((name ?? string.Empty).ToLowerInvariant(), " ").Trim();
    }

    private static List<string> GetAppleIds(JsonElement entity, string kind)
    {
        if (!entity.TryGetProperty("relations", out var relations) || relations.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return relations.EnumerateArray()
            .Select(relation => relation.TryGetProperty("url", out var url) ? GetString(url, "resource") : null)
            .Where(resource => resource is not null)
            .Select(resource => (Resource: resource!, Match: AppleUrlRegex().Match(resource!)))
            .Where(x => x.Match.Success && x.Match.Groups["kind"].Value == kind)
            .GroupBy(x => x.Match.Groups["id"].Value)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Any(x => x.Resource.Contains("music.apple.com", StringComparison.Ordinal)) ? 0 : 1)
            .Select(group => group.Key)
            .ToList();
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static List<string> GetArtistIds(JsonElement? lookupResponse)
    {
        if (lookupResponse is null || !lookupResponse.Value.TryGetProperty("results", out var results))
        {
            return [];
        }

        return results.EnumerateArray()
            .Where(result => result.TryGetProperty("artistId", out var id) && id.ValueKind == JsonValueKind.Number)
            .Select(result => result.GetProperty("artistId").GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();
    }

    private string? GetMusicBrainzIdFromTracks(MusicArtist artist)
    {
        var tracks = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            AlbumArtistIds = [artist.Id],
            Recursive = true,
        });

        return tracks
            .Select(track => track.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist))
            .Where(id => !string.IsNullOrEmpty(id))
            .GroupBy(id => id)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
    }

    private async Task<AppleMusicArtistMatch?> ResolveFromAlbumsAsync(MusicArtist artist, string? musicBrainzId, CancellationToken cancellationToken)
    {
        var releaseIds = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.MusicAlbum],
                AlbumArtistIds = [artist.Id],
                Recursive = true,
            })
            .Select(album => album.GetProviderId(MetadataProvider.MusicBrainzAlbum))
            .Where(id => !string.IsNullOrEmpty(id) && Guid.TryParse(id, out _))
            .Distinct()
            .Take(MaxAlbumsToCheck)
            .ToList();

        var votes = new Dictionary<string, int>();
        foreach (var releaseId in releaseIds)
        {
            var release = await GetJsonAsync($"{MusicBrainzBaseUrl}/release/{releaseId}?inc=url-rels+artist-credits&fmt=json", _musicBrainzThrottle, cancellationToken);
            if (release is null)
            {
                continue;
            }

            // Only single-artist releases credited to this artist say anything about its Apple Music ID.
            var credits = release.Value.TryGetProperty("artist-credit", out var credit)
                ? credit.EnumerateArray().Select(c => c.TryGetProperty("artist", out var a) ? GetString(a, "id") : null).ToList()
                : [];
            if (credits.Count != 1 || (musicBrainzId is not null && credits[0] != musicBrainzId))
            {
                continue;
            }

            var artistIds = await GetArtistIdsForReleaseAsync(release.Value, cancellationToken);
            if (artistIds.Count == 1)
            {
                votes[artistIds[0]] = votes.GetValueOrDefault(artistIds[0]) + 1;
                if (votes[artistIds[0]] >= 2)
                {
                    break;
                }
            }
        }

        if (votes.Count != 1)
        {
            if (votes.Count > 1)
            {
                _logger.LogInformation("Albums of artist {Name} point to different Apple Music artists ({AppleIds}), not using them", artist.Name, string.Join(", ", votes.Keys));
            }

            return null;
        }

        var (appleId, count) = votes.First();
        return new AppleMusicArtistMatch(appleId, musicBrainzId, null, $"{count} album(s) on MusicBrainz");
    }

    private async Task<List<string>> GetArtistIdsForReleaseAsync(JsonElement release, CancellationToken cancellationToken)
    {
        foreach (var albumId in GetAppleIds(release, "album"))
        {
            var ids = GetArtistIds(await GetJsonAsync($"{ITunesLookupUrl}?id={albumId}", _iTunesThrottle, cancellationToken));
            if (ids.Count > 0)
            {
                return ids;
            }
        }

        var barcode = GetString(release, "barcode");
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return [];
        }

        foreach (var storefront in _barcodeStorefronts)
        {
            var url = $"{ITunesLookupUrl}?upc={Uri.EscapeDataString(barcode)}&entity=album&country={storefront}";
            var ids = GetArtistIds(await GetJsonAsync(url, _iTunesThrottle, cancellationToken));
            if (ids.Count > 0)
            {
                return ids;
            }
        }

        return [];
    }

    private async Task<JsonElement?> GetJsonAsync(string url, Throttle throttle, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(url, out var cached) && cached.Expires > DateTime.UtcNow)
        {
            return cached.Data;
        }

        JsonElement? data = null;
        try
        {
            await throttle.WaitAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                data = document.RootElement.Clone();
            }
            else if ((int)response.StatusCode != 404)
            {
                // Don't cache transient failures like rate limiting.
                _logger.LogWarning("Request to {Url} failed with status {StatusCode}", url, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request to {Url} failed", url);
            return null;
        }

        _cache[url] = (DateTime.UtcNow + _cacheDuration, data);
        return data;
    }

    [GeneratedRegex(@"apple\.com/(?:[a-z]{2}/)?(?<kind>artist|album)/(?:[^/?#]+/)?(?:id)?(?<id>\d+)")]
    private static partial Regex AppleUrlRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"[,&+/]|\s(?:feat\.?|ft\.?|featuring|with|and|und|con|y|x|vs\.?)\s", RegexOptions.IgnoreCase)]
    private static partial Regex CollaborationSeparatorRegex();

    private sealed class Throttle(TimeSpan interval) : IDisposable
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private DateTime _next = DateTime.MinValue;

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var delay = _next - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                _next = DateTime.UtcNow + interval;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose() => _lock.Dispose();
    }
}
