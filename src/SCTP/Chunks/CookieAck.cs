using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTP.Chunks
{
    internal class CookieAck
        : Chunk
    {
        public CookieAck()
            : base(ChunkType.CookieAck)
        {
            this.Flags = 0;
        }

        protected override int ToBuffer(byte[] buffer, int offset, out int dataLength)
        {
            dataLength = 0;
            return 0;
        }


        protected override int FromBuffer(byte[] buffer, int offset, int length)
        {
            int start = offset;
            return offset - start;
        }
    }
}
