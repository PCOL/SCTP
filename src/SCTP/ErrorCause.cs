namespace SCTP
{
    using System;

    /// <summary>
    /// Represents an error cause.
    /// </summary>
    public abstract class ErrorCause
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ErrorCause"/> class.
        /// </summary>
        /// <param name="code"></param>
        protected ErrorCause(CauseCode code)
        {
            this.Code = code;
        }

        /// <summary>
        /// Gets the error cause code.
        /// </summary>
        public CauseCode Code { get; private set; }

        /// <summary>
        /// Gets the length of the error cause.
        /// </summary>
        internal ushort Length { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] buffer = new byte[this.Length];
            this.ToArray(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int ToArray(byte[] buffer, int offset)
        {
            NetworkHelpers.CopyTo((ushort)this.Code, buffer, offset);
            int payloadLength = this.ToBuffer(buffer, offset + 4);
            this.Length = Convert.ToUInt16(payloadLength + 4);
            NetworkHelpers.CopyTo(this.Length, buffer, offset + 2);
            return this.Length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        protected abstract int ToBuffer(byte[] buffer, int offset);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal int FromArray(byte[] buffer, int offset)
        {
            int start = offset;

            this.Code = (CauseCode)NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            this.Length = NetworkHelpers.ToUInt16(buffer, offset);
            offset += 2;
            offset += this.FromBuffer(buffer, offset);
            return offset - start; ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        protected abstract int FromBuffer(byte[] buffer, int offset);
    }
}
