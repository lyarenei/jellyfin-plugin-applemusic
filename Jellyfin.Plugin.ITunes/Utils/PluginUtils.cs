using System;

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
}
