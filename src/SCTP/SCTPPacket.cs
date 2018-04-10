namespace SCTP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using SCTP.Chunks;

    /// <summary>
    /// Represent an <c>SCTP</c> packet.
    /// </summary>
    /// <remarks>
    /// The SCTP packet format is shown below:
    ///
    ///  0                   1                   2                   3   
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Common Header                           |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                         Chunk #1                              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           ...                                 |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                         Chunk #n                              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///
    /// SCTP Common Header Format
    ///
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |       Source Port Number      |   Destination Port Number     |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                       Verification Tag                        |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// | Checksum |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///
    /// </remarks>
    internal class SCTPPacket
    {
        private int size;

        /// <summary>
        /// Initialises a new instance of the <see cref="SCTPPacket"/> class.
        /// </summary>
        public SCTPPacket()
        {
            this.Header = new CommonHeader();
            this.Chunks = new List<Chunk>(20);
        }

        /// <summary>
        /// Gets the packet header.
        /// </summary>
        public CommonHeader Header { get; private set; }

        /// <summary>
        /// Gets a list of chunks in the packet.
        /// </summary>
        public List<Chunk> Chunks { get; private set; }

        /// <summary>
        /// Gets the end point the packet was received from.
        /// </summary>
        public IPEndPoint ReceivedFrom { get; internal set; }

        /// <summary>
        /// Gets the size of the packet in bytes.
        /// </summary>
        public int Size => this.size;

        /// <summary>
        /// Adds a chunk to the packet.
        /// </summary>
        /// <param name="chunk">The chunk to add.</param>
        public void AddChunk(Chunk chunk)
        {
            this.Chunks.Add(chunk);
            this.CalculateSize();
        }

        /// <summary>
        /// Adds a list of chunks to the packet.
        /// </summary>
        /// <param name="chunks">The list of chunks.</param>
        public void AddChunks(IEnumerable<Chunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                this.Chunks.Add(chunk);
            }

            this.CalculateSize();
        }

        /// <summary>
        /// Gets a chunk from the packet.
        /// </summary>
        /// <typeparam name="T">The type of chunk.</typeparam>
        /// <param name="index">The chunk index.</param>
        /// <returns>The chunk.</returns>
        public T GetChunk<T>(int index)
            where T : Chunk
        {
            return this.Chunks[index] as T;
        }

        /// <summary>
        /// Calculates the size of the packet.
        /// </summary>
        private void CalculateSize()
        {
            this.size = 12;
            foreach (var chunk in this.Chunks)
            {
                chunk.CalculateLength(out int bufferSize);
                this.size += bufferSize;
            }
        }

        /// <summary>
        /// Updates the packets CRC.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        private void UpdateCRC(byte[] buffer, int offset, int count)
        {
            uint crc = CRC32c.GetCRC(buffer, offset, count);
            Buffer.BlockCopy(BitConverter.GetBytes(crc), 0, buffer, offset + 8, 4);
        }

        /// <summary>
        /// Checks the CRC of the incomming packet
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static bool CheckCRC(byte[] buffer, int offset, int count)
        {
            uint packetCrc = BitConverter.ToUInt32(buffer, offset + 8);
            for (int i = offset + 8; i < offset + 12; i++)
            {
                buffer[i] = 0;
            }

            uint crc = CRC32c.GetCRC(buffer, offset, count);
            return packetCrc == crc;
        }

        /// <summary>
        /// Converts the packet into a byte array.
        /// </summary>
        /// <returns>An array of bytes.</returns>
        public byte[] ToArray()
        {
            byte[] buffer = new byte[this.Size];
            this.ToArray(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Writes the packet to an existing byte array at given position.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The offset to begin writing the packet.</param>
        /// <returns>The number of bytes written.</returns>
        public int ToArray(byte[] buffer, int offset)
        {
            int start = offset;
            offset += this.Header.ToArray(buffer, offset);
            foreach (var chunk in this.Chunks)
            {
                offset += chunk.ToArray(buffer, offset);
            }

            int count = offset - start;
            this.UpdateCRC(buffer, start, count /*this.Size*/);
            return count;
        }

        /// <summary>
        /// Reads a packet from a given positin in a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The position to start reading from.</param>
        /// <param name="length">The number of bytes to read for.</param>
        /// <returns>The <c>SCTP</c> packet.</returns>
        public static SCTPPacket FromArray(byte[] buffer, int offset, int length, bool throwOnCRCFailure = false)
        {
            if (offset + 12 < length)
            {
                if (CheckCRC(buffer, offset, length) == false)
                {
                    if (throwOnCRCFailure == true)
                    {
                        throw new Exception("CRC Check failed");
                    }

                    return null;
                }

                SCTPPacket packet = new SCTPPacket();
                int start = offset;

                offset += packet.Header.FromArray(buffer, offset);

                while (offset + 4 <= length)
                {
                    Chunk chunk = Chunk.Create(buffer, offset, out int chunkLength);
                    if (chunk == null)
                    {
                        break;
                    }

                    packet.Chunks.Add(chunk);
                    offset += chunkLength;
                }

                packet.CalculateSize();

                return packet;
            }

            return null;
        }

        /// <summary>
        /// Creates an SCTP packet.
        /// </summary>
        /// <param name="sourcePort">The source port.</param>
        /// <param name="destinationPort">The destination port.</param>
        /// <param name="verificationTag">The verification tag.</param>
        /// <param name="chunks">The packets chunks.</param>
        /// <returns>An <see cref="SCTPPacket"/> instance.</returns>
        public static SCTPPacket Create(ushort sourcePort, ushort destinationPort, uint verificationTag, params Chunk[] chunks)
        {
            SCTPPacket packet = new SCTPPacket();
            packet.Header.SourcePort = sourcePort;
            packet.Header.DestinationPort = destinationPort;
            packet.Header.Checksum = 0;
            packet.Header.VerificationTag = verificationTag;

            if (chunks != null &&
                chunks.Any())
            {
                packet.AddChunks(chunks);
                packet.CalculateSize();
            }

            return packet;
        }
    }
}
