using System;
using System.Linq;

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
    /// Gets Apple Music API base URL.
    /// </summary>
    public static string AppleMusicApiBaseUrl => "https://api.music.apple.com/v1";

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
    /// Get Apple Music ID from Apple Music URL.
    /// The URL format is always "https://music.apple.com/us/[item type]/[ID]".
    /// </summary>
    /// <param name="url">Apple Music URL.</param>
    /// <returns>Item ID.</returns>
    public static string GetIdFromUrl(string url)
    {
        return url.Split('/').LastOrDefault(string.Empty);
    }
}
