using System;
using System.Globalization;
using Jellyfin.Plugin.AppleMusic.ExternalIds;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.AppleMusic.Utils;

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
    /// Update image resolution (width)x(height)(opts) in image URL.
    /// For example 1440x1440cc is an image with 1440x1440 resolution with ?center crop? from the source image.
    /// </summary>
    /// <param name="url">URL to work with.</param>
    /// <param name="newImageRes">New image resolution.</param>
    /// <returns>Updated URL.</returns>
    public static string UpdateImageSize(string url, string newImageRes)
    {
        var idx = url.LastIndexOf('/');
        if (idx < 0)
        {
            return url;
        }

        return string.Concat(url.AsSpan(0, idx + 1), newImageRes, ".jpg");
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
