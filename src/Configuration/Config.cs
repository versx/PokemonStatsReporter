namespace PokemonStatsReporter.Configuration
{
    using System.Text.Json.Serialization;

    using PokemonStatsReporter.Extensions;

    /// <summary>
    /// Configuration file class
    /// </summary>
    public class Config
    {
        #region Properties

        /// <summary>
        /// Gets or sets the locale translation file to use
        /// </summary>
        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the amount of hours to check for previous data
        /// to include with statistic reports
        /// </summary>
        [JsonPropertyName("statHours")]
        public ushort StatHours { get; set; }

        /// <summary>
        /// Gets or sets the date format to use with statistic reports
        /// </summary>
        [JsonPropertyName("dateFormat")]
        public string DateFormat { get; set; }

        /// <summary>
        /// Gets or sets the Discord servers configuration
        /// </summary>
        [JsonPropertyName("servers")]
        public Dictionary<ulong, DiscordServerConfig> Servers { get; set; } = new();

        /// <summary>
        /// Gets or sets the Database configuration
        /// </summary>
        [JsonPropertyName("database")]
        public DatabaseConfig Database { get; set; } = new();

        /// <summary>
        /// Gets or sets the configuration file path
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        #endregion

        /// <summary>
        /// Instantiate a new <see cref="Config"/> class
        /// </summary>
        public Config()
        {
            Locale = "en";
            StatHours = 24;
            DateFormat = "dddd, MMMM d, yyyy";
            FileName = Strings.ConfigFileName;
        }

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var data = this.ToJson();
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static Config Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }
            var config = filePath.LoadFromFile<Config>();
            return config;
        }
    }
}