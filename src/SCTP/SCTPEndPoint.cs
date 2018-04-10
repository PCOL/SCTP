namespace SCTP
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Represents an SCTP end point.
    /// </summary>
    public class SCTPEndPoint
        : IDisposable
    {
        /// <summary>
        /// The SCTP socket the endpoint belongs to.
        /// </summary>
        private SCTPSocket sctpSocket;

        /// <summary>
        /// The end points socket.
        /// </summary>
        private Socket socket;

        /// <summary>
        /// The current MTU size.
        /// </summary>
        private int mtu = SCTPConstants.InitialMTUSize;

        /// <summary>
        /// Slow start threshold.
        /// </summary>
        private int ssthresh;

        /// <summary>
        /// A count of the packets lost?
        /// </summary>
        private int lostCount;

        /// <summary>
        /// The last time the end point last sent a packet.
        /// </summary>
        private DateTime lastSentTime;

        /// <summary>
        /// The time that the end point was created.
        /// </summary>
        private DateTime createdTime;

        /// <summary>
        /// The number of bytes in flight.
        /// </summary>
        private int flightSize;

        /// <summary>
        /// The congestion window size;
        /// </summary>
        private int cwnd;

        /// <summary>
        /// The size of the congestion window before processing.
        /// </summary>
        private int previousCwnd;

        /// <summary>
        /// The number of partial bytes acked.
        /// </summary>
        private int partialBytesAcked;

        /// <summary>
        /// Tracks the highest newly acked <c>TSN</c> for the current <c>SACK</c>.
        /// Used in <c>SFR</c> and <c>HTNA</c> algorithms.
        /// </summary>
        private uint currentSackHighestTsn;

        /// <summary>
        /// 
        /// </summary>
        private TimeSpan retransmissionTimeout = SCTPConstants.RTOInitial;

        /// <summary>
        /// The smoothed round trip time.
        /// </summary>
        private int smoothedRoundTripTime;

        /// <summary>
        /// The round trip variation time.
        /// </summary>
        private int RoundTripTimeVariation;

        /// <summary>
        /// 
        /// </summary>
        private uint fastRecoveryTsn;

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPEndPoint"/> class.
        /// </summary>
        /// <param name="sctpSocket"></param>
        /// <param name="endPoint"></param>
        internal SCTPEndPoint(SCTPSocket sctpSocket, IPEndPoint endPoint)
        {
            this.sctpSocket = sctpSocket;
            this.EndPoint = endPoint;
        }

        /// <summary>
        /// Gets or sets the end points address and port.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Gets the current MTU size for this endpoint.
        /// </summary>
        public int MTUSize
        {
            get { return this.mtu; }
        }

        /// <summary>
        /// Gets or sets teh congestion window size.
        /// </summary>
        internal int CongestionWindowSize
        {
            get { return this.cwnd; }
            set
            {
                this.previousCwnd = this.cwnd;
                this.cwnd = value;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[{0}] Congestion Window Size: {1}, Previous: {2}", this.socket.GetHashCode(), this.cwnd, this.previousCwnd);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets or sets the slow start threshold.
        /// </summary>
        internal int SlowStartThreshold
        {
            get { return this.ssthresh; }
            set
            {
                this.ssthresh = value;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[{0}] Slow Start Threshold: {1}", this.socket.GetHashCode(), this.ssthresh);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets or sets the number of partial bytes acked.
        /// </summary>
        internal int PartialBytesAcked
        {
            get
            {
                return this.partialBytesAcked;
            }

            set
            {
                this.partialBytesAcked = value;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[{0}] Partial Bytes Acked: {1}", this.socket.GetHashCode(), this.partialBytesAcked);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets the retransmission timeout.
        /// </summary>
        internal TimeSpan RetransmissionTimeout => retransmissionTimeout;

        /// <summary>
        /// Gets the current flight size.
        /// </summary>
        internal int FlightSize => this.flightSize;

        /// <summary>
        /// Initialises the end point
        /// </summary>
        internal void Initialise()
        {
            if (this.socket == null)
            {
                this.socket = new Socket(this.EndPoint.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                this.socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, true);
                this.socket.Bind(this.EndPoint);

                this.BeginReceiveFrom();
            }

            this.InitialiseCongestionWindowSize();
        }

        /// <summary>
        /// Initialises the congestion window size.
        /// </summary>
        private void InitialiseCongestionWindowSize()
        {
            this.CongestionWindowSize = Math.Min(4 * this.mtu, Math.Max(2 * this.mtu, 4380));
        }

        /// <summary>
        /// Resets the retransmission timeout.
        /// </summary>
        internal void ResetRTO()
        {
            this.retransmissionTimeout = SCTPConstants.RTOInitial;
        }

        /// <summary>
        /// Recalculates the retransmission timeout.
        /// </summary>
        /// <param name="retransmissionExpired">A value indicating whether or not retransmission has expired.</param>
        internal void RecalculateRTO(bool retransmissionExpired)
        {
            if (retransmissionExpired == true)
            {
                this.retransmissionTimeout = TimeSpan.FromTicks(Math.Min(this.retransmissionTimeout.Ticks * 2, SCTPConstants.RTOMax.Ticks));
            }
        }

        /// <summary>
        /// Called when the connection has been established.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point</param>
        internal void Established(IPEndPoint remoteEndPoint)
        {
            this.RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private IAsyncResult BeginReceiveFrom()
        {
            NetworkBuffer buffer = new NetworkBuffer(this.socket, this.mtu);
            AsyncReceiveFrom asyncOp = new AsyncReceiveFrom(buffer, ReceiveFromHandler);
            asyncOp.BeginReceiveFrom(SocketFlags.None);
            return asyncOp;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        private NetworkBuffer EndReceiveFrom(IAsyncResult asyncResult)
        {
            if (asyncResult is AsyncReceiveFrom asyncReceive)
            {
                return asyncReceive.EndReceiveFrom();
            }

            throw new InvalidOperationException("Invalid async result");
        }

        /// <summary>
        /// Handles a received packet.
        /// </summary>
        /// <param name="asyncResult">The result of the async operation.</param>
        private void ReceiveFromHandler(IAsyncResult asyncResult)
        {
            try
            {
                NetworkBuffer buffer = this.EndReceiveFrom(asyncResult);
                SCTPPacket packet = SCTPPacket.FromArray(buffer.Buffer, 0, buffer.BytesReceived);
                if (packet != null)
                {
                    packet.ReceivedFrom = buffer.ReceivedFrom;
                    this.sctpSocket.ProcessInboundPacket(packet);
                }

                this.BeginReceiveFrom();
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10040)
                {
                    // Message too long.
                    // reduce mtu...
                }
                
                sctpSocket.WriteConsole(ConsoleColor.Red, ex.ToString());
                Environment.FailFast("-------------------------", ex);
            }
            catch (Exception ex)
            {
                sctpSocket.WriteConsole(ConsoleColor.Red, ex.ToString());
                Environment.FailFast("-------------------------", ex);
            }
        }

        /// <summary>
        /// Validate a packet.
        /// </summary>
        /// <param name="buffer">The buffer containing the packet.</param>
        /// <param name="offset">The offset of the packet within the buffer.</param>
        /// <param name="length">The length of the packet within the buffer.</param>
        /// <param name="checksum">The packet checksum.</param>
        /// <returns>True if the packet if valid; otherwise false.</returns>
        private bool ValidatePacket(byte[] buffer, int offset, int length, uint checksum)
        {
            // check packet against checksum.
            return true;
        }

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        internal void SendPacket(SCTPPacket packet)
        {
            this.SendPacket(packet, this.RemoteEndPoint);
        }

        /// <summary>
        /// Sends the packet to the end point.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="destinationAddress"></param>
        internal void SendPacket(SCTPPacket packet, IPEndPoint destinationAddress)
        {
            NetworkBuffer buffer = new NetworkBuffer(this.socket, packet.ToArray());
            buffer.SendTo(SocketFlags.None, destinationAddress);

            // Increment the flight size.
            Interlocked.Increment(ref this.flightSize);
        }

        /// <summary>
        /// Disposes the end point.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// clean up.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the instance is being disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                if (this.socket != null)
                {
                    this.socket.Shutdown(SocketShutdown.Both);
                    this.socket.Dispose();
                    this.socket = null;
                }
            }
        }
    }
}
