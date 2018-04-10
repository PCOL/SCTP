using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTP.Chunks
{
    internal class CookieEcho
        : Chunk
    {

        public byte[] Cookie { get; set; }

        public CookieEcho()
            : base(ChunkType.CookieEcho)
        {
        }

        protected internal override int CalculateLength(out int bufferSize)
        {
            int length = base.CalculateLength(out bufferSize);

            if (this.Cookie != null)
            {
                length += this.Cookie.Length;
                bufferSize += Utility.CalculateBufferSize(0, this.Cookie.Length);
            }

            return length;
        }

        protected override int ToBuffer(byte[] buffer, int offset, out int dataLength)
        {
            dataLength = 0;
            offset += NetworkHelpers.CopyTo(this.Cookie, buffer, offset, false);
            return this.Cookie.Length;
        }


        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            this.Cookie = NetworkHelpers.ToBytes(buffer, offset, length - 4);
            offset += this.Cookie.Length;
            return offset - start;
        }
    }
}
