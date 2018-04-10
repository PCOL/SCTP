namespace SCTP.Chunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a Selective Ack chunk.
    /// </summary>
    /// <remarks>
    ///  0                   1                   2                   3  
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Type = 3      |Chunk Flags        | Chunk Length              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                   Cumulative TSN Ack                          |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |           Advertised Receiver Window Credit (a_rwnd)          |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Number of Gap Ack Blocks = N  | Number of Duplicate TSNs = X  |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |   Gap Ack Block #1 Start      |   Gap Ack Block #1 End        |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// /                                                               /
    /// \                               ...                             \
    /// /                                                               /
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |   Gap Ack Block #N Start      |   Gap Ack Block #N End        |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Duplicate TSN 1                         |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// /                                                               /
    /// \                               ...                             \
    /// /                                                               /
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Duplicate TSN X                         |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </remarks>
    class SelectiveAckChunk
        : Chunk
    {
        /// <summary>
        /// Represents a gap ack block.
        /// </summary>
        public class GapAckBlock
        {
            /// <summary>
            /// Gets or sets the start offset <c>TSN</c> for this gap ack block.
            /// To calculate the actual <c>TSN</c> number the cululative <c>TSN</c> ack is added to this offset.
            /// </summary>
            public ushort GapAckBlockStart { get; set; }

            /// <summary>
            /// Gets or sets the end offset <c>TSN</c> for this gap ack block.
            /// To calculate the actual <c>TSN</c> number the cululative <c>TSN</c> ack is added to this offset.
            /// </summary>
            public ushort GapAckBlockEnd { get; set; }
        }

        /// <summary>
        /// Gets or sets the <c>TSN</c> of the last <c>DATA</c> chunk received in sequence before a gap.
        /// </summary>
        /// <remarks>
        /// If no <c>DATA</c> chunks have been received this should this value should be set to the 
        /// peers initial <c>TSN</c> minus one.
        /// </remarks>
        public uint CumulativeTSNAck { get; set; }

        /// <summary>
        /// Gets or set the size of the receive buffer of the sender of the <c>SACK</c>.
        /// </summary>
        public uint AdvertisedReceiverWindowCredit { get; set; }

        /// <summary>
        /// Gets or set a list of <see cref="GapAckBlock"/> instances.
        /// </summary>
        public List<GapAckBlock> GapAckBlocks;

        /// <summary>
        /// Gets or sets the list of duplicate <c>TSN's</c>.
        /// </summary>
        public List<uint> DuplicateTSNs { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SelectiveAckChunk()
            : base (ChunkType.Sack)
        {
        }

        /// <summary>
        /// Adds a Gap Ack Block.
        /// </summary>
        /// <param name="block">The gap ack block.</param>
        public void AddGapAckBlock(GapAckBlock block)
        {
            if (this.GapAckBlocks == null)
            {
                this.GapAckBlocks = new List<GapAckBlock>();
            }
            this.GapAckBlocks.Add(block);
        }

        /// <summary>
        /// Iterates through the gap ack blocks in the chunk.
        /// </summary>
        /// <param name="action">An action to perform on each iteration.</param>
        public void ForEachGapAckBlock(Action<uint, uint> action)
        {
            foreach(var block in this.GapAckBlocks)
            {
                action(this.CumulativeTSNAck + block.GapAckBlockStart, this.CumulativeTSNAck + block.GapAckBlockEnd);
            }
        }

        /// <summary>
        /// Calculates the length and buffer size of the chunk.
        /// </summary>
        /// <param name="bufferSize">Outputs the size of the buffer (in bytes) required to contain the chunk.</param>
        /// <returns>The length of the chunk (in bytes).</returns>
        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            int len = 12;
            len += this.DuplicateTSNs != null ? this.DuplicateTSNs.Count * 4 : 0;
            len += this.GapAckBlocks != null ? this.GapAckBlocks.Count * 4 : 0;

            length += len;
            bufferSize += len;

            return length;
        }

        /// <summary>
        /// Writes the chunk into a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset at which to start writing the chunk.</param>
        /// <returns>The number of bytes written.</returns>
        protected override int ToBuffer(byte[] buffer, int offset, out int dataLength)
        {
            dataLength = 0;
            
            int start = offset;

            offset += NetworkHelpers.CopyTo(this.CumulativeTSNAck, buffer, offset);
            offset += NetworkHelpers.CopyTo(this.AdvertisedReceiverWindowCredit, buffer, offset);
            offset += NetworkHelpers.CopyTo((ushort)(this.GapAckBlocks == null ? 0 : this.GapAckBlocks.Count), buffer, offset);
            offset += NetworkHelpers.CopyTo((ushort)(this.DuplicateTSNs == null ? 0 : this.DuplicateTSNs.Count), buffer, offset);

            if (this.GapAckBlocks != null &&
                this.GapAckBlocks.Any())
            {
                foreach (var block in this.GapAckBlocks)
                {
                    offset += NetworkHelpers.CopyTo(block.GapAckBlockStart, buffer, offset);
                    offset += NetworkHelpers.CopyTo(block.GapAckBlockEnd, buffer, offset);
                }
            }

            if (this.DuplicateTSNs != null &&
                this.DuplicateTSNs.Any())
            {
                foreach (var dtsn in this.DuplicateTSNs)
                {
                    offset += NetworkHelpers.CopyTo(dtsn, buffer, offset);
                }
            }

            return offset - start;
        }

        /// <summary>
        /// Reads the chunk from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset at which to start reading the chunk.</param>
        /// <param name="length">The number of bytes that make up the chunk..</param>
        /// <returns>The number of bytes read.</returns>
        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            this.CumulativeTSNAck = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            this.AdvertisedReceiverWindowCredit = NetworkHelpers.ToUInt32(buffer, offset);
            offset += 4;
            int numberOfGapAckBlocks = (int)NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            int numberOfDuplicateTSNs = (int)NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;

            for (int i = 0; i < numberOfGapAckBlocks; i++)
            {
                if (this.GapAckBlocks == null)
                {
                    this.GapAckBlocks = new List<GapAckBlock>(numberOfGapAckBlocks);
                }

                GapAckBlock block = new GapAckBlock();
                block.GapAckBlockStart = NetworkHelpers.ToUInt16(buffer, offset);
                offset += 2;
                block.GapAckBlockEnd = NetworkHelpers.ToUInt16(buffer, offset);
                offset += 2;

                this.AddGapAckBlock(block);
            }

            for (int i = 0; i < numberOfDuplicateTSNs; i++)
            {
                if (this.DuplicateTSNs == null)
                {
                    this.DuplicateTSNs = new List<uint>(numberOfDuplicateTSNs);
                }

                this.DuplicateTSNs.Add(NetworkHelpers.ToUInt32(buffer, offset));
                offset += 4;
            }

            return offset - start;
        }
    }
}
