﻿namespace StatsReporter.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using HandlebarsDotNet;

    public static class StringExtensions
    {
        public static string FormatText(this string text, dynamic args)
        {
            var template = Handlebars.Compile(text);
            return template(args);
        }

        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            if (partLength <= 0)
                throw new ArgumentException("Part length must be a positive number.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
            {
                yield return s.Substring(i, Math.Min(partLength, s.Length));
            }
        }

        public static List<string> RemoveSpaces(this string value)
        {
            return value.Replace(", ", ",")
                        .Replace(" ,", ",")
                        .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
        }

        public static string ToCamelCase(this string value)
        {
            var first = value[0].ToString().ToLower();
            var last = string.Concat(value.Skip(1));
            return first + last;
        }
    }
}