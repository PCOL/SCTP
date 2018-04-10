namespace SCTP.Chunks
{
    /// <summary>
    /// Represents a Shutdown Complete chunk.
    /// </summary>
    internal class ShutdownCompleteChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ShutdownCompleteChunk"/> class
        /// </summary>
        public ShutdownCompleteChunk()
            : base(ChunkType.ShutdownComplete)
        {
            this.Flags = 0x00000001;
        }

        /// <summary>
        /// Writes the chunk into a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset at which to start writing the chunk.</param>
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
        /// <param name="offset">The offset at which to start reading the chunk.</param>
        /// <param name="length">The number of bytes that make up the chunk..</param>
        /// <returns>The number of bytes read.</returns>
        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            return offset - start;
        }
    }
}
