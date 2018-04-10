namespace SCTP
{
    using System;
    using System.IO;

    /// <summary>
    /// 
    /// </summary>
    public class ChunkStream
        : Stream
    {
        /// <summary>
        /// An array of buffers which make up the contents of the stream.
        /// </summary>
        private ArraySegment<byte>[] buffers;

        /// <summary>
        /// The current stream position.
        /// </summary>
        private long position;

        /// <summary>
        /// The current buffer index in the array.
        /// </summary>
        private int currentBufferIndex;

        /// <summary>
        /// The current buffer.
        /// </summary>
        private ArraySegment<byte> currentBuffer;

        /// <summary>
        /// The current offset within the current buffer.
        /// </summary>
        private int currentOffset;

        /// <summary>
        /// The length of the stream.
        /// </summary>
        private long length;

        /// <summary>
        /// Initilises a new instance of the <see cref="ChunkStream"/> class.
        /// </summary>
        /// <param name="buffer">The buffer to break up.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="copyIntoNewBuffers">A value indicating whether or not the buffer should be copied into new buffers.</param>
        public ChunkStream(byte[] buffer, int chunkSize, bool copyIntoNewBuffers = true)
            : this(new ArraySegment<byte>(buffer), chunkSize, copyIntoNewBuffers)
        {
        }

        /// <summary>
        /// Initilises a new instance of the <see cref="ChunkStream"/> class.
        /// </summary>
        /// <param name="buffer">The buffer to break up.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="copyIntoNewBuffers">A value indicating whether or not the buffer should be copied into new buffers.</param>
        public ChunkStream(ArraySegment<byte> buffer, int chunkSize, bool copyIntoNewBuffers = true)
        {
            int count = buffer.Count;
            int offset = buffer.Offset;

            int index = 0;
            this.buffers = new ArraySegment<byte>[(int)Math.Round((double)buffer.Count / (double)chunkSize, 0)];
            while (count > 0)
            {
                int size = Math.Min(chunkSize, count);
                if (copyIntoNewBuffers == true)
                {
                    this.buffers[index] = new ArraySegment<byte>(new byte[size]);
                    Buffer.BlockCopy(buffer.Array, offset, this.buffers[index].Array, 0, size);
                }
                else
                {
                    this.buffers[index] = new ArraySegment<byte>(buffer.Array, offset, size);
                }

                index++;
                count -= size;
                offset += size;
            }

            this.SetCurrentBuffer(0);
            this.length = this.CalculateLength();
        }

        /// <summary>
        /// Initilises a new instance of the <see cref="ChunkStream"/> class.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public ChunkStream(ArraySegment<byte>[] buffers)
        {
            this.buffers = buffers;
            this.SetCurrentBuffer(0);
            this.length = this.CalculateLength();
        }

        /// <summary>
        /// Initilises a new instance of the <see cref="ChunkStream"/> class.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public ChunkStream(byte[][] buffers)
        {
            this.buffers = new ArraySegment<byte>[buffers.Length];
            for (int i = 0; i < buffers.Length; i++)
            {
                this.buffers[i] = new ArraySegment<byte>(buffers[i]);
            }

            this.SetCurrentBuffer(0);
            this.length = this.CalculateLength();
        }

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        public override long Length => this.length;

        /// <summary>
        /// Gets or sets the current position in the stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.position = value;
                this.currentBuffer = this.GetBuffer(this.position, out this.currentOffset, out this.currentBufferIndex);
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the stream can be read from.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether or not the stream can be written to.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets a value indicating whether or not the stream can be seeked.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Reads a number of bytes into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read the byte into.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = this.ReadFromBuffer(buffer, offset, count);
            return read;
        }

        /// <summary>
        /// Reads the nex 
        /// </summary>
        /// <returns>The byte if one is available; otherwise -1</returns>
        public override int ReadByte()
        {
            if (this.position >= this.length)
            {
                return -1;
            }

            byte b = this.currentBuffer.Array[this.currentOffset++];
            if (this.currentOffset >= this.currentBuffer.Offset + this.currentBuffer.Count)
            {
                this.currentBufferIndex++;
                if (this.currentBufferIndex < this.buffers.Length)
                {
                    this.currentBuffer = this.buffers[this.currentBufferIndex];
                    this.currentOffset = this.currentBuffer.Offset;
                }
                else
                {
                    this.currentOffset = -1;
                }
            }

            return (int)b;
        }

        /// <summary>
        /// Write an array of bytes to the stream.
        /// </summary>
        /// <param name="buffer">The array</param>
        /// <param name="offset">The offset to start from.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flushes the buffer.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Seeks to a specific position within the stream.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="origin">The origin.</param>
        /// <returns>The new position.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                this.Position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                this.Position += offset;
            }
            else if (origin == SeekOrigin.End)
            {
                this.Position = this.Length - offset;
            }

            return this.Position;
        }

        /// <summary>
        /// Sets the length of the stream.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the current buffer.
        /// </summary>
        /// <param name="bufferNo"></param>
        private void SetCurrentBuffer(int bufferNo)
        {
            this.currentBufferIndex = bufferNo;
            this.currentBuffer = this.buffers[this.currentBufferIndex];
            this.currentOffset = this.currentBuffer.Offset;
        }

        /// <summary>
        /// Calculates the length of the stream.
        /// </summary>
        /// <returns>The size of the stream in bytes.</returns>
        private long CalculateLength()
        {
            long length = 0;
            for (int i = 0; i < this.buffers.Length; i++)
            {
                length += this.buffers[i].Count;
            }

            return length;
        }

        /// <summary>
        /// Reads a number of bytes from the streams curent position.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            if (this.position >= this.length)
            {
                return 0;
            }

            int length = this.currentBuffer.Count - this.currentOffset;
            int left = count;
            int read = 0;
            while (left > 0)
            {
                Buffer.BlockCopy(this.currentBuffer.Array, this.currentOffset, buffer, offset, length);

                read += length;
                offset += length;
                left -= length;

                if (left > 0)
                {
                    this.currentBufferIndex++;
                    if (this.currentBufferIndex >= this.buffers.Length)
                    {
                        throw new ArgumentOutOfRangeException("count");
                    }

                    this.SetCurrentBuffer(this.currentBufferIndex);
                    length = Math.Min(this.currentBuffer.Count, left);
                }
            }

            this.position += read;
            return read;
        }

        /// <summary>
        /// Gets the buffer which contains the index, and its offset into that buffer.
        /// </summary>
        /// <param name="index">The index in the stream.</param>
        /// <param name="offset">Outputs the offset into the stream for the index.</param>
        /// <param name="num">The number of the buffer which contains the index.</param>
        /// <returns>The buffer which contains the index.</returns>
        private ArraySegment<byte> GetBuffer(long index, out int bufferOffset, out int num)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            long offset = index;
            num = 0;

            int length = this.buffers[num].Count;
            while (offset > length)
            {
                num++;
                if (num >= this.buffers.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                offset -= length;
                length = this.buffers[num].Count;
            }

            bufferOffset = Convert.ToInt32(offset);
            return this.buffers[num];
        }
    }
}
