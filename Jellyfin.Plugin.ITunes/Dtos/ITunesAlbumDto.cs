#pragma warning disable CA1819

using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// The ALbum DTO.
    /// </summary>
    public class ITunesAlbumDto
    {
        /// <summary>
        /// Gets or sets the result count.
        /// </summary>
        /// <value>The result count.</value>
        [JsonPropertyName("resultCount")]
        public long ResultCount { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>The results.</value>
        [JsonPropertyName("results")]
        public Result[]? Results { get; set; }
    }
}
