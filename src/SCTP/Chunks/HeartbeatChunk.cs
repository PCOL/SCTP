namespace SCTP.Chunks
{
    /// <summary>
    /// Represents a heartbeat chunk.
    /// </summary>
    internal class HeartbeatChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="HeartbeatChunk"/> class.
        /// </summary>
        /// <param name="ack">A value indicating whether or not this is an ack chunk.</param>
        public HeartbeatChunk(bool ack)
            : base(ack == true ? ChunkType.HeartbeatAck : ChunkType.Heartbeat)
        {
        }

        /// <summary>
        /// Gets or sets the heartbeat information.
        /// </summary>
        public ChunkParameter HeartbeatInfo { get; set; }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            if (this.HeartbeatInfo != null)
            {
                length += this.HeartbeatInfo.Length;
                bufferSize += Utility.CalculateBufferSize(4, this.HeartbeatInfo.Length);
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
            offset += this.HeartbeatInfo.ToArray(buffer, offset);
            return this.HeartbeatInfo.Length;
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
            this.HeartbeatInfo.FromArray(buffer, offset);
            offset += this.HeartbeatInfo.Length;
            return offset - start;
        }
    }
}
