namespace SCTP
{
    /// <summary>
    /// Represents an Invalid Stream Identifier error cause.
    /// </summary>
    public class InvalidStreamIdentifier
        : ErrorCause
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="InvalidStreamIdentifier"/> class.
        /// </summary>
        public InvalidStreamIdentifier()
            : base (CauseCode.InvalidStreamIdentifier)
        {
        }

        /// <summary>
        /// Gets or sets the stream id.
        /// </summary>
        public ushort StreamId { get; set; }

        /// <summary>
        /// Gets or sets the reserved.
        /// </summary>
        internal ushort Reserved { get; set; }

        /// <summary>
        /// Writes the data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array</param>
        /// <param name="offset">The offset at which to write the data.</param>
        /// <returns>The number of bytes written.</returns>
        protected override int ToBuffer(byte[] buffer, int offset)
        {
            offset += NetworkHelpers.CopyTo(this.StreamId, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Reserved, buffer, offset);
            return 4;
        }

        /// <summary>
        /// Reads the data from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset to read from.</param>
        /// <returns>The number of bytes read.</returns>
        protected override int FromBuffer(byte[] buffer, int offset)
        {
            int start = offset;
            this.StreamId = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.Reserved = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            return offset - start;
        }
    }
}
