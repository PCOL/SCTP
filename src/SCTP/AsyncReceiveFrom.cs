namespace SCTP
{
    using System;
    using System.Net.Sockets;

    /// <summary>
    /// Represents an asynchronous receive from.
    /// </summary>
    internal class AsyncReceiveFrom
        : AsyncResultBase<NetworkBuffer>
    {
        /// <summary>
        /// The network buffer.
        /// </summary>
        private NetworkBuffer buffer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public AsyncReceiveFrom(NetworkBuffer buffer, AsyncCallback callback = null, object state = null)
            : base(callback, state)
        {
            this.buffer = buffer;
        }

        /// <summary>
        /// Begins the async operation.
        /// </summary>
        /// <param name="flags">Socket flags.</param>
        public void BeginReceiveFrom(SocketFlags flags)
        {
            this.buffer.BeginReceiveFrom(flags, this.ReceiveFromCallback, this);
        }

        /// <summary>
        /// Ends the async operation.
        /// </summary>
        /// <returns>A network buffer.</returns>
        public NetworkBuffer EndReceiveFrom()
        {
            this.EndInvoke();
            return this.buffer;
        }

        /// <summary>
        /// Processes the receive from completion.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        private void ReceiveFromCallback(IAsyncResult asyncResult)
        {
            AsyncReceiveFrom asyncOp = asyncResult.AsyncState as AsyncReceiveFrom;

            try
            {
                asyncOp.buffer.EndReceiveFrom(asyncResult);

                asyncOp.SetComplete(asyncResult.CompletedSynchronously);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                asyncOp.SetComplete(asyncResult.CompletedSynchronously, ex);
            }
        }
    }
}
