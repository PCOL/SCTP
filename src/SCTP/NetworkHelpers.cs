namespace SCTP
{
    using System;

    internal static class NetworkHelpers
    {
        /// <summary>
        /// Copies a <see cref="short"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(short value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes16();
            if (buffer != null)
            {
                Buffer.BlockCopy(buffer, offset, data, 0, data.Length);
            }

            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="short"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(ushort value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes16();
            if (buffer != null)
            {
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            }
            
            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="int"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(int value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes32();
            if (buffer != null)
            {
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            }

            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="uint"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(uint value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes32();
            if (buffer != null)
            {
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            }

            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="long"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(long value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes64();
            if (buffer != null)
            {
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            }

            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="short"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(ulong value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value).SwapBytes64();
            if (buffer != null)
            {
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
            }

            return data.Length;
        }

        /// <summary>
        /// Copies a <see cref="byte"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(byte value, byte[] buffer, int offset)
        {
            if (buffer != null)
            {
                buffer[offset] = (byte)(object)value;
            }
            return 1;
        }

        /// <summary>
        /// Copies a <see cref="byte"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(byte[] value, byte[] buffer, int offset, bool padding = true)
        {
            if (value != null)
            {
                int start = offset;

                int length = value.Length;
                
                if (buffer != null)
                {
                    Buffer.BlockCopy(value, 0, buffer, offset, length);
                }

                offset += length;
             
                int padLength = 4 - (length % 4);
                if (padding == true && padLength > 0 && padLength < 4)
                {
                    for(int i = 0; i < padLength; i++)
                    {
                        if (buffer != null)
                        {
                            buffer[offset] = 0;
                        }

                        offset++;
                    }
                }

                return offset - start;
            }

            return 0;
        }

        /// <summary>
        /// Copies a <see cref="byte"/> to a network buffer in network byte order.
        /// </summary>
        /// <param name="value">The value to copy.</param>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyTo(ArraySegment<byte> value, byte[] buffer, int offset, bool padding = true)
        {
            if (value != null)
            {
                int start = offset;

                int length = value.Count;

                if (buffer != null)
                {
                    Buffer.BlockCopy(value.Array, value.Offset, buffer, offset, length);
                }

                offset += length;

                int padLength = 4 - (length % 4);
                if (padding == true && padLength > 0 && padLength < 4)
                {
                    for (int i = 0; i < padLength; i++)
                    {
                        if (buffer != null)
                        {
                            buffer[offset] = 0;
                        }

                        offset++;
                    }
                }

                return offset - start;
            }

            return 0;
        }

        /// <summary>
        /// Gets a <see cref="short"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static byte ToByte(byte[] buffer, int offset)
        {
            return buffer[offset];
        }

        /// <summary>
        /// Gets a <see cref="short"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static byte[] ToBytes(byte[] buffer, int offset, int length)
        {
            return ToBytes(buffer, offset, length, out int paddedLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="paddedLength"></param>
        /// <returns></returns>
        public static byte[] ToBytes(byte[] buffer, int offset, int length, out int paddedLength)
        {
            paddedLength = length;
            paddedLength += Utility.CalculatePadding(paddedLength);

            byte[] data = new byte[length];
            Buffer.BlockCopy(buffer, offset, data, 0, length);
            return data;
        }

        /// <summary>
        /// Gets a <see cref="short"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static short ToInt16(byte[] buffer, int offset)
        {
            return BitConverter.ToInt16(buffer.SwapBytes16(offset), offset);
        }

        /// <summary>
        /// Gets a <see cref="ushort"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static ushort ToUInt16(byte[] buffer, int offset)
        {
            return BitConverter.ToUInt16(buffer.SwapBytes16(offset), offset);
        }

        /// <summary>
        /// Gets a <see cref="int"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int ToInt32(byte[] buffer, int offset)
        {
            return BitConverter.ToInt32(buffer.SwapBytes32(offset), offset);
        }

        /// <summary>
        /// Gets a <see cref="uint"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static uint ToUInt32(byte[] buffer, int offset)
        {
            return BitConverter.ToUInt32(buffer.SwapBytes32(offset), offset);
        }

        /// <summary>
        /// Gets a <see cref="long"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static long ToInt64(byte[] buffer, int offset)
        {
            return BitConverter.ToInt64(buffer.SwapBytes64(offset), offset);
        }

        /// <summary>
        /// Gets a <see cref="ulong"/> from the buffer
        /// </summary>
        /// <param name="buffer">The buffer to copy into.</param>
        /// <param name="offset">The offset in the buffer to copy into.</param>
        /// <returns>The number of bytes copied.</returns>
        public static ulong ToUInt64(byte[] buffer, int offset)
        {
            return BitConverter.ToUInt64(buffer.SwapBytes64(offset), offset);
        }
    }
}
