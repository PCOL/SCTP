namespace SCTP.Chunks
{
    using System;

    [Flags]
    public enum DataChunkFlags
        : byte
    {
        Ending = 1,

        Begining = 2,

        Unordered = 4
    }
}
