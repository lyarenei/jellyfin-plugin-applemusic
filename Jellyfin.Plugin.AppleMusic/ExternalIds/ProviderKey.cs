namespace Jellyfin.Plugin.AppleMusic.ExternalIds;

// Disable warning for missing enum item with value 0.
#pragma warning disable CA1008

/// <summary>
/// Apple Music provider keys.
/// </summary>
public enum ProviderKey
{
    /// <summary>
    /// Apple Music album provider ID.
    /// </summary>
    ITunesAlbum = 11448800,

    /// <summary>
    /// Apple Music album artist provider ID.
    /// </summary>
    ITunesAlbumArtist = 11448801,

    /// <summary>
    /// Apple Music artist provider ID.
    /// </summary>
    ITunesArtist = 11448802,
}
