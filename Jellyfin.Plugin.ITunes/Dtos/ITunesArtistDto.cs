using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// The artist DTO.
    /// </summary>
    public class ITunesArtistDto
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
        public ArtistResult[] Results { get; set; } = Array.Empty<ArtistResult>();
    }
}
