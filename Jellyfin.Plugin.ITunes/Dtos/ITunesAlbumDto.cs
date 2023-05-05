using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// The ALbum DTO.
    /// </summary>
    public class ITunesAlbumDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ITunesAlbumDto"/> class.
        /// </summary>
        public ITunesAlbumDto()
        {
            ResultCount = 0;
            Results = new List<Result>();
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
        [SuppressMessage("Usage", "CA2227", Justification = "Setter is necessary for deserialization")]
        public ICollection<Result> Results { get; set; }
    }
}
