using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ITunes.Configuration
{
    /// <summary>
    /// The (empty) plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            LangCode = "us";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        /// <param name="langCode">Language code.</param>
        public PluginConfiguration(string langCode)
        {
            LangCode = langCode;
        }

        /// <summary>
        /// Gets or sets a language code to use in URLs.
        /// </summary>
        public string LangCode { get; set; }
    }
}
