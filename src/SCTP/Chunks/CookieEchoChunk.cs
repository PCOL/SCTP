namespace SCTP.Chunks
{
    /// <summary>
    /// Represents a Cookie Echo chunk.
    /// </summary>
    internal class CookieEchoChunk
        : Chunk
    {
        /// <summary>
        /// Gets or sets the cookie.
        /// </summary>
        public byte[] Cookie { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="CookieEchoChunk"/> class.
        /// </summary>
        public CookieEchoChunk()
            : base(ChunkType.CookieEcho)
        {
        }

        /// <summary>
        /// Calculate the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the buffer size.</param>
        /// <returns>The length of the chunk.</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            if (this.Cookie != null)
            {
                length += this.Cookie.Length;
                bufferSize += Utility.CalculateBufferSize(0, this.Cookie.Length);
            }

            return length;
        }

        /// <summary>
        /// Writes the chunk to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset at which to start writing.</param>
        /// <returns>The number of bytes written to the buffer.</returns>
        protected override int ToBuffer(byte[] buffer, int offset, out int dataLength)
        {
            dataLength = 0;
            offset += NetworkHelpers.CopyTo(this.Cookie, buffer, offset, false);
            return this.Cookie.Length;
        }

        /// <summary>
        /// Read the chunk from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset at which to start reading the chunk.</param>
        /// <param name="length">The number of bytes that make up the chunk.</param>
        /// <returns>The number of bytes read.</returns>
        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            this.Cookie = NetworkHelpers.ToBytes(buffer, offset, length - 4);
            offset += this.Cookie.Length;
            return offset - start;
        }
    }
}
