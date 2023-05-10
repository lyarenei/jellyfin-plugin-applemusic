using System;
using System.Globalization;
using Jellyfin.Plugin.ITunes.ExternalIds;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.ITunes.Utils;

/// <summary>
/// Various general plugin utilities.
/// </summary>
public static class PluginUtils
{
    /// <summary>
    /// Gets plugin name.
    /// </summary>
    public static string PluginName => "Apple Music";

    /// <summary>
    /// Gets Apple Music base URL.
    /// </summary>
    public static string AppleMusicBaseUrl => "https://music.apple.com/us";

    /// <summary>
    /// Replace size in image URL.
    /// </summary>
    /// <param name="url">URL to work with.</param>
    /// <param name="searchSize">Size to be replaced.</param>
    /// <param name="newSize">Size to replace with.</param>
    /// <returns>URL with new size..</returns>
    public static string ModifyImageUrlSize(string url, string searchSize, string newSize)
    {
        return url.Replace(searchSize, newSize, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get provider URL specified by <see cref="ProviderKey"/>.
    /// </summary>
    /// <param name="item">Item to get the provider URL for.</param>
    /// <param name="key">Kind of provider URL to get.</param>
    /// <returns>Item provider URL. Empty if not found.</returns>
    public static string GetProviderUrl(IHasProviderIds item, ProviderKey key)
    {
        var providerId = item.GetProviderId(key.ToString());
        if (providerId is null)
        {
            return string.Empty;
        }

        string? urlFormat = key switch
        {
            ProviderKey.ITunesAlbum => new ITunesAlbumExternalId().UrlFormatString,
            ProviderKey.ITunesAlbumArtist => new ITunesAlbumArtistExternalId().UrlFormatString,
            ProviderKey.ITunesArtist => new ITunesArtistExternalId().UrlFormatString,
            _ => null
        };

        return urlFormat is not null ? string.Format(CultureInfo.InvariantCulture, urlFormat, providerId) : string.Empty;
    }
}
