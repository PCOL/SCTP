using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTP
{
    internal static class Utility
    {
        public static void ThrowIfArgumentNull(string argmentName, object argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argmentName);
            }
        }

        public static void ThrowIfArgumentNullOrEmpty<T>(string argmentName, T[] argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argmentName);
            }

            if (argument.Length == 0)
            {
                throw new ArgumentException("Array is empty", argmentName);
            }
        }

        public static void ThrowIfNotUInt16Bounds(string paramName, int value)
        {
            if (value < ushort.MinValue || value > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetSize"></param>
        /// <param name="packetLength"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static int CalculateBufferSize(int packetSize, int packetLength, bool padding = true)
        {
            int length = packetSize;
            int dataLength = packetLength - length;
            length += dataLength;
            if (padding == true)
            {
                int padLength = CalculatePadding(length);
                length += padLength;
            }

            return length;
        }

        /// <summary>
        /// Calculates the padding required for a given data length.
        /// </summary>
        /// <param name="dataLength">The length of the data.</param>
        /// <returns>The number of padding bytes.</returns>
        public static int CalculatePadding(int dataLength)
        {
            int padLength = 4 - (dataLength % 4);
            if (padLength > 0 && padLength < 4)
            {
                return padLength;
            }

            return 0;
        }

        /// <summary>
        /// Joins two or more byte arrays tegether into a single byte array.
        /// </summary>
        /// <param name="buffer">The byte array to join to.</param>
        /// <param name="buffers">The byte arrays to join.</param>
        /// <returns>A new buffer containing all of the join buffers.</returns>
        public static byte[] Join(this byte[] buffer, params byte[][] buffers)
        {
            int length = buffer.Length;
            for (int i = 0; i < buffers.Length; i++)
            {
                length += buffers[i].Length;
            }

            int offset = 0;
            byte[] output = new byte[length];
            Buffer.BlockCopy(buffer, 0, output, offset, buffer.Length);
            offset += buffer.Length;

            foreach (var b in buffers)
            {
                Buffer.BlockCopy(b, 0, output, offset, b.Length);
                offset += b.Length;
            }

            return output;
        }

        /// <summary>
        /// Appends a byte array to another byte array.
        /// </summary>
        /// <param name="buffer">The byte array to append to.</param>
        /// <param name="appendBuffer">The byte array to append.</param>
        /// <returns></returns>
        public static byte[] Append(this byte[] buffer, byte[] appendBuffer)
        {
            if (buffer != null &&
                appendBuffer != null)
            {
                int offset = 0;
                byte[] output = new byte[buffer.Length + appendBuffer.Length];
                Buffer.BlockCopy(buffer, 0, output, offset, buffer.Length);
                offset += buffer.Length;
                Buffer.BlockCopy(appendBuffer, 0, output, offset, appendBuffer.Length);

                return output;
            }

            return buffer;
        }

        /// <summary>
        /// Swaps the 2 bytes around at a given offset in a byte array.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <param name="offset">The offset of the first byte</param>
        /// <returns>The byte array.</returns>
        public static byte[] SwapBytes16(this byte[] data, int offset = 0)
        {
            if (data != null &&
                data.Length >= offset + 2)
            {
                byte tmp = data[offset];
                data[offset] = data[offset + 1];
                data[offset + 1] = tmp;
            }

            return data;
        }

        /// <summary>
        /// Swaps 4 bytes around at a given offset in a byte array.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <param name="offset">The offset of the first byte.</param>
        /// <returns>The byte array.</returns>
        public static byte[] SwapBytes32(this byte[] data, int offset = 0)
        {
            if (data != null &&
                data.Length >= offset + 4)
            {
                data.SwapBytes16(offset);
                data.SwapBytes16(offset + 2);

                byte tmp0 = data[offset];
                byte tmp1 = data[offset + 1];

                data[offset] = data[offset + 2];
                data[offset + 1] = data[offset + 3];

                data[offset + 2] = tmp0;
                data[offset + 3] = tmp1;
            }

            return data;
        }

        /// <summary>
        /// Swaps 8 bytes around at a given offset in a byte array.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <param name="offset">The offset of the first byte.s</param>
        /// <returns>The byte array.</returns>
        public static byte[] SwapBytes64(this byte[] data, int offset = 0)
        {
            if (data != null &&
                data.Length >= offset + 8)
            {
                data.SwapBytes32(offset);
                data.SwapBytes32(offset + 4);

                byte[] tmp = new byte[4];
                Buffer.BlockCopy(data, offset, tmp, 0, 4);
                Buffer.BlockCopy(data, offset, data, offset + 4, 4);
                Buffer.BlockCopy(tmp, 0, data, offset + 4, 4);
            }

            return data;
        }
    }
}
