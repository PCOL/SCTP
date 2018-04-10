namespace SCTP.Chunks
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an Abort chunk.
    /// </summary>
    /// <remarks>
    /// 
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Type = 6      |Reserved     |T|           Length              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// \                                                               \
    /// /                   zero or more Error Causes                   /
    /// \                                                               \
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    internal class AbortChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="AbortChunk"/> class.
        /// </summary>
        public AbortChunk()
            : base(ChunkType.Abort)
        {
        }

        /// <summary>
        /// Gets or sets a list of error causes.
        /// </summary>
        public List<ErrorCause> Causes { get; set; }

        /// <summary>
        /// Adds an error cause to the chunk.
        /// </summary>
        /// <param name="cause"></param>
        public void AddCause(ErrorCause cause)
        {
            if (this.Causes == null)
            {
                this.Causes = new List<ErrorCause>();
            }

            this.Causes.Add(cause);
        }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);
            if (this.Causes != null)
            {
                foreach (var cause in this.Causes)
                {
                    length += cause.Length;
                    bufferSize += cause.Length;
                }
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
