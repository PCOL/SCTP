namespace SCTP
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Represents a buffer for receiving data from the network.
    /// </summary>
    internal class NetworkBuffer
    {
        /// <summary>
        /// The socket.
        /// </summary>
        private Socket socket;

        /// <summary>
        /// The buffer.
        /// </summary>
        public readonly byte[] buffer;

        /// <summary>
        /// The offset of the segment in the buffer.
        /// </summary>
        private int offset;

        /// <summary>
        /// The length of the segment in the buffer.
        /// </summary>
        private int length;

        /// <summary>
        /// The end point the data was received from.
        /// </summary>
        private EndPoint endPoint;

        /// <summary>
        /// The error details should the operatio result in error.
        /// </summary>
        private SocketError error;

        /// <summary>
        /// Initialises a new instance of the <see cref="NetworkBuffer"/> class.
        /// </summary>
        /// <param name="socket">The socket</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        public NetworkBuffer(Socket socket, int bufferSize)
            : this(socket, new byte[bufferSize], 0, bufferSize)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="NetworkBuffer"/> class.
        /// </summary>
        /// <param name="socket">The socket</param>
        /// <param name="buffer">A byte array to be used as the buffer.</param>
        public NetworkBuffer(Socket socket, byte[] buffer)
            : this(socket, buffer, 0, buffer.Length)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="NetworkBuffer"/> class.
        /// </summary>
        /// <param name="socket">The socket</param>
        /// <param name="buffer">A byte array to be used as the buffer.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="length">The length of the buffer.</param>
        public NetworkBuffer(Socket socket, byte[] buffer, int offset, int length)
        {
            this.socket = socket;
            this.buffer = buffer;
            this.offset = offset;
            this.length = length;
            this.endPoint = new IPEndPoint(IPAddress.None, 0);
        }

        /// <summary>
        /// Receives data from a remote end point.
        /// </summary>
        /// <param name="flags">The receive flags.</param>
        /// <returns>The number of bytes received.</returns>
        public int ReceiveFrom(SocketFlags flags)
        {
            try
            {
                return this.BytesReceived = this.socket.ReceiveFrom(this.buffer, 0, this.buffer.Length, flags, ref this.endPoint);
            }
            catch (SocketException ex)
            {
                this.error = ex.SocketErrorCode;

                throw;
            }
        }

        /// <summary>
        /// Begins a receive operation from a remote end point.
        /// </summary>
        /// <param name="flags">The receive flags</param>
        /// <param name="callback">A method to 'callback' upon completion of the receive.</param>
        /// <param name="state">State to pass to the callback method.</param>
        /// <returns>An <see cref="IAsyncResult"/> which represents the asynchronous operation.</returns>
        public IAsyncResult BeginReceiveFrom(SocketFlags flags, AsyncCallback callback, object state)
        {
            return this.socket.BeginReceiveFrom(this.buffer, 0, this.buffer.Length, flags, ref this.endPoint, callback, state);
        }

        /// <summary>
        /// End an asynchronous receive operation.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <returns>The number of bytes received.</returns>
        public int EndReceiveFrom(IAsyncResult asyncResult)
        {
            try
            {
                return this.BytesReceived = this.socket.EndReceiveFrom(asyncResult, ref this.endPoint);
            }
            catch (SocketException ex)
            {
                this.error = ex.SocketErrorCode;

                throw;
            }
        }

        /// <summary>
        /// Sends data to an endpoint.
        /// </summary>
        /// <param name="flags">Any flags.</param>
        /// <param name="endPoint">The end point to send to.</param>
        /// <returns>The number of bytes sent.</returns>
        public int SendTo(SocketFlags flags, IPEndPoint endPoint)
        {
            try
            {
                if (endPoint != null)
                {
                    return this.socket.SendTo(this.buffer, this.offset, this.length, flags, endPoint);
                }

                return 0;
            }
            catch (SocketException ex)
            {
                this.error = ex.SocketErrorCode;

                throw;
            }
        }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        public byte[] Buffer => this.buffer;

        /// <summary>
        /// Gets the number of bytes received.
        /// </summary>
        public int BytesReceived { get; private set; }

        /// <summary>
        /// Gets the end point the packet was received from.
        /// </summary>
        public IPEndPoint ReceivedFrom
        {
            get { return (IPEndPoint)this.endPoint; }
        }
    }
}
