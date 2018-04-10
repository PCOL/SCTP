namespace SCTP
{
    /// <summary>
    /// Represents a common header.
    /// </summary>
    internal class CommonHeader
    {
        /// <summary>
        /// Gets or sets the source port.
        /// </summary>
        public ushort SourcePort { get; set; }

        /// <summary>
        /// Gets or sets the destination port.
        /// </summary>
        public ushort DestinationPort { get; set; }

        /// <summary>
        /// Gets or sets the verification tag.
        /// </summary>
        public uint VerificationTag { get; set; }

        /// <summary>
        /// Gets or sets the checksum value.
        /// </summary>
        public uint Checksum { get; set; }

        /// <summary>
        /// Writes the common header into a byte array at a given offset.
        /// </summary>
        /// <param name="buffer">The byte array</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The number of bytes written to the byte array.</returns>
        public int ToArray(byte[] buffer, int offset)
        {
            int start = offset;
            offset += NetworkHelpers.CopyTo(this.SourcePort, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.DestinationPort, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.VerificationTag, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Checksum, buffer, offset);
            return offset - start;
        }

        /// <summary>
        /// Reads a common header's values from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset of the common header in the byte array.</param>
        /// <returns>The number of bytes read.</returns>
        internal int FromArray(byte[] buffer, int offset)
        {
            int start = offset;
            this.SourcePort = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.DestinationPort = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.VerificationTag = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.Checksum = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;

            return offset - start;
        }
    }
}
