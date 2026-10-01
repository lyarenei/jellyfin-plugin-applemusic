namespace Jellyfin.Plugin.AppleMusic.MusicBrainz;

/// <summary>
/// Result of resolving an artist's Apple Music ID through MusicBrainz.
/// </summary>
/// <param name="AppleMusicId">Apple Music artist ID, or null if none was found.</param>
/// <param name="MusicBrainzId">MusicBrainz artist ID that was used, if any.</param>
/// <param name="MusicBrainzName">Artist name on MusicBrainz, a better search term than a folder name.</param>
/// <param name="Source">Human-readable description of how the ID was found, for logging.</param>
/// <param name="IsCollaboration">Whether the artist looks like a collaboration of the MusicBrainz artist.</param>
public record AppleMusicArtistMatch(
    string? AppleMusicId,
    string? MusicBrainzId,
    string? MusicBrainzName,
    string Source,
    bool IsCollaboration = false);
