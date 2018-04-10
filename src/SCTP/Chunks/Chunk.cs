namespace SCTP.Chunks
{
    using System;

    /// <summary>
    /// Represents a <c>SCTP</c> chunk.
    /// </summary>
    internal abstract class Chunk
    {
        /// <summary>
        /// The base chunk size.
        /// </summary>
        private const int ChunkSize = 4;

        /// <summary>
        /// Initialises a new instance of the <see cref="Chunk"/> class.
        /// </summary>
        /// <param name="chunkType">The chunk type.</param>
        protected Chunk(ChunkType chunkType)
        {
            this.Type = chunkType;
        }

        /// <summary>
        /// Gets the chunk type.
        /// </summary>
        public ChunkType Type { get; private set; }

        /// <summary>
        /// Gets or sets the chunks flags.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Gets the chunks length.
        /// </summary>
        public ushort Length { get; private set; }

        /// <summary>
        /// Gets or sets the ack count.
        /// </summary>
        internal int AckCount { get; set; }

        /// <summary>
        /// Gets or sets the transmission count.
        /// </summary>
        internal int TransmitCount {  get; set; }

        /// <summary>
        /// Gets or sets the miss count.
        /// </summary>
        internal int MissCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the packet has been fast retransmitted.
        /// </summary>
        internal bool FastRetransmit { get; set; }

        /// <summary>
        /// Gets or sets the first transmission timestamp;
        /// </summary>
        internal long FirstTransmitTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the last transmission timestamp;
        /// </summary>
        internal long LastTransmitTimestamp { get; set; }

        /// <summary>
        /// Calculates the length of the chunk.
        /// </summary>
        /// <returns>The length of the chunk.</returns>
        internal int CalculateLength()
        {
            this.Length = Convert.ToUInt16(this.CalculateLength(out int bufferSize));
            return this.Length;
        }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the buffer size.</param>
        /// <returns>The length of the chunk.</returns>
        protected internal virtual int CalculateLength(out int bufferSize)
        {
            bufferSize = ChunkSize;
            return bufferSize;
        }

        /// <summary>
        /// Outputs the chunk as a byte array.
        /// </summary>
        /// <returns>The byte array containing the chunk data.</returns>
        public byte[] ToArray()
        {
            this.Length = Convert.ToUInt16(this.CalculateLength(out int bufferSize));

            byte[] buffer = new byte[bufferSize];
            this.ToArray(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Outputs the chunk into a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset into the array to start writing the chunk.</param>
        /// <returns>The length of the chunk.</returns>
        public int ToArray(byte[] buffer, int offset)
        {
            int start = offset;
            this.Length = (ushort)this.CalculateLength(out int bufferSize);

            offset += NetworkHelpers.CopyTo((byte)this.Type, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Flags, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Length, buffer, offset);
            offset += this.ToBuffer(buffer, offset, out int dataLength);

            return offset - start;
        }

        /// <summary>
        /// Convert 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        protected abstract int ToBuffer(byte[] buffer, int offset, out int dataLength);

        /// <summary>
        /// Builds the chunk from a byte array.
        /// </summary>
        /// <param name="buffer">The byte arry.</param>
        /// <param name="offset">The offset to the start of the chunk.</param>
        /// <returns>The number of bytes read from the buffer.</returns>
        internal virtual int FromArray(byte[] buffer, int offset)
        {
            int start = offset;

            this.Type = (ChunkType)NetworkHelpers.ToByte(buffer, offset);
            offset += 1;
            this.Flags = NetworkHelpers.ToByte(buffer, offset);
            offset += 1;
            this.Length = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            offset += this.FromBuffer(buffer, offset, this.Length);

            return offset - start;
        }

        /// <summary>
        /// Builds the chunk from a byte buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The ofset to the start of the chunk.</param>
        /// <param name="length">The length of the chunk.</param>
        /// <returns>The number of bytes read from the buffer.</returns>
        protected abstract int FromBuffer(byte[] buffer, int offset, int length);

        /// <summary>
        /// Creates a chunk from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset to the start of the chunk.</param>
        /// <returns>The chunk.</returns>
        public static Chunk Create(byte[] buffer, int offset, out int bytesRead)
        {
            bytesRead = 0;
            Chunk chunk = null;
            byte tmp = NetworkHelpers.ToByte(buffer, offset);
            if (Enum.IsDefined(typeof(ChunkType), tmp) == false)
            {
                throw new NotSupportedException("Unkown chunk type");
            }

            ChunkType chunkType = (ChunkType)tmp;
            if (chunkType == ChunkType.Data)
            {
                chunk = new DataChunk();
            }
            else if (chunkType == ChunkType.Sack)
            {
                chunk = new SelectiveAckChunk();
            }
            else if (chunkType == ChunkType.Init)
            {
                chunk = new InitChunk(false);
            }
            else if (chunkType == ChunkType.InitAck)
            {
                chunk = new InitChunk(true);
            }
            else if (chunkType == ChunkType.CookieEcho)
            {
                chunk = new CookieEchoChunk();
            }
            else if (chunkType == ChunkType.CookieAck)
            {
                chunk = new CookieAckChunk();
            }
            else if (chunkType == ChunkType.Heartbeat)
            {
                chunk = new HeartbeatChunk(false);
            }
            else if (chunkType == ChunkType.HeartbeatAck)
            {
                chunk = new HeartbeatChunk(true);
            }
            else if (chunkType == ChunkType.Shutdown)
            {
                chunk = new ShutdownChunk(false);
            }
            else if (chunkType == ChunkType.ShutdownAck)
            {
                chunk = new ShutdownChunk(true);
            }
            else if (chunkType == ChunkType.ShutdownComplete)
            {
                chunk = new ShutdownCompleteChunk();
            }
            else if (chunkType == ChunkType.Abort)
            {
                chunk = new AbortChunk();
            }
            else if (chunkType == ChunkType.Error)
            {
                chunk = new ErrorChunk();
            }
            else
            {
                throw new NotSupportedException("Chunk type not supported");
            }

            bytesRead = chunk.FromArray(buffer, offset);
            return chunk;
        }
    }
}
