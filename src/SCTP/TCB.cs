namespace SCTP
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Represents the transmission control block.
    /// </summary>
    internal class TCB
    {
        /// <summary>
        /// The socket the control block belongs to.
        /// </summary>
        private SCTPSocket socket;

        /// <summary>
        /// The socket state.
        /// </summary>
        private int state;

        /// <summary>
        /// The initial TSN value.
        /// </summary>
        private uint initialTSN;

        /// <summary>
        /// The next TSN value
        /// </summary>
        private uint nextTSN;

        /// <summary>
        /// Peer receive window size.
        /// </summary>
        private uint peerRWnd;

        /// <summary>
        /// A stream sequence generator.
        /// </summary>
        private StreamSequenceGenerator sequenceGenerator;

        /// <summary>
        /// The streams associated with the socket.
        /// </summary>
        private ConcurrentDictionary<ushort, SCTPStream> streams;

        /// <summary>
        /// A lock object.
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// Initialises a new instance of the <see cref="TCB"/> class.
        /// </summary>
        /// <param name="socket">The owning SCTP socket.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="peerInitialTSN">The peers initial TSN.</param>
        public TCB(SCTPSocket socket, IPEndPoint localEndPoint)
        {
            this.socket = socket;
            this.State = SCTPSocketState.Closed;
            this.CreationTime = DateTime.UtcNow;
            this.Lifetime = SCTPConstants.ValidCookieLifetime;
            this.SourceEndPoint = localEndPoint;
            this.streams = new ConcurrentDictionary<ushort, SCTPStream>();
            this.sequenceGenerator = new StreamSequenceGenerator();
            this.ReceiveWindowSize = SCTPConstants.DefaultReceiverWindowSize;

            Random rnd = new Random();
            uint itag = (uint)rnd.Next(int.MinValue, int.MaxValue);
            this.LocalVerificationTag = itag;
            this.initialTSN = itag;
            this.nextTSN = this.initialTSN;
            this.LastCumulativeTSNAcked = Convert.ToUInt32(this.nextTSN - 1);

            this.socket.WriteConsole(string.Format("Initial TSN {0}", this.initialTSN));
        }

        /// <summary>
        /// Gets the TCB's creation time.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Gets the TCB's lifetime value.
        /// </summary>
        public TimeSpan Lifetime { get; private set; }

        /// <summary>
        /// Gets the source end point.
        /// </summary>
        public IPEndPoint SourceEndPoint { get; private set; }

        /// <summary>
        /// Gets the destination end point.
        /// </summary>
        public IPEndPoint DestinationEndPoint { get; set; }

        /// <summary>
        /// Gets the local verification tag.
        /// </summary>
        public uint LocalVerificationTag { get; private set; }

        /// <summary>
        /// Gets or sets the peer verification tag.
        /// </summary>
        public uint PeerVerificationTag { get; set; }

        /// <summary>
        /// Gets or sets overall error count.
        /// </summary>
        public int OverallErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the threshold for overall error count. If this value is reached the association should be torn down.
        /// </summary>
        public int OverallErrorThreshold { get; set; }

        /// <summary>
        /// Gets or set the local receive window size.
        /// </summary>
        public uint ReceiveWindowSize { get; set; }

        /// <summary>
        /// Gets or sets the peers receive window size (<c>rwnd</c>).
        /// </summary>
        public uint PeerReceiveWindowSize
        {
            get
            {
                return this.peerRWnd;
            }

            set
            {
                this.peerRWnd = value;
            }
        }

        /// <summary>
        /// Gets the next TSN value.
        /// </summary>
        public uint NextTSN => this.nextTSN;

        /// <summary>
        /// Gets the last received cumulative TSN.
        /// </summary>
        public uint LastCumulativeTSNReceived { get; private set; }

        /// <summary>
        /// Gets or sets the highest TSN received so far.
        /// </summary>
        public uint HighestReceivedTSN { get; private set; }

        /// <summary>
        /// Gets or sets the last cumulative <c>TSN</c> acked.
        /// </summary>
        public uint LastCumulativeTSNAcked { get; set; }

        /// <summary>
        /// Gets the ack state (a flag indicating whether or not to send a SACK)
        /// </summary>
        public int AckState { get; private set; }

        /// <summary>
        /// Gets or set the smallest Path MTU value discovered for all of the peers transport addresses.
        /// </summary>
        public int SmallestPMTU { get; set; }

        /// <summary>
        /// Gets or sets the state of the socket.
        /// </summary>
        public SCTPSocketState State
        {
            get
            {
                return (SCTPSocketState)this.state;
            }

            set
            {
                this.state = (int)value;
            }
        }

        /// <summary>
        /// Sets the initial peer TSN value.
        /// </summary>
        /// <param name="tsn"></param>
        public void SetInitialPeerTSN(uint tsn)
        {
            lock (this.lockObj)
            {
                this.LastCumulativeTSNReceived = tsn - 1;
            }
        }

        /// <summary>
        /// Updates the TSN received.
        /// Updates the highest TSN.
        /// If the TSN is in sequence, updates the last received in sequence value.
        /// </summary>
        /// <param name="tsn">The TSN</param>
        /// <returns>True if the value was updated; otherwise false.</returns>
        public bool UpdateTSNReceived(uint tsn)
        {
            lock (this.lockObj)
            {
                if (tsn > this.HighestReceivedTSN)
                {
                    this.HighestReceivedTSN = tsn;
                }

                if (tsn == (this.LastCumulativeTSNReceived + 1))
                {
                    this.LastCumulativeTSNReceived = tsn;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Updates the <c>TSN</c> acked.
        /// </summary>
        /// <param name="tsn"></param>
        /// <returns></returns>
        public bool UpdateTSNAcked(uint tsn)
        {
            lock (this.lockObj)
            {
                if (tsn == (this.LastCumulativeTSNAcked + 1))
                {
                    this.LastCumulativeTSNAcked = tsn;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resets the ack state.
        /// </summary>
        /// <returns></returns>
        public void ResetAckState()
        {
            lock (this.lockObj)
            {
                this.AckState = 0;
            }
        }

        /// <summary>
        /// Incrments the ack state.
        /// </summary>
        /// <returns></returns>
        public bool IncrementAckState()
        {
            lock (this.lockObj)
            {
                return (++this.AckState % 2 == 0);
            }
        }

        /// <summary>
        /// Gets a stream by id. If the stream does not exist then it is added.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        private SCTPStream GetStream(ushort streamId)
        {
            return this.streams.GetOrAdd(streamId, (id) => { return new SCTPStream(id); });
        }

        /// <summary>
        /// Increments the number of bytes received for a stream.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="bytes">The number of bytes.</param>
        public void IncrementBytesReceived(ushort streamId, long bytes)
        {
            var stream = this.GetStream(streamId);
            stream.IncrementReceivedBytes(bytes);
        }

        /// <summary>
        /// Increments the bytes sent for a stream.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="bytes">The number of bytes.</param>
        public void IncrementBytesSent(ushort streamId, long bytes)
        {
            var stream = this.GetStream(streamId);
            stream.IncrementSentBytes(bytes);
        }

        /// <summary>
        /// Check if a verification tag is valid against the local verificaton tag.
        /// </summary>
        /// <param name="verificationTag">The verification tag to check.</param>
        /// <returns>True if they match; otherwise false.</returns>
        public bool IsVerificationTagOK(uint verificationTag)
        {
            return this.LocalVerificationTag == verificationTag;
        }

        /// <summary>
        /// Increments the TSN and returns the value for assigning.
        /// </summary>
        /// <returns></returns>
        public uint IncrementTSN()
        {
            lock (this.lockObj)
            {
                uint tsn = this.nextTSN;
                this.nextTSN = unchecked(this.nextTSN + 1);
                return tsn;
            }
        }

        /// <summary>
        /// Gets the next sequence number for a given stream.
        /// </summary>
        /// <param name="streamId"></param>
        /// <returns></returns>
        public ushort GetNextStreamSequenceNo(int streamId)
        {
            return this.sequenceGenerator.NextSequence(streamId);
        }

        /// <summary>
        /// Compares the current state with a passed in state for equality and
        /// if they are equal, replaces the state with the passed in value.
        /// </summary>
        /// <param name="value">The passed in state.</param>
        /// <param name="comparand">The state that is compared to the current state.</param>
        /// <returns></returns>
        public SCTPSocketState CompareExchangeState(SCTPSocketState value, params SCTPSocketState[] comparands)
        {
            Utility.ThrowIfArgumentNullOrEmpty("comparands", comparands);

            SCTPSocketState state = (SCTPSocketState)this.state;

            for (int i = 0; i < comparands.Length; i++)
            {
                state = (SCTPSocketState)Interlocked.CompareExchange(ref this.state, (int)value, (int)comparands[i]);
                if (comparands.Contains(state) == true)
                {
                    break;
                }
            }

            return state;
        }

        /// <summary>
        /// Waits a specified amount of time for the TCB to have a specified state.
        /// </summary>
        /// <param name="state">The state to wait for.</param>
        /// <param name="timeout">The amount of time to wait.</param>
        /// <returns>True if the state is met within the timeout period; otherwise false.</returns>
        public bool WaitForState(SCTPSocketState state, int timeout)
        {
            return this.WaitForStates(new SCTPSocketState[] { state }, timeout, out SCTPSocketState s);
        }

        /// <summary>
        /// Waits a specified amount of time for the TCB to have a state from a list of specified states.
        /// </summary>
        /// <param name="states">A list of states to wait for.</param>
        /// <param name="timeout">The amount of time to wait.</param>
        /// <param name="state">The state on exit.</param>
        /// <returns>True if one of the states is met within the timeout period; otherwise false.</returns>
        public bool WaitForStates(SCTPSocketState[] states, int timeout, out SCTPSocketState state)
        {
            state = this.State;
            DateTime timeoutTime = DateTime.UtcNow.AddMilliseconds(timeout);
            while (timeout == -1 || DateTime.UtcNow < timeoutTime)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    state = this.State;
                    if (state == states[i])
                    {
                        return true;
                    }
                }

                Thread.Sleep(100);
            }

            return false;
        }

        /// <summary>
        /// Converts the TCB into a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] buffer = new byte[40];

            byte[] sourceAddress = this.SourceEndPoint.Address.GetAddressBytes();
            byte[] destinationAddress = this.DestinationEndPoint.Address.GetAddressBytes();

            int offset = 0;
            offset += NetworkHelpers.CopyTo(this.CreationTime.Ticks, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.Lifetime.Ticks, buffer, offset);
            offset += NetworkHelpers.CopyTo(sourceAddress.Length, buffer, offset);
            offset += NetworkHelpers.CopyTo(sourceAddress, buffer, offset, false);
            offset += NetworkHelpers.CopyTo(this.SourceEndPoint.Port, buffer, offset);
            offset += NetworkHelpers.CopyTo(destinationAddress.Length, buffer, offset);
            offset += NetworkHelpers.CopyTo(destinationAddress, buffer, offset, false);
            offset += NetworkHelpers.CopyTo(this.DestinationEndPoint.Port, buffer, offset);

            return buffer;
        }

        /// <summary>
        /// Extracts a TCB from a byte array.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset of the TCB.</param>
        /// <param name="length">A variable to receive the length.</param>
        /// <returns>The TCB.</returns>
        public static TCB FromArray(byte[] buffer, int offset, out int length)
        {
            length = 0;
            return null;
        }
    }
}
