namespace SCTP.Chunks
{
    internal enum ChunkType
        : byte
    {
        /// <summary>
        /// Payload data
        /// </summary>
        Data = 0,

        /// <summary>
        /// Initiation
        /// </summary>
        Init = 1,

        /// <summary>
        /// Initiation acknowledgement.
        /// </summary>
        InitAck = 2,

        /// <summary>
        /// Selective acknowledgement.
        /// </summary>
        Sack = 3,

        /// <summary>
        /// Heartbeat request.
        /// </summary>
        Heartbeat = 4,

        /// <summary>
        /// Heartbeat acnowledgement.
        /// </summary>
        HeartbeatAck = 5,

        /// <summary>
        /// Abort.
        /// </summary>
        Abort = 6,

        /// <summary>
        /// Shutdown.
        /// </summary>
        Shutdown = 7,

        /// <summary>
        /// Shutdown acknowledgement.
        /// </summary>
        ShutdownAck = 8,

        /// <summary>
        /// Operation error.
        /// </summary>
        Error = 9,

        /// <summary>
        /// State cookie.
        /// </summary>
        CookieEcho = 10,

        /// <summary>
        /// Cookie acknowledgement.
        /// </summary>
        CookieAck = 11,

        /// <summary>
        /// Reserved for Explicit Congestion Notification Echo (ECNE)
        /// </summary>
        ReservedEcne = 12,

        /// <summary>
        /// Reserved for Congestion Window Reduced (CWR)
        /// </summary>
        ReservesCwr = 13,

        /// <summary>
        /// Shutdown complete.
        /// </summary>
        ShutdownComplete = 14,
    }
}
