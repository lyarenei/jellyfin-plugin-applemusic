using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// Artist result.
    /// </summary>
    public class ArtistResult
    {
        /// <summary>
        /// Gets or sets the wrapper type.
        /// </summary>
        /// <value>The wrapper type.</value>
        [JsonPropertyName("wrapperType")]
        public string? WrapperType { get; set; }

        /// <summary>
        /// Gets or sets the artist type.
        /// </summary>
        /// <value>The artist type.</value>
        [JsonPropertyName("artistType")]
        public string? ArtistType { get; set; }

        /// <summary>
        /// Gets or sets the artist name.
        /// </summary>
        /// <value>The artist name.</value>
        [JsonPropertyName("artistName")]
        public string? ArtistName { get; set; }

        /// <summary>
        /// Gets or sets the artist link url.
        /// </summary>
        /// <value>The artist link url.</value>
        [JsonPropertyName("artistLinkUrl")]
        public string? ArtistLinkUrl { get; set; }

        /// <summary>
        /// Gets or sets the artist id.
        /// </summary>
        /// <value>The artist id.</value>
        [JsonPropertyName("artistId")]
        public long? ArtistId { get; set; }

        /// <summary>
        /// Gets or sets the primary genre name.
        /// </summary>
        /// <value>The primary genre name.</value>
        [JsonPropertyName("primaryGenreName")]
        public string? PrimaryGenreName { get; set; }

        /// <summary>
        /// Gets or sets the primary genre id.
        /// </summary>
        /// <value>The primary genre id.</value>
        [JsonPropertyName("primaryGenreId")]
        public long? PrimaryGenreId { get; set; }

        /// <summary>
        /// Gets or sets the amg artist id.
        /// </summary>
        /// <value>The amg artist id.</value>
        [JsonPropertyName("amgArtistId")]
        public long? AmgArtistId { get; set; }
    }
}
