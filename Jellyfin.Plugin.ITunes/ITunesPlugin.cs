using System;
using Jellyfin.Plugin.ITunes.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ITunes;

/// <summary>
/// The ITunes art plugin.
/// </summary>
public class ITunesPlugin : BasePlugin<PluginConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public ITunesPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Apple Music";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a9f62a44-fea5-46c3-ac26-55abba29c7c8");

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public static ITunesPlugin? Instance { get; private set; }
}
