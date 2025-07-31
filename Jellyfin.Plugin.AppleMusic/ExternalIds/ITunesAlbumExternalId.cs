using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.AppleMusic.ExternalIds;

/// <summary>
/// Apple Music album external ID.
/// </summary>
public class ITunesAlbumExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => PluginUtils.PluginName;

    /// <inheritdoc />
    public string Key => ProviderKey.ITunesAlbum.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Album;

    /// <inheritdoc />
    public string UrlFormatString => PluginUtils.AppleMusicBaseUrl + "/album/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio or MusicAlbum;
}
