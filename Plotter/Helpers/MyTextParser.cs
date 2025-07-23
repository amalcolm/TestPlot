
namespace Plotter
{
    internal static class MyTextParser
    {
        public static Dictionary<string, double> Data { get; } = [];
        public static Dictionary<string, double> Parse(string text)
        {
            // Get a ReadOnlySpan from the input string to avoid allocations
            ReadOnlySpan<char> textSpan = text.AsSpan();

            // Loop through tab-separated parts without using Split()
            while (true)
            {
                int tabIndex = textSpan.IndexOf('\t');

                // Get the part before the tab, or the rest of the string if no tab is found
                ReadOnlySpan<char> part = (tabIndex == -1) ? textSpan : textSpan[..tabIndex];

                // -- Process the key-value pair from the 'part' span --
                int colonIndex = part.IndexOf(':');
                if (colonIndex != -1)
                {
                    ReadOnlySpan<char> fieldSpan = part[..colonIndex];
                    ReadOnlySpan<char> valueSpan = part[(colonIndex + 1)..];

                    if (double.TryParse(valueSpan.ToString(), out double parsedValue))
                        Data[fieldSpan.ToString()] = parsedValue;
                }

                if (tabIndex == -1) break;
                textSpan = textSpan[(tabIndex + 1)..];
            }

            return Data;
        }


        private static string _text = string.Empty;
    }
}