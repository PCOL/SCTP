namespace SCTP
{
    using System;
    using System.Threading;

    /// <summary>
    /// Represents a stream.
    /// </summary>
    internal class SCTPStream
    {
        /// <summary>
        /// The stream sequence number.
        /// </summary>
        private int streamSequenceNo;

        /// <summary>
        /// The number of bytes sent.
        /// </summary>
        private long bytesSent;

        /// <summary>
        /// The number of bytes received.
        /// </summary>
        private long bytesReceived;

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPStream"/> class.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        public SCTPStream(ushort streamId)
        {
            this.StreamId = streamId;
            this.bytesSent = 0;
            this.bytesReceived = 0;
            this.streamSequenceNo = 0;
        }

        /// <summary>
        /// Gets the stream id.
        /// </summary>
        public ushort StreamId { get; }

        /// <summary>
        /// Gets the number of bytes sent.
        /// </summary>
        public long BytesSent => this.bytesSent;

        /// <summary>
        /// Gets the number of bytes received.
        /// </summary>
        public long BytesReceived => this.bytesReceived;

        /// <summary>
        /// Gets the current stream sequence number.
        /// </summary>
        public ushort StreamSequenceNo => Convert.ToUInt16(this.streamSequenceNo);

        /// <summary>
        /// Increments the bytes received.
        /// </summary>
        /// <param name="bytes"></param>
        public void IncrementReceivedBytes(long bytes)
        {
            Interlocked.Add(ref this.bytesReceived, bytes);
        }

        /// <summary>
        /// Increments the bytes sent.
        /// </summary>
        /// <param name="bytes"></param>
        public void IncrementSentBytes(long bytes)
        {
            Interlocked.Add(ref this.bytesSent, bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ushort IncrementStreamSequenceNo()
        {
            int comparand;
            int value;

            do
            {
                comparand = this.streamSequenceNo;
                value = ((comparand + 1) % ushort.MaxValue);
            } while (Interlocked.CompareExchange(ref this.streamSequenceNo, value, comparand) != comparand);
            // lock (this)
            // {
            //     this.streamSequenceNo = (ushort)((this.streamSequenceNo + 1) % ushort.MaxValue);
            //     return this.streamSequenceNo;
            // }

            return Convert.ToUInt16(value);
        }
    }

}
