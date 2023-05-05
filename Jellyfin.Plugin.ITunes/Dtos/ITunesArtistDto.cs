using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// The artist DTO.
    /// </summary>
    public class ITunesArtistDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ITunesArtistDto"/> class.
        /// </summary>
        public ITunesArtistDto()
        {
            ResultCount = 0;
            Results = new List<ArtistResult>();
        }

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
        public ICollection<ArtistResult> Results { get; set; }
    }
}
