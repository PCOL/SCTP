namespace SCTP
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using SCTP.Chunks;

    /// <summary>
    /// Represents a message.
    /// </summary>
    public class SCTPMessage
    {
        /// <summary>
        /// The message content.
        /// </summary>
        private ArraySegment<byte> content;

        /// <summary>
        /// The message chunks.
        /// </summary>
        private SortedList<uint, DataChunk> chunks;

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPMessage"/> class.
        /// </summary>
        /// <param name="streamId">The stream the message will be transmitted on.</param>
        /// <param name="streamSeqNo">The messages stream sequence number.</param>
        /// <param name="buffer">The buffer to construct the message from.</param>
        /// <param name="offset">The offset to the start of the message.</param>
        /// <param name="length">The length of the message.</param>
        public SCTPMessage(int streamId, int streamSeqNo, byte[] buffer, int offset, int length)
            : this(streamId, streamSeqNo, new ArraySegment<byte>(buffer, offset, length))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPMessage"/> class.
        /// </summary>
        /// <param name="streamId">The stream the message will be transmitted on.</param>
        /// <param name="streamSeqNo">The messages stream sequence number.</param>
        /// <param name="content">An <see cref="ArraySegment"/> which represents the message content.</param>
        public SCTPMessage(int streamId, int streamSeqNo, ArraySegment<byte> content)
            : this(streamId, streamSeqNo)
        {
            this.content = content;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPMessage"/> class.
        /// </summary>
        /// <param name="streamId">The stream the message will be transmitted on.</param>
        /// <param name="streamSeqNo">The messages stream sequence number.</param>
        public SCTPMessage(int streamId, int streamSeqNo = -1)
        {
            this.StreamId = streamId;
            this.StreamSequenceNo = streamSeqNo;
            this.chunks = new SortedList<uint, DataChunk>();
        }

        /// <summary>
        /// Gets the id of the stream the message will be sent on.
        /// </summary>
        public int StreamId { get; private set; }

        /// <summary>
        /// Gets the stream sequence number for this message.
        /// </summary>
        public int StreamSequenceNo { get; private set; }

        /// <summary>
        /// Gets a stream containing the received data.
        /// </summary>
        public ChunkStream Stream { get; private set; }

        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        public long Length
        {
            get
            {
                if (this.Stream != null)
                {
                    return this.Stream.Length;
                }
                else if (this.content != null)
                {
                    return this.content.Count;
                }

                return 0L;
            }
        }

        /// <summary>
        /// Adds a data chunk to the message.
        /// </summary>
        /// <param name="tsn">The tsn.</param>
        /// <param name="dataChunk">The data chunk.</param>
        /// <returns>True if the chunk was added; otherwise false.</returns>
        internal bool AddChunk(DataChunk dataChunk)
        {
            if (this.chunks.ContainsKey(dataChunk.TSN) == false)
            {
                this.chunks.Add(dataChunk.TSN, dataChunk);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the chunks to a transmit queue.
        /// </summary>
        /// <param name="transmitQueue">The transmit queue.</param>
        /// <returns>The number of chunks added.</returns>
        internal int AddChunksToTransmitQueue(IList<DataChunk> transmitQueue)
        {
            foreach (var chunk in this.chunks.Values)
            {
                transmitQueue.Add(chunk);
            }

            return this.chunks.Count;
        }

        /// <summary>
        /// Checks if the message is complete.
        /// </summary>
        /// <returns>True if the message is complete; otherwise false.</returns>
        internal bool IsMessageComplete()
        {
            if (this.chunks.Count == 0)
            {
                return false;
            }

            if (((int)this.chunks.First().Value.Flags & (int)DataChunkFlags.Begining) == 0)
            {
                return false;
            }

            if (((int)this.chunks.Last().Value.Flags & (int)DataChunkFlags.Ending) == 0)
            {
                return false;
            }

            var e = this.chunks.GetEnumerator();
            if (e.MoveNext() == true)
            {
                uint lastTsn = e.Current.Key;
                while(e.MoveNext() == true)
                {
                    if (e.Current.Key != lastTsn + 1)
                    {
                        return false;
                    }

                    lastTsn = e.Current.Key;
                }
            }

            // Builder the stream.
            var chunkBuffers = this.chunks.Select((kvp) => { return kvp.Value.UserData; });
            this.Stream = new ChunkStream(chunkBuffers.ToArray());

            return true;
        }

        /// <summary>
        /// Breaks the message into chunks.
        /// </summary>
        /// <param name="tsnGenerator">A function which generates the next tsn.</param>
        /// <param name="seqNo">The stream sequence number.</param>
        /// <param name="mtuSize">The current <c>MTU</c> size.</param>
        /// <returns>The size of the chunked message (in bytes)</returns>
        internal int ToChunks(Func<uint> tsnGenerator, ushort seqNo, int mtuSize)
        {
            bool first = true;
            int size = 0;
            int start = content.Offset;
            int offset = start;
            int maxDataLength = mtuSize - 30;

            DataChunk chunk = null;
            while (offset < content.Count)
            {
                chunk = new DataChunk();
                int length = Convert.ToUInt16(Math.Min(content.Count - offset, maxDataLength));

                if (first == true)
                {
                    first = false;
                    chunk.Flags |= (byte)DataChunkFlags.Begining;
                }

                chunk.TSN = tsnGenerator();
                chunk.StreamId = Convert.ToUInt16(this.StreamId);
                chunk.StreamSeqNo = seqNo;
                chunk.PayloadProtocolId = 0;
                chunk.UserData = new ArraySegment<byte>(new byte[length]);
                Buffer.BlockCopy(this.content.Array, offset, chunk.UserData.Array, 0, length);
                offset += length;

                this.chunks.Add(chunk.TSN, chunk);

                size += chunk.Length;
            }

            if (chunk != null)
            {
                chunk.Flags |= (byte)DataChunkFlags.Ending;
            }

            return size;
        }
    }
}
