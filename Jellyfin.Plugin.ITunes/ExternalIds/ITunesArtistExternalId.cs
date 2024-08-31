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
    /// <summary>
    /// Apple Music country code.
    /// </summary>
    private string _countryCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistExternalId"/> class.
    /// </summary>
    public ITunesArtistExternalId()
    {
        _countryCode = "us";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistExternalId"/> class.
    /// </summary>
    /// <param name="countryCode">Country code for Apple Music.</param>
    public ITunesArtistExternalId(string countryCode)
    {
        _countryCode = countryCode;
    }

    /// <inheritdoc />
    public string ProviderName => PluginUtils.PluginName;

    /// <inheritdoc />
    public string Key => ProviderKey.ITunesArtist.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Artist;

    /// <inheritdoc />
    public string UrlFormatString => PluginUtils.AppleMusicBaseUrl + $"/{_countryCode}/artist/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is MusicArtist;
}
