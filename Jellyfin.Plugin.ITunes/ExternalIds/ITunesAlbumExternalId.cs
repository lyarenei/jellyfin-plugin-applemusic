using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.ExternalIds;

/// <summary>
/// Apple Music album external ID.
/// </summary>
public class ITunesAlbumExternalId : IExternalId
{
    /// <summary>
    /// Apple Music country code.
    /// </summary>
    private string _countryCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumExternalId"/> class.
    /// </summary>
    public ITunesAlbumExternalId()
    {
        _countryCode = "us";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumExternalId"/> class.
    /// </summary>
    /// <param name="countryCode">Country code for Apple Music.</param>
    public ITunesAlbumExternalId(string countryCode)
    {
        _countryCode = countryCode;
    }

    /// <inheritdoc />
    public string ProviderName => PluginUtils.PluginName;

    /// <inheritdoc />
    public string Key => ProviderKey.ITunesAlbum.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Album;

    /// <inheritdoc />
    public string UrlFormatString => PluginUtils.AppleMusicBaseUrl + $"/{_countryCode}/album/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio or MusicAlbum;
}
