namespace SCTP
{
    using System.Collections.Concurrent;

    internal class StreamSequenceGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        private class Generator
        {
            private ushort sequenceNo = 0;

            /// <summary>
            /// Gets the next sequence number.
            /// </summary>
            /// <returns>The next number in the sequence.</returns>
            public ushort GetNextSequence()
            {
                return unchecked(this.sequenceNo++);
            }
        }

        private ConcurrentDictionary<int, Generator> sequenceGenerators;

        public StreamSequenceGenerator()
        {
            this.sequenceGenerators = new ConcurrentDictionary<int, Generator>();
        }

        public ushort NextSequence(int streamId)
        {
            Generator generator = this.sequenceGenerators.GetOrAdd(streamId, (sid) => { return new Generator(); });
            return generator.GetNextSequence();
        }
    }
}
