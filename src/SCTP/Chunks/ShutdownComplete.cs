using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTP.Chunks
{
    internal class ShutdownComplete
        : Chunk
    {
        public ShutdownComplete()
            : base(ChunkType.ShutdownComplete)
        {
            this.Flags = 0x00000001;
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
