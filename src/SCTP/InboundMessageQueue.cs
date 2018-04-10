namespace SCTP
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SCTP.Chunks;

    /// <summary>
    /// Represents a queue of inbound messages
    /// </summary>
    internal class InboundMessageQueue
    {
        /// <summary>
        ///
        /// </summary>
        private int signalMessageReceivedLock;

        /// <summary>
        /// 
        /// </summary>
        private int lastMessageSeqNo = -1;

        /// <summary>
        /// The message queue.
        /// </summary>
        private SortedList<int, SCTPMessage> queue = new SortedList<int, SCTPMessage>(100);

        /// <summary>
        /// Queues a new <c>DATA</c> chunk.
        /// </summary>
        /// <param name="message">The message.</param>
        public void QueueDataChunk(DataChunk dataChunk)
        {
            lock (this.queue)
            {
                if (this.queue.TryGetValue(dataChunk.StreamSeqNo, out SCTPMessage message) == false)
                {
                    message = new SCTPMessage(dataChunk.StreamId, dataChunk.StreamSeqNo);
                    this.queue.Add(dataChunk.StreamSeqNo, message);
                }

                message.AddChunk(dataChunk);
            }
        }

        /// <summary>
        /// Dequeues the next message from the queue.
        /// </summary>
        /// <returns>A message; otherwise null.</returns>
        private SCTPMessage DequeueNextMessage(SCTPSocket socket)
        {
            lock (this.queue)
            {
                SCTPMessage message = null;

                if (this.queue.Count > 0)
                {
                    KeyValuePair<int, SCTPMessage> item = this.queue.First();

                    if (item.Value.StreamSequenceNo == this.lastMessageSeqNo + 1 &&
                        item.Value.IsMessageComplete() == true)
                    {
                        this.lastMessageSeqNo = item.Value.StreamSequenceNo;
                        message = item.Value;
                        this.queue.RemoveAt(0);
                    }
                }

                return message;
            }
        }

        /// <summary>
        /// Process the next message in the queue.
        /// </summary>
        /// <param name="action"></param>
        public void ProcessNextMessage(SCTPSocket socket, Action<SCTPMessageReceivedEventArgs> action)
        {
            if (Interlocked.CompareExchange(ref this.signalMessageReceivedLock, 1, 0) == 0)
            {
                Task.Run(
                () =>
                {
                    bool more = false;

                    try
                    {
                        SCTPMessage message = this.DequeueNextMessage(socket);
                        if (message != null)
                        {
                            socket.PeerReceiveWindowSize += (uint)message.Length;
                            more = true;

                            action(new SCTPMessageReceivedEventArgs(message));
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref this.signalMessageReceivedLock, 0);
                    }

                    if (more == true)
                    {
                        this.ProcessNextMessage(socket, action);
                    }
                });
            }
        }
    }
}
