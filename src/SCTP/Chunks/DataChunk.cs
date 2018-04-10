namespace SCTP.Chunks
{
    using System;

    /// <summary>
    /// Represents a <c>DATA</c> chunk.
    /// </summary>
    /// <remarks>
    /// The following format MUST be used for the DATA chunk:
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Type = 0      | Reserved|U|B|E|           Length              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           TSN                                 |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |   Stream Identifier S         | Stream Sequence Number n      |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |               Payload Protocol Identifier                     |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// \                                                               \
    /// /               User Data (seq n of Stream S)                   /
    /// \                                                               \
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    internal class DataChunk
        : Chunk
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="DataChunk"/> class.
        /// </summary>
        public DataChunk()
            : base(ChunkType.Data)
        {
        }

        /// <summary>
        /// Gets or sets the <c>TSN</c>.
        /// </summary>
        public uint TSN { get; set; }

        /// <summary>
        /// Gets or sets the stream id.
        /// </summary>
        public ushort StreamId { get; set; }

        /// <summary>
        /// Gets or sets the stream sequence number.
        /// </summary>
        public ushort StreamSeqNo { get; set; }

        /// <summary>
        /// Gets or set the payload protocol id.
        /// </summary>
        public uint PayloadProtocolId { get; set; }

        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        public ArraySegment<byte> UserData { get; set; }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            length += 12;
            bufferSize += 12;

            if (this.UserData != null)
            {
                length += this.UserData.Count;
                bufferSize += Utility.CalculateBufferSize(0, this.UserData.Count);
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
            dataLength = this.UserData.Count;

            int start = offset;
            offset += NetworkHelpers.CopyTo(this.TSN, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.StreamId, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.StreamSeqNo, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.PayloadProtocolId, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.UserData, buffer, offset);
            return offset - start;
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
            this.TSN = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.StreamId = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.StreamSeqNo = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.PayloadProtocolId = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.UserData = new ArraySegment<byte>(NetworkHelpers.ToBytes(buffer, offset, length - 16, out int paddedLength));
            offset += paddedLength;
            return offset - start;
        }
    }
}
