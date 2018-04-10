[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestApp")]

namespace SCTP
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SCTP.Chunks;

    /// <summary>
    /// Represents
    /// </summary>
    public class SCTPSocket
        : IDisposable
    {
        /// <summary>
        /// Message recieved event.
        /// </summary>
        private event EventHandler<SCTPMessageReceivedEventArgs> messageReceived;

        /// <summary>
        /// A value indicating whether or not the instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// List of end points.
        /// </summary>
        private List<SCTPEndPoint> endpoints;

        /// <summary>
        /// The primary end point.
        /// </summary>
        private SCTPEndPoint primaryEndpoint;

        /// <summary>
        /// The local port.
        /// </summary>
        private int port;

        /// <summary>
        ///
        /// </summary>
        private List<DataChunk> transmitQueue;

        /// <summary>
        ///
        /// </summary>
        private Dictionary<uint, DataChunk> transmittedQueue;

        /// <summary>
        ///
        /// </summary>
        private SortedList<uint, Chunk> inboundQueue;

        /// <summary>
        ///
        /// </summary>
        private ConcurrentDictionary<int, InboundMessageQueue> inboundStreamQueues;

        /// <summary>
        /// The duplicate <c>TSN</c> counter.
        /// </summary>
        private ConcurrentDictionary<uint, int> duplicateTSNs;

        /// <summary>
        /// A sorted list of acked <c>TSN's</c>
        /// </summary>
        private SortedSet<uint> ackedTSNs;

        /// <summary>
        ///
        /// </summary>
        private object recieveLock = new object();

        /// <summary>
        ///
        /// </summary>
        private TCB tcb;

        /// <summary>
        ///
        /// </summary>
        private Timer initTimer;

        /// <summary>
        ///
        /// </summary>
        private Timer cookieTimer;

        /// <summary>
        ///
        /// </summary>
        private Timer shutdownTimer;

        /// <summary>
        ///
        /// </summary>
        private Timer retransmissionTimer;

        /// <summary>
        ///
        /// </summary>
        private Timer acknowledgeTimer;

        /// <summary>
        ///
        /// </summary>
        private SCTPPacket currentStateControlPacket;

        /// <summary>
        ///
        /// </summary>
        private int currentStateRetryCount;

        /// <summary>
        ///
        /// </summary>
        private object transmitLock = new object();

        /// <summary>
        ///
        /// </summary>
        private byte[] secretKey;

        /// <summary>
        ///
        /// </summary>
        private object consoleLock = new object();

        /// <summary>
        /// Initialises a new intance of the <see cref="SCTPScoket"/> class.
        /// </summary>
        public SCTPSocket(int port)
        {
            this.endpoints = new List<SCTPEndPoint>();
            this.transmitQueue = new List<DataChunk>(100);
            this.transmittedQueue = new Dictionary<uint, DataChunk>(100);
            this.inboundQueue = new SortedList<uint, Chunk>(100);
            this.inboundStreamQueues = new ConcurrentDictionary<int, InboundMessageQueue>();
            this.duplicateTSNs = new ConcurrentDictionary<uint, int>();
            this.ackedTSNs = new SortedSet<uint>();
            this.port = port;

            this.initTimer = new Timer("initTimer", SCTPConstants.RTOInitial, this.InitTimerExpired);
            this.cookieTimer = new Timer("cookieTimer", SCTPConstants.RTOInitial, this.CookieTimerExpired);
            this.shutdownTimer = new Timer("shutdownTimer", SCTPConstants.RTOInitial, this.ShutdownTimerExpired);
            this.retransmissionTimer = new Timer("retransmissionTimer", SCTPConstants.RTOInitial, this.RetransmissionTimerExpired);
            this.acknowledgeTimer = new Timer("acknowledgeTimer", TimeSpan.FromMilliseconds(200), this.AcknowledgeTimerExpired);

            this.GenerateKey();
        }

        /// <summary>
        /// An event that is triggered when a message is received.
        /// </summary>
        public event EventHandler<SCTPMessageReceivedEventArgs> MessageReceived
        {
            add
            {
                this.messageReceived += value;
            }

            remove
            {
                this.messageReceived -= value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the socket is connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                return this.tcb != null && this.tcb.State == SCTPSocketState.Established;
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal uint PeerReceiveWindowSize
        {
            get
            {
                return this.tcb.PeerReceiveWindowSize;
            }

            set
            {
                this.tcb.PeerReceiveWindowSize = value;
            }
        }

        public int MTUSize => this.primaryEndpoint.MTUSize;

        /// <summary>
        /// Binds an address to the socket.
        /// </summary>
        /// <param name="address"></param>
        public void Bind(IPAddress address)
        {
            SCTPEndPoint ep = new SCTPEndPoint(this, new IPEndPoint(address, this.port));
            this.endpoints.Add(ep);

            if (this.primaryEndpoint == null)
            {
                this.primaryEndpoint = ep;
            }

            ep.Initialise();
        }

        /// <summary>
        /// Unbinds an address from the socket.
        /// </summary>
        /// <param name="address"></param>
        public void Unbind(IPAddress address)
        {
            this.Unbind(new IPEndPoint(address, this.port));
        }

        /// <summary>
        /// Unbinds an end point from the socket.
        /// </summary>
        /// <param name="endPoint">The end point</param>
        private void Unbind(IPEndPoint endPoint)
        {
            SCTPEndPoint ep = this.endpoints.FirstOrDefault((sep) => { return sep.EndPoint == endPoint; });
            if (ep != null)
            {
                this.endpoints.Remove(ep);

                if (this.primaryEndpoint == ep)
                {
                    this.primaryEndpoint = this.endpoints.FirstOrDefault();
                }

                ep.Dispose();
            }
        }

        /// <summary>
        /// Connects to a remote <c>SCTP</c> end point.
        /// </summary>
        /// <param name="address">The address</param>
        /// <param name="port">The port.</param>
        public bool Connect(IPAddress address, int port)
        {
            return this.Connect(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Connects to a remote <c>SCTP</c> end point.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        public bool Connect(IPEndPoint endPoint)
        {
            if (this.tcb!= null)
            {
                if (this.Connected == true)
                {
                    throw new Exception("Already connected");
                }

                throw new Exception("Connection already initiated");
            }

            this.WriteConsole(string.Format("Connecting to {0}", endPoint.ToString()));

            this.primaryEndpoint.ResetRTO();

            this.tcb = new TCB(this, this.primaryEndpoint.EndPoint)
            {
                DestinationEndPoint = endPoint
            };

            SCTPSocketState previousState = this.tcb.CompareExchangeState(SCTPSocketState.CookieWait, SCTPSocketState.Closed, SCTPSocketState.Aborted);
            if (previousState == SCTPSocketState.Closed ||
                previousState == SCTPSocketState.Aborted)
            {
                string hostNameAddress = this.primaryEndpoint.EndPoint.Address.ToString();

                SCTPPacket connectPacket = SCTPPacket.Create(
                    Convert.ToUInt16(this.primaryEndpoint.EndPoint.Port),
                    Convert.ToUInt16(endPoint.Port),
                    0,
                    new InitChunk(false)
                    {
                        InitiateTag = this.tcb.LocalVerificationTag,
                        AdvertisedReceiverWindowCredit = 64 * 1024,
                        OutboundStreams = 1,
                        InboundStreams = 1,
                        InitialTSN = this.tcb.NextTSN,
                        TransmitCount = 1,
                        FirstTransmitTimestamp = Stopwatch.GetTimestamp(),
                        Parameters = new List<ChunkParameter>()
                        {
                            new ChunkParameter()
                            {
                                Type = ChunkParameterType.HostNameAddress,
                                Length = (ushort)(4 + hostNameAddress.Length),
                                Value = UTF8Encoding.UTF8.GetBytes(hostNameAddress)
                            }
                        }
                    });

                this.WriteConsole(string.Format("Sending INIT to {0}", endPoint.ToString()));

                this.primaryEndpoint.SendPacket(connectPacket, endPoint);

                this.SetCurrentControlPacket(connectPacket);

                this.initTimer.Start();
            }

            SCTPSocketState state;
            if (this.tcb.WaitForStates(new SCTPSocketState[] { SCTPSocketState.Established, SCTPSocketState.Aborted }, 10000, out state) == false)
            {
                this.Aborted();
                return false;
            }

            this.WriteConsole("Connected");
            return true;
        }

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        /// <param name="errorCauses">A list of error causes.</param>
        public void Abort(params ErrorCause[] errorCauses)
        {
            if (this.tcb == null)
            {
                return;
            }

            this.SendAbort(this.primaryEndpoint, errorCauses);

        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Shutdown()
        {
            if (this.tcb == null)
            {
                return;
            }

            if (this.tcb.CompareExchangeState(SCTPSocketState.ShutdownPending, SCTPSocketState.Established) != SCTPSocketState.Established)
            {
                Aborted();
                return;
            }

            // Build the shutdown packet.
            SCTPPacket shutdownPacket = this.CreatePacket(
                new ShutdownChunk(false)
                {
                    CumulativeTSNAck = this.tcb.LastCumulativeTSNReceived
                });

            // Send the shutdown packet.
            this.primaryEndpoint.SendPacket(shutdownPacket);

            // Stash the current control packet is case retransmission is required.
            this.SetCurrentControlPacket(shutdownPacket);

            // Start the shutdown timer.
            this.shutdownTimer.Start();

            // Set the state to shutdown sent.
            this.tcb.State = SCTPSocketState.ShutdownSent;

            // Wait for shutdown to complete or abort.
            SCTPSocketState actualState;
            this.tcb.WaitForStates(new SCTPSocketState[] { SCTPSocketState.Closed, SCTPSocketState.Aborted }, 30000, out actualState);

            this.tcb = null;
        }

        /// <summary>
        /// Closes the socket and releases all resources.
        /// </summary>
        public void Close()
        {
            this.Shutdown();
            this.Unbind();

            this.primaryEndpoint = null;
        }

        /// <summary>
        /// Unbinds all of the bound end points.
        /// </summary>
        private void Unbind()
        {
            while (this.endpoints != null && this.endpoints.Count > 0)
            {
                this.Unbind(this.endpoints[0].EndPoint);
            }
        }

        /// <summary>
        /// Creates a packet using the sockets <c>TCB</c>.
        /// </summary>
        /// <param name="chunks">The chunks.</param>
        /// <returns>An <see cref="SCTPPacket"/>.</returns>
        private SCTPPacket CreatePacket(params Chunk[] chunks)
        {
            return this.CreatePacket(this.tcb, chunks);
        }

        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="verificationTag">The verification tag.</param>
        /// <param name="chunks">The chunks.</param>
        /// <returns>An <see cref="SCTPPacket"/>.</returns>
        private SCTPPacket CreatePacket(uint? verificationTag, params Chunk[] chunks)
        {
            return this.CreatePacket(this.tcb, verificationTag, chunks);
        }

        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="tcb">The <c>TCB</c></param>
        /// <param name="chunks">The chunks</param>
        /// <returns>An <see cref="SCTPPacket"/>.</returns>
        private SCTPPacket CreatePacket(TCB tcb, params Chunk[] chunks)
        {
            return this.CreatePacket(tcb, null, chunks);
        }

        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="tcb">The <c>TCB</c></param>
        /// <param name="verificationTag">The verification tag.</param>
        /// <param name="chunks">The chunks</param>
        /// <returns>An <see cref="SCTPPacket"/>.</returns>
        private SCTPPacket CreatePacket(TCB tcb, uint? verificationTag, params Chunk[] chunks)
        {
            Utility.ThrowIfArgumentNull("tcb", tcb);

            return SCTPPacket.Create(
                (ushort)tcb.SourceEndPoint.Port,
                (ushort)tcb.DestinationEndPoint.Port,
                verificationTag.HasValue == true ? verificationTag.Value : tcb.PeerVerificationTag,
                chunks);
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="buffer">The message buffer.</param>
        /// <returns>True if the message has been transmitted; otherwise false.</returns>
        public Task<bool> SendMessageAsync(int streamId, byte[] buffer)
        {
            return this.SendMessageAsync(new SCTPMessage(streamId, -1, buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Sends part of a buffer as a message.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="buffer">The buffer to send from.</param>
        /// <param name="offset">The offset into buffer.</param>
        /// <param name="length">The number of bytes to send.</param>
        /// <returns>True if the message has been transmitted; otherwise false.</returns>
        public Task<bool> SendMessageAsync(int streamId, byte[] buffer, int offset, int length)
        {
            return this.SendMessageAsync(new SCTPMessage(streamId, -1, buffer, offset, length));
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message to transmit.</param>
        /// <returns>True if the message has been transmitted; otherwise false.</returns>
        public Task<bool> SendMessageAsync(SCTPMessage message)
        {
            if (this.tcb == null || this.tcb.State != SCTPSocketState.Established)
            {
                throw new Exception("Not Connected");
            }

            lock (this.transmitLock)
            {
                ushort seqNo = this.tcb.GetNextStreamSequenceNo(message.StreamId);
                message.ToChunks(this.tcb.IncrementTSN, seqNo, this.primaryEndpoint.MTUSize);
                message.AddChunksToTransmitQueue(this.transmitQueue);
            }

            while (this.TransmitData() == true)
            {
                Thread.SpinWait(1);
            }

            return Task.FromResult(true);
        }

        private void SendAbort(SCTPEndPoint endPoint, params ErrorCause[] causes)
        {
            this.SendAbort(endPoint, null, null, causes);
        }

        private void SendAbort(SCTPEndPoint endPoint, Chunk[] chunks, params ErrorCause[] causes)
        {
            this.SendAbort(endPoint, null, chunks, causes);
        }

        private void SendAbort(SCTPEndPoint endPoint, uint? vericationTag, params ErrorCause[] causes)
        {
            this.SendAbort(endPoint, vericationTag, null, causes);
        }

        /// <summary>
        /// Sends an abort to the remote end point.
        /// </summary>
        /// <param name="endPoint">The end point to send the abort to.</param>
        /// <param name="verificationTag"></param>
        /// <param name="chunks">Any extra chunks to bundle with the abort.</param>
        /// <param name="causes">Error causes</param>
        private void SendAbort(SCTPEndPoint endPoint, uint? verificationTag, Chunk[] chunks, params ErrorCause[] causes)
        {
            SCTPPacket packet = this.CreatePacket(verificationTag);

            AbortChunk abort = new AbortChunk()
            {
                Flags = (byte)(verificationTag.HasValue == true ? 1 : 0)
            };


            if (chunks != null)
            {
                // DATA chunks MUST NOT be bundled with ABORT.
                // Control chunks (except for INIT, INIT ACK, and SHUTDOWN COMPLETE) MAY be bundled with an ABORT, but they MUST be
                // placed before the ABORT in the SCTP packet or they will be ignored by the receiver.
                foreach (var chunk in chunks)
                {
                    if (chunk.Type != ChunkType.Data &&
                        chunk.Type != ChunkType.Init &&
                        chunk.Type != ChunkType.InitAck &&
                        chunk.Type != ChunkType.ShutdownComplete)
                    {
                        packet.AddChunk(chunk);
                    }
                }
            }

            packet.AddChunk(abort);

            (endPoint ?? this.primaryEndpoint).SendPacket(packet);
        }

        /// <summary>
        /// Builds a packet.
        /// </summary>
        /// <param name="mtuSize">The current MTU size.</param>
        /// <returns>The packet if there is anything to transmit; otherwise null.</returns>
        private SCTPPacket BuildPacket(int mtuSize)
        {
            lock (transmitLock)
            {
                SCTPPacket packet = null;
                var retransmittable = this.transmittedQueue
                    .Where(kvp => kvp.Value.AckCount == 0)
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Value);

                if (retransmittable.Any() == true)
                {
                    this.WriteConsole(ConsoleColor.DarkYellow, $"Retransmit {retransmittable.Count()}");
                    BuildPacket(retransmittable, mtuSize, ref packet);
                }

                if (this.transmitQueue.Any() == true)
                {
                    var count = BuildPacket(this.transmitQueue, mtuSize, ref packet);
                    this.WriteConsole(ConsoleColor.DarkYellow, $"Transmitting {count} chunks");
                    for (int i = 0; i < count; i++)
                    {
                        DataChunk chunk = this.transmitQueue[0];
                        this.transmittedQueue.Add(chunk.TSN, chunk);
                        this.transmitQueue.RemoveAt(0);
                    }
                }

                return packet;
            }
        }

        /// <summary>
        /// Builds a packet for transmission.
        /// </summary>
        /// <param name="chunks">The chunk list to take chunks from.</param>
        /// <param name="mtuSize">The current MTU size.</param>
        /// <param name="packet">The packet to add the chunks to.</param>
        /// <returns>The number of chunks added.</returns>
        private int BuildPacket(IEnumerable<DataChunk> chunks, int mtuSize, ref SCTPPacket packet)
        {
            lock (this.transmitLock)
            {
                if (chunks.Any() == false)
                {
                    return 0;
                }

                int count = 0;
                DataChunk chunk;
                while ((chunk = chunks.Skip(count).FirstOrDefault()) != null)
                {
                    int chunkLength = chunk.CalculateLength();
                    if (packet != null &&
                        packet.Size + chunkLength >= mtuSize)
                    {
                        return count;
                    }

                    if (packet == null)
                    {
                        packet = this.CreatePacket();
                    }

                    packet.AddChunk(chunk);

                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Transmit data.
        /// </summary>
        /// <returns></returns>
        private bool TransmitData()
        {
            if (this.tcb == null ||
                tcb.State != SCTPSocketState.Established)
            {
                // No connection so don't try
                // to transmit.
                return false;
            }

            if (this.transmitQueue.Any() == false)
            {
                // Nothing to transmit.
                return false;
            }

            if (this.tcb.PeerReceiveWindowSize == 0)
            {
                // Peer receive window size is 0.

                return false;
            }

            SCTPPacket packet = this.BuildPacket(this.primaryEndpoint.MTUSize);

            this.TransmitPacket(packet);

            this.retransmissionTimer.Start();

            return true;
        }

        /// <summary>
        /// Transmits a packet.
        /// </summary>
        /// <param name="packet">The pack to transmit.</param>
        private void TransmitPacket(SCTPPacket packet)
        {
            this.tcb.PeerReceiveWindowSize -= (uint)packet.Size;
            this.primaryEndpoint.SendPacket(packet);
        }

        /// <summary>
        /// Processes an inbound packet.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        internal void ProcessInboundPacket(SCTPPacket packet)
        {
            this.WriteConsole(ConsoleColor.Yellow, $"Processing inbound packet");

            // Check if the packet is valid
            if (this.IsPacketValid(packet) == false)
            {
                // If not then silently ignore...
                return;
            }

            int dataChunkCount = 0;
            int dataChunksAdded = 0;
            bool sendAck = false;
            foreach (var chunk in packet.Chunks)
            {
                if (chunk.Type == ChunkType.Data)
                {
                    dataChunkCount++;
                    if (this.ProcessDataChunk((DataChunk)chunk, packet.ReceivedFrom, out bool ackDataChunk) == true)
                    {
                        dataChunksAdded++;
                    }

                    if (ackDataChunk == true)
                    {
                        sendAck = true;
                    }
                }
                else
                {
                    // Process a control chunk.
                    this.ProcessControlChunk(packet, chunk);
                }
            }

            this.WriteConsole(ConsoleColor.Cyan, $"SendAck: {sendAck}, DataChunkCount: {dataChunkCount}, DataChunksAdded: {dataChunksAdded}");

            // Do we need to ack or were all DATA chunks NOT processed?
            if (sendAck == true ||
                (dataChunkCount > 0 &&
                dataChunkCount != dataChunksAdded))
            {
                // TODO: Send ACK immediately.
                this.SendAcks();
            }
            else if (dataChunkCount > 0)
            {
                // Start the acknowledge timer (if not already running).
                this.acknowledgeTimer.Start();
            }
        }

        /// <summary>
        /// Checks if the packet is valid.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns>True if the packet is valid; otherwise false.</returns>
        private bool IsPacketValid(SCTPPacket packet)
        {
            if (packet == null ||
                packet.Chunks == null)
            {
                this.WriteConsole("Discarding Invalid Packet - null packet or no chunks");
                return false;
            }

            // No Verificaton Tag, MUST be an INIT packet...
            if (packet.Header.VerificationTag == 0)
            {
                // Check that the packet contains ONLY an INIT chunk.
                if (packet.Chunks.Count != 1 ||
                    packet.Chunks[0].Type != ChunkType.Init)
                {
                    this.WriteConsole("Discarding Invalid Packet - Verification Tag == 0 and packet does not contain a single INIT chunk");
                    return false;
                }

                return true;
            }

            if (this.tcb == null)
            {
                this.WriteConsole("Discarding Invalid Packet - TCB is null");
                return false;
            }

            Chunk chunk = packet.Chunks.FirstOrDefault((c) => { return c.Type == ChunkType.Abort || c.Type == ChunkType.ShutdownComplete; });
            if (chunk != null)
            {
                if (!((chunk.Flags & 1) == 1 && packet.Header.VerificationTag == this.tcb.LocalVerificationTag) &&
                    !((chunk.Flags & 1) == 0 && packet.Header.VerificationTag == this.tcb.PeerVerificationTag))
                {
                    this.WriteConsole(string.Format("Discarding Invalid Packet - Incorrect {0} chunk verification tag", chunk.Type));
                    return false;
                }
            }
            else if (packet.Header.VerificationTag != this.tcb.LocalVerificationTag)
            {
                this.WriteConsole("Discarding Invalid Packet - TCB is null or packet contains incorrect verification tag");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Process control chunk.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="chunk">The control chunk.</param>
        private void ProcessControlChunk(SCTPPacket packet, Chunk chunk)
        {
            try
            {
                if (chunk.Type == ChunkType.Sack)
                {
                    ProcessSelectiveAck((SelectiveAckChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.Init)
                {
                    ProcessInit((InitChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.InitAck)
                {
                    ProcessInitAck((InitChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.CookieEcho)
                {
                    ProcessCookieEcho((CookieEchoChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.CookieAck)
                {
                    ProcessCookieAck((CookieAckChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.Heartbeat)
                {
                    this.ProcessHeartbeat((HeartbeatChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.HeartbeatAck)
                {
                    this.ProcessHeartbeatAck((HeartbeatChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.Shutdown)
                {
                    this.ProcessShutdown((ShutdownChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.ShutdownAck)
                {
                    this.ProcessShutdownAck(packet.Header.VerificationTag, (ShutdownChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.ShutdownComplete)
                {
                    this.ProcessShutdownComplete((ShutdownCompleteChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.Error)
                {
                    this.ProcessError((ErrorChunk)chunk, packet.ReceivedFrom);
                }
                else if (chunk.Type == ChunkType.Abort)
                {
                    this.ProcessAbort((AbortChunk)chunk, packet.ReceivedFrom);
                }
            }
            catch (Exception ex)
            {
                this.WriteConsole(string.Format("ERROR: {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Processes an <c>INIT</c> chunk.
        /// </summary>
        /// <param name="initChunk">An <c>INIT</c> chunk.</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        private void ProcessInit(InitChunk initChunk, IPEndPoint remoteEndPoint)
        {
            if (this.tcb != null && this.tcb.State != SCTPSocketState.Closed)
            {
                this.WriteConsole("Init Received in wrong state");

                this.Aborted();
                return;
            }

            this.WriteConsole("Init Received");

            // Setup the TCB.
            this.tcb = new TCB(this, this.primaryEndpoint.EndPoint)
            {
                DestinationEndPoint = remoteEndPoint,
                PeerVerificationTag = initChunk.InitiateTag
            };

            this.tcb.SetInitialPeerTSN(initChunk.InitialTSN);

            InitChunk initAckChunk = new InitChunk(true)
            {
                InitiateTag = this.tcb.LocalVerificationTag,
                AdvertisedReceiverWindowCredit = 64 * 1024,
                OutboundStreams = 1,
                InboundStreams = 1,
                InitialTSN = this.tcb.NextTSN,
                TransmitCount = 1,
                FirstTransmitTimestamp = Stopwatch.GetTimestamp()
            };

            initAckChunk.AddParameter(this.CreateStateCookie(this.tcb));

            SCTPPacket initAckPacket = this.CreatePacket(initAckChunk);

            this.WriteConsole("Sending Init Ack");
            this.primaryEndpoint.SendPacket(initAckPacket, remoteEndPoint);

        }

        /// <summary>
        /// Processes an <c>INIT ACK</c>.
        /// </summary>
        /// <param name="initAckChunk">The <c>INIT ACK</c> chunk</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        private void ProcessInitAck(InitChunk initAckChunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Init Ack Received");

            if (initAckChunk.InitiateTag == 0)
            {
                this.Aborted();
                return;
            }

            if (this.tcb.CompareExchangeState(SCTPSocketState.CookieEchoed, SCTPSocketState.CookieWait) == SCTPSocketState.CookieWait)
            {
                // Stop the init timer.
                this.initTimer.Stop();

                this.tcb.PeerVerificationTag = initAckChunk.InitiateTag;
                this.tcb.SetInitialPeerTSN(initAckChunk.InitialTSN);
                this.tcb.PeerReceiveWindowSize = initAckChunk.AdvertisedReceiverWindowCredit;

                byte[] stateCookie = initAckChunk.GetParameters(ChunkParameterType.StateCookie).First().Value;

                SCTPPacket cookieEchoPacket = this.CreatePacket(
                    new CookieEchoChunk()
                    {
                        Cookie = stateCookie
                    });

                this.WriteConsole("Sending Cookie Echo");
                this.primaryEndpoint.SendPacket(cookieEchoPacket, remoteEndPoint);

                this.SetCurrentControlPacket(cookieEchoPacket);

                // Start the cookie retransmission timer.
                this.cookieTimer.Start();
            }
        }

        /// <summary>
        /// Processes a <c>COOKIE ECHO</c> chunk.
        /// </summary>
        /// <param name="cookieEcho">The <c>COOKIE ECHO</c> chunk.</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        private void ProcessCookieEcho(CookieEchoChunk cookieEcho, IPEndPoint remoteEndPoint)
        {
            if (this.tcb.State != SCTPSocketState.Closed)
            {
                this.WriteConsole("Cookie Echo Received in wrong state");

                this.Aborted();
                return;
            }

            this.WriteConsole("Cookie Echo Received");

            if (this.tcb.CompareExchangeState(SCTPSocketState.Established, SCTPSocketState.Closed) == SCTPSocketState.Closed)
            {
                SCTPPacket cookieAckPacket = this.CreatePacket(new CookieAckChunk());

                this.WriteConsole("Sending Cookie Ack");
                this.primaryEndpoint.SendPacket(cookieAckPacket, remoteEndPoint);

                // Set established...
                this.primaryEndpoint.Established(remoteEndPoint);
                this.WriteConsole("Connection Established");
            }
        }

        /// <summary>
        /// Processes a <c>COOKIE ACK</c> chunk.
        /// </summary>
        /// <param name="cookieAck">A <c>COOKIE ACK</c> chunk.</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        private void ProcessCookieAck(CookieAckChunk cookieAck, IPEndPoint remoteEndPoint)
        {
            if (this.tcb.CompareExchangeState(SCTPSocketState.Established, SCTPSocketState.CookieEchoed) != SCTPSocketState.CookieEchoed)
            {
                this.WriteConsole("Cookie Ack Received in wrong state");
                Aborted();
                return;
            }

            // stop the cookie timer.
            this.cookieTimer.Stop();

            this.WriteConsole("Cookie Ack Received", "Connection Established");

            this.primaryEndpoint.Established(remoteEndPoint);
        }

        /// <summary>
        /// Processes a <c>DATA</c> chunk.
        /// </summary>
        /// <param name="dataChunk">A <c>DATA</c> chunk.</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        /// <returns>True if the <c>DATA</c> chunk was added to the inbound queue; otherwise false.</returns>
        private bool ProcessDataChunk(DataChunk dataChunk, IPEndPoint remoteEndpoint, out bool sendAck)
        {
            sendAck = false;

            if (this.tcb.State != SCTPSocketState.Established)
            {
                this.WriteConsole("Data Received in wrong state");

                this.Aborted();
                return false;
            }

            lock (this.inboundQueue)
            {
                // Has the chunk been received already?
                if (this.inboundQueue.ContainsKey(dataChunk.TSN) == true)
                {
                    return false;
                }

                if (this.tcb.ReceiveWindowSize == 0)
                {
                    if (dataChunk.TSN > this.tcb.HighestReceivedTSN)
                    {
                        // When the receiver advertised window size is 0, the receiver MUST drop any new
                        // incomming DATA chunk with a TSN larger than the largest TSN received so far.
                        return false;
                    }

                    // If the new incoming DATA chunk holds a TSN value
                    // less than the largest TSN received so far, then the receiver SHOULD
                    // drop the largest TSN held for reordering and accept the new incoming
                    // DATA chunk

                    this.inboundQueue.RemoveAt(this.inboundQueue.Count - 1);
                    sendAck = true;
                }

                this.inboundQueue.Add(dataChunk.TSN, dataChunk);

                // Reduce the receiver window.
                this.tcb.ReceiveWindowSize -= dataChunk.Length;

                // Check and if required update the highest contiguous TSN value received.
                this.tcb.UpdateTSNReceived(dataChunk.TSN);

                // Increment the duplicate TSN counter.
                this.duplicateTSNs.AddOrUpdate(dataChunk.TSN, 1, (tsn, value) => { return value + 1; });

                // Increment the TCB ack state, and if it returns true trigger a SACK.
                if (this.tcb.IncrementAckState() == true)
                {
                    sendAck = true;
                }

                // Increment internal counters.
                this.tcb.IncrementBytesReceived(dataChunk.StreamId, dataChunk.Length);

                this.WriteConsole($"Data {dataChunk.StreamId}:{dataChunk.StreamSeqNo} Received [ACK: {sendAck}]");

                //SCTPMessage message = null;
                //if ((dataChunk.Flags & (byte)DataChunk.DataChunkFlags.Begining) != 0 &&
                //    (dataChunk.Flags & (byte)DataChunk.DataChunkFlags.Ending) != 0)
                //{
                //    // Message fits into a single chunk.
                //    message = new SCTPMessage(dataChunk.StreamId, dataChunk.StreamSeqNo, dataChunk.UserData);
                //}

                // Is pack complete?
                //int index = this.inboundQueue.IndexOfKey(dataChunk.TSN);

                //this.tcb.PeerReceiveWindowSize += dataChunk.Size;

                InboundMessageQueue queue = this.inboundStreamQueues.GetOrAdd(
                    dataChunk.StreamId,
                    (streamId) =>
                    {
                        return new InboundMessageQueue();
                    });

                queue.QueueDataChunk(dataChunk);
                queue.ProcessNextMessage(this, this.OnMessageReceived);

                return true;
            }
        }

        /// <summary>
        /// Processes a <c>SACK</c> chunk.
        /// </summary>
        /// <param name="ackChunk">A <c>SACK</c> chunk.</param>
        /// <param name="remoteEndPoint">The endpoint from which the chunk was received.</param>
        private void ProcessSelectiveAck(SelectiveAckChunk ackChunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole(ConsoleColor.Blue, "Processing SACK");

            // If the ack chunks cumulative TSN ack is less than the cumulative TSN ack point
            // then drop the SACK.
            if (ackChunk.CumulativeTSNAck < this.tcb.LastCumulativeTSNAcked)
            {
                this.WriteConsole("SACK dropped");
                return;
            }

            // Set rwnd equal to the newly received a_rwnd minus the number
            // of bytes still outstanding after processing the Cumulative
            // TSN Ack and the Gap Ack Blocks.

            // Set the Peers receive window size in the TCB
            // TODO: Minus the number of outstanding bytes
            this.tcb.PeerReceiveWindowSize = ackChunk.AdvertisedReceiverWindowCredit;

            lock (this.transmitLock)
            {
                var earliestOutstandingTSN = this.transmittedQueue.Values.FirstOrDefault(c => c.AckCount == 0)?.TSN;

WriteConsole(ConsoleColor.Red, $"Earliest Outstanding TSN:  {earliestOutstandingTSN}");
WriteConsole(ConsoleColor.Red, $"CumulativeTSNAck:          {ackChunk.CumulativeTSNAck}");
WriteConsole(ConsoleColor.Red, $"Last Cumulative TSN Acked: {this.tcb.LastCumulativeTSNAcked}");

                foreach (var chunk in this.transmittedQueue.Where(
                    kvp => kvp.Key <= ackChunk.CumulativeTSNAck &&
                    kvp.Key > this.tcb.LastCumulativeTSNAcked))
                {
                    WriteConsole(ConsoleColor.Green, $"Acking Chunk TSN: {chunk.Key}");
                    chunk.Value.AckCount++;
                    this.tcb.UpdateTSNAcked(chunk.Key);
                    if (chunk.Key == earliestOutstandingTSN)
                    {
                        this.WriteConsole(ConsoleColor.Blue, "Starting the retransmission timer");
                        this.retransmissionTimer.Start();
                    }
                }

                if (ackChunk.DuplicateTSNs != null)
                {
                    foreach (uint dtsn in ackChunk.DuplicateTSNs)
                    {
                        if (this.transmittedQueue.TryGetValue(dtsn, out DataChunk chunk) == true)
                        {
                            this.transmittedQueue.Remove(dtsn);

                            chunk.AckCount++;
                            this.tcb.UpdateTSNAcked(chunk.TSN);
                            this.WriteConsole(ConsoleColor.Blue, string.Format("Duplicate TSN: {0}", chunk.TSN));
                        }
                    }
                }
                else
                {
                    WriteConsole(ConsoleColor.Red, "No Duplicate TSN's");
                }

                // TODO: stop the retransmission timer when all traffic has been sent, and ack'd.
                if (this.transmitQueue.Any() == false &&
                    this.transmittedQueue.Any((kvp) => kvp.Value.AckCount == 0) == false)
                {
                    this.retransmissionTimer.Stop();
                }

                // If the SACK is missing a TSN that was previously acknowledged via a Gap Ack Block
                // (e.g. the data receiver reneged on the data), then consider the corresponding DATA
                // that might be possibly missing: Count one miss indication towards Fast Retransmit
                // as described in Section 7.2.4, and if no retransmit timer is running for the
                // destination address to which the DATA chunk was originally transmitted, then T3-rtx
                // is started for that desination address.
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessHeartbeat(HeartbeatChunk chunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Heartbeat Received");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessHeartbeatAck(HeartbeatChunk chunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Heartbeat ACK Received");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="shutdownChunk"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessShutdown(ShutdownChunk shutdown, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Shutdown Received");

            if (this.tcb.CompareExchangeState(SCTPSocketState.ShutdownAckSent, SCTPSocketState.Established) != SCTPSocketState.Established)
            {
                this.WriteConsole("Shutdown Received in wrong state");

                this.Aborted();
                return;
            }


            // deal with TSN's

            // Send any outstanding DATA chunks.


            this.WriteConsole("Sending Shutdown Ack");

            // Build shutdown packet.
            SCTPPacket shutdownPacket = this.CreatePacket(
                    new ShutdownChunk(true));

            this.primaryEndpoint.SendPacket(shutdownPacket);

            // Stash the shutdown packet for retransmission.
            this.SetCurrentControlPacket(shutdownPacket);

            // Start the shutdown timer.
            this.shutdownTimer.Start();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="shutdownAck"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessShutdownAck(uint packetVerificationTag, ShutdownChunk shutdownAck, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Shutdown Ack Received");

            // Stop the shutdown timer.
            this.shutdownTimer.Stop();

            uint verificationTag;
            ShutdownCompleteChunk chunk;

            if (this.tcb != null)
            {
                if (this.tcb.CompareExchangeState(SCTPSocketState.Closed, SCTPSocketState.ShutdownSent) != SCTPSocketState.ShutdownSent)
                {
                    this.WriteConsole("Shutdown Ack Received in wrong state");

                    this.Aborted();
                    return;
                }

                verificationTag = this.tcb.LocalVerificationTag;
                chunk = new ShutdownCompleteChunk()
                {
                    Flags = 0
                };
            }
            else
            {
                verificationTag = packetVerificationTag;
                chunk = new ShutdownCompleteChunk()
                {
                    Flags = 1
                };
            }

            this.WriteConsole("Sending Shutdown Complete");

            this.primaryEndpoint.SendPacket(
                this.CreatePacket(
                    verificationTag,
                    chunk));

            this.WriteConsole("Shutdown Complete");

            this.tcb.State = SCTPSocketState.Closed;
            this.tcb = null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="shutdownComplete"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessShutdownComplete(ShutdownCompleteChunk shutdownComplete, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Shutdown Complete Received");

            if (this.tcb.CompareExchangeState(SCTPSocketState.Closed, SCTPSocketState.ShutdownAckSent) != SCTPSocketState.ShutdownAckSent)
            {
                this.WriteConsole("Shutdown Complete Received in wrong state");

                this.Aborted();
                return;
            }

            this.shutdownTimer.Stop();

            this.WriteConsole("Shutdown Complete");

            this.tcb.State = SCTPSocketState.Closed;
            this.tcb = null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="abortChunk"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessAbort(AbortChunk abortChunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Abort Received");

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="errorChunk"></param>
        /// <param name="remoteEndPoint"></param>
        private void ProcessError(ErrorChunk errorChunk, IPEndPoint remoteEndPoint)
        {
            this.WriteConsole("Error Received");
        }

        /// <summary>
        /// Sends an ack packet.
        /// </summary>
        private void SendAcks()
        {
            var ackChunk = this.BuildAckChunk();
            this.primaryEndpoint.SendPacket(this.CreatePacket(ackChunk));

            this.WriteConsole(ConsoleColor.Cyan, "Sending ACK");
            this.WriteConsole(ConsoleColor.Cyan, $"Highest TSN:         {tcb.HighestReceivedTSN}");
            this.WriteConsole(ConsoleColor.Cyan, $"Last Contiguous TSN: {ackChunk.CumulativeTSNAck}");
            this.WriteConsole(ConsoleColor.Cyan, $"Duplicate TSNs:      {ackChunk.DuplicateTSNs.Count}");
            this.WriteConsole(ConsoleColor.Cyan, $"Gap Ack Blocks:      {ackChunk?.GapAckBlocks?.Count}");

            this.tcb.ResetAckState();
        }

        /// <summary>
        /// Builds a selective ack chunk.
        /// </summary>
        /// <returns>A selective ack chunk if acks are required; otherwise null.</returns>
        private SelectiveAckChunk BuildAckChunk()
        {
            lock (this.transmitLock)
            {
                List<uint> duplicateTSNList = new List<uint>(100);
                foreach (var duplicateTSN  in this.duplicateTSNs.Keys)
                {
                    int value = 0;
                    bool updated = false;

                    do
                    {
                        if (this.duplicateTSNs.TryGetValue(duplicateTSN, out value) == true)
                        {
                            updated = this.duplicateTSNs.TryUpdate(duplicateTSN, 1, value);
                        }
                    } while (updated == false);

                    for (int i = 1; i < value; i++)
                    {
                        duplicateTSNList.Add(duplicateTSN);
                    }
                }

                uint lastContiguousTSN = this.tcb.LastCumulativeTSNReceived;

                SelectiveAckChunk ackChunk = new SelectiveAckChunk()
                {
                    AdvertisedReceiverWindowCredit = (uint) this.tcb.ReceiveWindowSize,
                    CumulativeTSNAck = lastContiguousTSN,
                    DuplicateTSNs = duplicateTSNList
                };

                SelectiveAckChunk.GapAckBlock block = null;
                uint lastTSN = lastContiguousTSN;
                foreach (var tsn in this.ackedTSNs.SkipWhile((i) => { return i < lastTSN; }))
                {
                    if (block != null &&
                        tsn != lastTSN + 1)
                    {
                        if (ackChunk.CalculateLength() + 4 < this.primaryEndpoint.MTUSize)
                        {
                            block.GapAckBlockEnd = Convert.ToUInt16(lastTSN - lastContiguousTSN);
                            ackChunk.AddGapAckBlock(block);
                            block = null;
                        }
                        else
                        {
                            block = null;
                            break;
                        }
                    }

                    if (block == null)
                    {
                        block = new SelectiveAckChunk.GapAckBlock()
                        {
                            GapAckBlockStart = Convert.ToUInt16(tsn - lastContiguousTSN)
                        };
                    }

                    lastTSN = tsn;
                }

                if (block != null)
                {
                    block.GapAckBlockEnd = Convert.ToUInt16(lastTSN - lastContiguousTSN);
                    ackChunk.AddGapAckBlock(block);
                }

                return ackChunk;
            }
        }

        /// <summary>
        /// Sets the current control packet for retry purposes, and zeros the retry counter.
        /// </summary>
        /// <param name="packet">The current control packet</param>
        private void SetCurrentControlPacket(SCTPPacket packet)
        {
            this.currentStateControlPacket = packet;
            this.currentStateRetryCount = 0;
        }

        /// <summary>
        /// Increments the control packet retry counter.
        /// </summary>
        /// <returns>True if the counter has reached its limit; otherwise false.</returns>
        private bool IncrementControlRetryCounter()
        {
            this.currentStateRetryCount++;
            if (this.currentStateRetryCount > SCTPConstants.MaxInitRetransmits)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// The acknowledge timer has expired, so send acks
        /// </summary>
        /// <param name="state">The object state</param>
        private void AcknowledgeTimerExpired(object state)
        {
            this.SendAcks();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        private void InitTimerExpired(object state)
        {
            if (this.IncrementControlRetryCounter() == true)
            {
                Aborted();
                return;
            }

            if (this.currentStateControlPacket == null)
            {
                // TODO: unexpected - Abort!!!
                Aborted();
                return;
            }

            // Resend the init packet.
            this.primaryEndpoint.SendPacket(this.currentStateControlPacket);

            // Restart the init timer.
            this.initTimer.Restart();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        private void CookieTimerExpired(object state)
        {
            if (this.IncrementControlRetryCounter() == true)
            {
                Aborted();
                return;
            }

            if (this.currentStateControlPacket == null)
            {
                // TODO: unexpected - Abort!!!
                return;
            }

            // Resend the cookie echo packet.
            this.primaryEndpoint.SendPacket(this.currentStateControlPacket);

            // Restart the cookie echo timer.
            this.cookieTimer.Restart();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        private void ShutdownTimerExpired(object state)
        {

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        private void RetransmissionTimerExpired(object state)
        {
            this.WriteConsole(ConsoleColor.Yellow, "Retransmission timer expired");

            // TODO: adjust ssthresh
            this.primaryEndpoint.SlowStartThreshold = Math.Max(this.primaryEndpoint.CongestionWindowSize / 2, 4 * this.primaryEndpoint.MTUSize);

            // TODO: Set cwnd = MTU
            this.primaryEndpoint.CongestionWindowSize = this.primaryEndpoint.MTUSize;

            // TODO: Retransmit as many of the chunks with the lowest TSN's as possible.
            if (this.TransmitData() == true)
            {
                // Recalculate the RTO
                this.primaryEndpoint.RecalculateRTO(true);

                // Restart the retransmission timer.
                this.retransmissionTimer.Restart(this.primaryEndpoint.RetransmissionTimeout);
            }
            else
            {
                // Stop the retransmission timer
                this.retransmissionTimer.Stop();
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void GenerateKey()
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            this.secretKey = new byte[128];
            rng.GetBytes(this.secretKey);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tcb"></param>
        /// <returns></returns>
        private ChunkParameter CreateStateCookie(TCB tcb)
        {
            HMACMD5 hmac = new HMACMD5(this.secretKey);

            byte[] tcbData = tcb.ToArray();
            byte[] hash = hmac.ComputeHash(tcbData);

            byte[] value = tcbData.Append(hash);

            return new ChunkParameter()
            {
                Type = ChunkParameterType.StateCookie,
                Length = (ushort)(4 + value.Length),
                Value = value
            };
        }

        private void Aborted()
        {
            bool aborted = true;
            if (this.tcb != null &&
                this.tcb.CompareExchangeState(SCTPSocketState.Aborted, this.tcb.State) != SCTPSocketState.Aborted)
            {
                aborted = false;
            }

            if (aborted == true)
            {
                this.WriteConsole("Aborted");

                // Stop all timers.
                this.initTimer.Stop();
                this.cookieTimer.Stop();
                this.retransmissionTimer.Stop();
                this.shutdownTimer.Stop();
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void SignalMessageReceived(int streamId)
        {
        }

        /// <summary>
        /// Raises the "Message Received" event.
        /// </summary>
        /// <param name="e">The events arguments.</param>
        protected virtual void OnMessageReceived(SCTPMessageReceivedEventArgs e)
        {
            EventHandler<SCTPMessageReceivedEventArgs> handler = this.messageReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the "Error" event.
        /// </summary>
        /// <param name="e">The events arguments.</param>
        protected virtual void OnError(EventArgs e)
        {

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lines"></param>
        internal void WriteConsole(params string[] lines)
        {
            this.WriteConsole(ConsoleColor.White, lines);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="color"></param>
        /// <param name="lines"></param>
        internal void WriteConsole(ConsoleColor color, params string[] lines)
        {
            foreach (var line in lines)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[{0}] {1}", this.GetHashCode(), line);
            }
        }

        internal IEnumerable<DataChunk> GetUnackedChunks()
        {
            lock (this.transmitLock)
            {
                return this.transmittedQueue.Where(kvp => kvp.Value.AckCount == 0).Select(kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed == true)
            {
                return;
            }

            this.disposed = true;

            if (disposing == true)
            {
                this.Close();
            }
        }
    }
}
