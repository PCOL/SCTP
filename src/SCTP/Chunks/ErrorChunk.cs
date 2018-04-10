namespace SCTP.Chunks
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an Error chunk.
    /// </summary>
    /// <remarks>
    /// 
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |   Type = 9    |   Chunk Flags |           Length              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// \                                                               \
    /// /                   one or more Error Causes                    /
    /// \                                                               \
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    internal class ErrorChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ErrorChunk"/> class.
        /// </summary>
        public ErrorChunk()
            : base(ChunkType.Error)
        {
        }

        /// <summary>
        /// Gets or set the list of error casues.
        /// </summary>
        public List<ErrorCause> ErrorCauses { get; set; }

        /// <summary>
        /// Adds an error cause.
        /// </summary>
        /// <param name="errorCause">The error cause</param>
        public void AddErrorCause(ErrorCause errorCause)
        {
            if (this.ErrorCauses == null)
            {
                this.ErrorCauses = new List<ErrorCause>();
            }

            this.ErrorCauses.Add(errorCause);
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
