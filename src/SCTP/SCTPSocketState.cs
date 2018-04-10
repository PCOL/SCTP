namespace SCTP
{
    /// <summary>
    /// 
    /// </summary>
    internal enum SCTPSocketState
    {
        Closed = 0,
        CookieWait,
        CookieEchoed,
        Established,
        ShutdownPending,
        ShutdownSent,
        ShutdownReceived,
        ShutdownAckSent,
        Aborted
    }
}
