﻿namespace PokemonStatsReporter.Extensions
{
    using System.Text.Json;

    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //IgnoreReadOnlyProperties = true,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static T FromJson<T>(this string json) =>
            JsonSerializer.Deserialize<T>(json, _jsonOptions);

        public static string ToJson<T>(this T obj) =>
            JsonSerializer.Serialize(obj, _jsonOptions);
    }
}