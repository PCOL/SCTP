namespace SCTP.Chunks
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an <c>INIT</c> chunk.
    /// </summary>
    /// <remarks>
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Type = 1      | Chunk Flags   |       Chunk Length            |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Initiate Tag                            |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |           Advertised Receiver Window Credit (a_rwnd)          |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |   Number of Outbound Streams  |   Number of Inbound Streams   |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Initial TSN                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// \                                                               \
    /// /           Optional/Variable-Length Parameters                 /
    /// \                                                               \
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// </remarks>
    class InitChunk
        : Chunk
    {
        /// <summary>
        /// The chunk size.
        /// </summary>
        private const int ChunkSize = 20;

        /// <summary>
        /// Initialises a new instance of the <see cref="InitChunk"/> class.
        /// </summary>
        /// <param name="ack">A value indicating whether or not this should be an ack chunk.</param>
        public InitChunk(bool ack)
            : base(ack == true ? ChunkType.InitAck : ChunkType.Init)
        {
        }

        /// <summary>
        /// Gets or sets the initiate tag.
        /// </summary>
        public uint InitiateTag { get; set; }

        /// <summary>
        /// Gets or sets the advertised receiver window size.
        /// </summary>
        public uint AdvertisedReceiverWindowCredit { get; set; }
        
        /// <summary>
        /// Gets or sets the number of outbound streams.
        /// </summary>
        public ushort OutboundStreams { get; set; }

        /// <summary>
        /// Gets or sets the number of inbound streams.
        /// </summary>
        public ushort InboundStreams { get; set; }

        /// <summary>
        /// Gets or sets the initial <c>TSN</c>.
        /// </summary>
        public uint InitialTSN { get; set; }

        /// <summary>
        /// Gets or sets any parameters.
        /// </summary>
        public List<ChunkParameter> Parameters { get; set; }

        /// <summary>
        /// Adds a chunk parameter.
        /// </summary>
        /// <param name="parameter">The chunk parameter.</param>
        public void AddParameter(ChunkParameter parameter)
        {
            if (this.Parameters == null)
            {
                this.Parameters = new List<ChunkParameter>();
            }

            this.Parameters.Add(parameter);
        }

        /// <summary>
        /// Gets a list containing chunk parameters of a required type.
        /// </summary>
        /// <param name="parameterType">The chunk parameter type.</param>
        /// <returns>A list of chunk parameters.</returns>
        public IEnumerable<ChunkParameter> GetParameters(ChunkParameterType parameterType)
        {
            if (this.Parameters != null)
            {
                List<ChunkParameter> list = new List<ChunkParameter>(this.Parameters.Count);
                foreach (var parm in this.Parameters)
                {
                    if (parm.Type == parameterType)
                    {
                        list.Add(parm); 
                    }
                }

                return list;
            }

            return new List<ChunkParameter>(0);
        }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            length += 16;
            bufferSize += 16;

            if (this.Parameters != null)
            {
                foreach(var parm in this.Parameters)
                {
                    length += parm.Length;
                    bufferSize += Utility.CalculateBufferSize(4, parm.Length);
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
            
            offset += NetworkHelpers.CopyTo(this.InitiateTag, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.AdvertisedReceiverWindowCredit, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.OutboundStreams, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.InboundStreams, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.InitialTSN, buffer, offset);
            int paramsLength = 0;
            if (this.Parameters != null &&
                this.Parameters.Any())
            {
                foreach (var parm in this.Parameters)
                {
                    offset += parm.ToArray(buffer, offset);
                    paramsLength += parm.Length;
                }
            }

            return 16 + paramsLength;
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
            this.InitiateTag = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.AdvertisedReceiverWindowCredit = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.OutboundStreams = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.InboundStreams = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.InitialTSN = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            
            while (offset < length)
            {
                if (this.Parameters == null)
                {
                    this.Parameters = new List<ChunkParameter>();
                }
                    
                ChunkParameter parm = new ChunkParameter();
                offset += parm.FromArray(buffer, offset);
                this.Parameters.Add(parm);
            }

            return offset - start;
        }
    }
}
