namespace SCTP.Chunks
{
    using System.Net;

    /// <summary>
    /// Represents a chunk parameter.
    /// </summary>
    /// <remarks>
    /// 
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |       Parameter Type          |       Parameter Length        |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// \                                                               \
    /// /                       Parameter Value                         /
    /// \                                                               \
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    internal class ChunkParameter
    {
        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public ChunkParameterType Type { get; set; }

        /// <summary>
        /// Gets or sets the length of the parameter.
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter as an ASCII string.
        /// </summary>
        public string ValueString
        {
            get { return this.Value != null ? System.Text.ASCIIEncoding.ASCII.GetString(this.Value) : null; }
            set { this.Value = value != null ? System.Text.ASCIIEncoding.ASCII.GetBytes(value) : null; }
        }

        /// <summary>
        /// Gets or sets the value of the parameter as an UTF8 string.
        /// </summary>
        public string ValueUTF8String
        {
            get { return this.Value != null ? System.Text.UTF8Encoding.UTF8.GetString(this.Value) : null; }
            set { this.Value = value != null ? System.Text.UTF8Encoding.UTF8.GetBytes(value) : null; }
        }

        /// <summary>
        /// Gets or sets the value of the parameter as an IP Address
        /// </summary>
        public IPAddress ValueIPAddress
        {
            get { return this.Value != null ? new IPAddress(this.Value) : null; }
            set { this.Value = value != null ? value.GetAddressBytes() : null; }
        }

        /// <summary>
        /// Returns the chunk parameter as a byte array.
        /// </summary>
        /// <returns>The byte array.</returns>
        public byte[] ToArray()
        {
            byte[] buffer = new byte[this.Length];
            this.ToArray(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Writes the chunk parameter into a byte array.
        /// </summary>
        /// <param name="buffer">the byte array.</param>
        /// <param name="offset">The Offset at which to start writing the chunk parameter.</param>
        /// <returns>The number of bytes written.</returns>
        public int ToArray(byte[] buffer, int offset)
        {
            int start = offset;
            offset += NetworkHelpers.CopyTo((ushort)this.Type, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Length, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Value, buffer, offset);
            return offset - start;
        }
        
        /// <summary>
        /// Populates the chunk parameter from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset into the array to start from.</param>
        /// <returns>The number of bytes used to make the chunk parameter.</returns>
        internal int FromArray(byte[] buffer, int offset)
        {
            int start = offset;

            this.Type = (ChunkParameterType)NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.Length = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.Value = NetworkHelpers.ToBytes(buffer, offset, this.Length - 4, out int paddedLength);
            offset += paddedLength;

            return offset - start;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static ChunkParameter Create(byte[] buffer, int offset)
        {
            ChunkParameter chunk = new ChunkParameter();
            chunk.FromArray(buffer, offset);

            return chunk;
        }
    }
}
