using System.Collections.Concurrent;

namespace Plotter
{
    public readonly ref struct AString : IDisposable
    {
        public ReadOnlySpan<char> Span { get; }
        public readonly int Length { get; }
        private readonly char[]? _bufferToReturn;

        private AString(char[]? buffer, int length )
        {
            _bufferToReturn = buffer;
            Length = length;
            if (buffer == null || length <= 0)
            {
                Span = [];
                return;
            }

            Span = buffer.AsSpan(0, length);
        }

        public static AString Create(ReadOnlySpan<char> sourceText)
        {
            var buffer = CharPool.Rent();

            sourceText.CopyTo(buffer);

            return new AString(buffer, sourceText.Length);
        }

        public static AString Create(char[] sourceText, int length)
        {
            if (sourceText == null || sourceText.Length == 0)
                return Empty;

            var buffer = CharPool.Rent();
            
            var span = sourceText.AsSpan(0, length);

            span.CopyTo(buffer);

            return new AString(buffer, length);
        }

        public static AString Create(double value, string format = "F2")
        {
            var buffer = CharPool.Rent();

            if (value.TryFormat(buffer, out int charsWritten, format))
                return new AString(buffer, charsWritten);
            
            CharPool.Return(buffer);

            return Empty;
        }

        public void Dispose()
        {
            if (_bufferToReturn != null)
                CharPool.Return(_bufferToReturn);
        }

        public override bool Equals(object? obj) => false;
        public bool Equals(AString other) => Span.SequenceEqual(other.Span);
        public override int GetHashCode() => string.GetHashCode(Span);
        public static AString Empty => new(null, 0);

        public static implicit operator ReadOnlySpan<char>(AString aString) => aString.Span;
        public static implicit operator AString(ReadOnlySpan<char> span) => Create(span);
        public static bool     operator == (AString left, AString right) => left.Equals(right);
        public static bool     operator != (AString left, AString right) => !left.Equals(right);

        public readonly char this[int index] => Span[index];
    }

}