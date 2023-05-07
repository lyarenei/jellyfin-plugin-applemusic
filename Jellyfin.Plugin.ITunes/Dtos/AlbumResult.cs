using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ITunes.Dtos
{
    /// <summary>
    /// Result.
    /// </summary>
    public class AlbumResult
    {
        /// <summary>
        /// Gets or sets the wrapper type.
        /// </summary>
        [JsonPropertyName("wrapperType")]
        public string? WrapperType { get; set; }

        /// <summary>
        /// Gets or sets the collection type.
        /// </summary>
        [JsonPropertyName("collectionType")]
        public string? CollectionType { get; set; }

        /// <summary>
        /// Gets or sets the artist id.
        /// </summary>
        [JsonPropertyName("artistId")]
        public long? ArtistId { get; set; }

        /// <summary>
        /// Gets or sets the collection id.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public long? CollectionId { get; set; }

        /// <summary>
        /// Gets or sets the artist name.
        /// </summary>
        [JsonPropertyName("artistName")]
        public string? ArtistName { get; set; }

        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        [JsonPropertyName("collectionName")]
        public string? CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the censored collection name.
        /// </summary>
        [JsonPropertyName("collectionCensoredName")]
        public string? CollectionCensoredName { get; set; }

        /// <summary>
        /// Gets or sets the artist view URL.
        /// </summary>
        [JsonPropertyName("artistViewUrl")]
        public string? ArtistViewUrl { get; set; }

        /// <summary>
        /// Gets or sets the collection view URL.
        /// </summary>
        [JsonPropertyName("collectionViewUrl")]

        public string? CollectionViewUrl { get; set; }

        /// <summary>
        /// Gets or sets the 60px artwork URL.
        /// </summary>
        [JsonPropertyName("artworkUrl60")]
        public string? ArtworkUrl60 { get; set; }

        /// <summary>
        /// Gets or sets the 100px artwork URL.
        /// </summary>
        [JsonPropertyName("artworkUrl100")]
        public string? ArtworkUrl100 { get; set; }

        /// <summary>
        /// Gets or sets the collection price.
        /// </summary>
        [JsonPropertyName("collectionPrice")]
        public double? CollectionPrice { get; set; }

        /// <summary>
        /// Gets or sets the collection explicitness.
        /// </summary>
        [JsonPropertyName("collectionExplicitness")]
        public string? CollectionExplicitness { get; set; }

        /// <summary>
        /// Gets or sets the track count.
        /// </summary>
        [JsonPropertyName("trackCount")]
        public long? TrackCount { get; set; }

        /// <summary>
        /// Gets or sets the copyright.
        /// </summary>
        [JsonPropertyName("copyright")]
        public string? Copyright { get; set; }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        /// <value>The country.</value>
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        /// <value>The currency.</value>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        /// <value>The release date.</value>
        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the primary genre name.
        /// </summary>
        /// <value>The primary genre name.</value>
        [JsonPropertyName("primaryGenreName")]
        public string? PrimaryGenreName { get; set; }

        /// <summary>
        /// Gets or sets the content advisory rating.
        /// </summary>
        /// <value>The content advisory rating.</value>
        [JsonPropertyName("contentAdvisoryRating")]
        public string? ContentAdvisoryRating { get; set; }

        /// <summary>
        /// Gets or sets the amg artist id.
        /// </summary>
        /// <value>The amg artist id.</value>
        [JsonPropertyName("amgArtistId")]
        public long? AmgArtistId { get; set; }
    }
}
