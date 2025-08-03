﻿using System.Globalization;

namespace Plotter
{
    internal static class MyTextParser
    {
        // Caller supplies (and reuses) the dictionary so we don’t keep a big static one alive.
        public static void Parse(string text, Dictionary<string, double> target)
        {
            target.Clear();                     // caller decides whether to clear
            ReadOnlySpan<char> span = text;     // same as text.AsSpan()

            while (!span.IsEmpty)
            {
                int tab = span.IndexOf('\t');
                ReadOnlySpan<char> part = tab < 0 ? span : span[..tab];

                int colon = part.IndexOf(':');
                if (colon > 0)
                {
                    ReadOnlySpan<char> fieldSpan = part[..colon];
                    ReadOnlySpan<char> valueSpan = part[(colon + 1)..];

                    // — no allocation here —
                    if (double.TryParse(valueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    {
                        target[OptimizedStringStorage.Get(fieldSpan)] = value;
                    }
                }

                if (tab < 0) break;
                span = span[(tab + 1)..];
            }
        }
    }


    public class OptimizedStringStorage
    {
        // A dictionary mapping a hash code to a list of strings that share that hash code.
        private static readonly Dictionary<int, List<string>> _buckets = [];

        /// <summary>
        /// Retrieves a cached string matching the span, or creates and caches a new one.
        /// Lookup performance is O(1) on average.
        /// </summary>
        public static string Get(ReadOnlySpan<char> span)
        {
            // 1. Get the hash code for the span without converting it to a string.
            int hashCode = string.GetHashCode(span);

            // 2. Look for a bucket with this hash code.
            if (_buckets.TryGetValue(hashCode, out var bucket))
            {
                // 3. A bucket exists. Check all strings within it (to handle hash collisions).
                foreach (string s in bucket)
                {
                    if (span.SequenceEqual(s))
                    {
                        return s; // Match found, return the cached string.
                    }
                }
            }

            // 4. No match found. Allocate the new string.
            string newString = span.ToString();

            // 5. Return the new string to the correct bucket.
            if (bucket == null)
            {
                bucket = [];
                _buckets[hashCode] = bucket;
            }
            bucket.Add(newString);

            return newString;
        }
    }
}