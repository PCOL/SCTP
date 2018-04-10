namespace SCTP.Chunks
{
    /// <summary>
    /// Represents a Cookie Ack chunk.
    /// </summary>
    internal class CookieAckChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of a <see cref="CookieAckChunk"/> class.
        /// </summary>
        public CookieAckChunk()
            : base(ChunkType.CookieAck)
        {
            this.Flags = 0;
        }

        /// <summary>
        /// Writes the chunk to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset to start writing.</param>
        /// <returns>The number of bytes written.</returns>
        protected override int ToBuffer(byte[] buffer, int offset, out int dataLength)
        {
            dataLength = 0;
            return 0;
        }

        /// <summary>
        /// Reads the chunk from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset to start reading from.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            return offset - start;
        }
    }
}
