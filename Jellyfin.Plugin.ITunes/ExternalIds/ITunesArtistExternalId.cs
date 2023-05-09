using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.ExternalIds;

/// <summary>
/// Apple Music album artist external ID.
/// </summary>
public class ITunesArtistExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => PluginUtils.PluginName;

    /// <inheritdoc />
    public string Key => ITunesProviderKey.Artist.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Artist;

    /// <inheritdoc />
    public string UrlFormatString => PluginUtils.AppleMusicBaseUrl + "/artist/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is MusicArtist;
}
