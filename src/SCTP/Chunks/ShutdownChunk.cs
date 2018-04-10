namespace SCTP.Chunks
{
    /// <summary>
    /// Represents a shutdown chunk.
    /// </summary>
    /// <remarks>
    /// 
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Type = 7      | Chunk Flags   |       Length = 8              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Cumulative TSN Ack                      |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    internal class ShutdownChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ShutdownChunk"/> class.
        /// </summary>
        /// <param name="ack">A value indicating whether or not this is an ack chunk.</param>
        public ShutdownChunk(bool ack)
            : base(ack == true ? ChunkType.ShutdownAck : ChunkType.Shutdown)
        {
        }

        /// <summary>
        /// Gets or sets the cumulative <c>TSN</c> acked.
        /// </summary>
        public uint CumulativeTSNAck { get; set; }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);
            if (this.Type == ChunkType.Shutdown)
            {
                length += 4;
                bufferSize += 4;
            }

            return length;
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
            
            if (this.Type == ChunkType.Shutdown)
            {
                offset += NetworkHelpers.CopyTo(this.CumulativeTSNAck, buffer, offset);
                return 4;
            }

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
            if (this.Type == ChunkType.Shutdown)
            {
                this.CumulativeTSNAck = NetworkHelpers.ToUInt32(buffer, offset);
                offset += 4;
            }

            return offset - start;
        }
    }
}
