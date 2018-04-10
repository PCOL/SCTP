namespace SCTP.Chunks
{
    /// <summary>
    /// Supported chunk parameter types.
    /// </summary>
    public enum ChunkParameterType
        : ushort
    {
        /// <summary>
        /// The parameter type is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The value of the parameter is heartbeat information.
        /// </summary>
        HeartbeatInfo = 1,

        /// <summary>
        /// The value of the parameter is an IP v4 address.
        /// </summary>
        IPv4Address = 5,

        /// <summary>
        /// The value of the parameter is an IP v6 address.
        /// </summary>
        IPv6Address = 6,

        /// <summary>
        /// The value of the parameter is a state cookie.
        /// </summary>
        StateCookie = 7,

        /// <summary>
        /// The value of the parameter is unrecognised.
        /// </summary>
        UnrecognizedParameters = 8,

        /// <summary>
        /// The value of the parameter is a cookie preservative.
        /// </summary>
        CookiePreservative = 9,

        /// <summary>
        /// The value of the parameter is a host name/address
        /// </summary>
        HostNameAddress = 11,

        /// <summary>
        /// The value of the parameter is supported address types.
        /// </summary>
        SupportedAddressTypes = 12,

        /// <summary>
        /// The value of the parameter is 
        /// </summary>
        OutgoingSSNResetRequestParameter = 13,

        /// <summary>
        /// The value of the parameter is 
        /// </summary>
        IncomingSSNResetRequestParameter = 14,

        /// <summary>
        /// The value of the parameter is 
        /// </summary>
        SSNTSNResetRequestParameter = 15,

        /// <summary>
        /// The value of the parameter is reconfiguration response.
        /// </summary>
        ReconfigurationResponseParameter = 16,

        /// <summary>
        /// The value of the parameter is an add outgoing stream request.
        /// </summary>
        AddOutgoingStreamsRequestParameter = 17,

        /// <summary>
        /// The value of the parameter is an add incomming stream request.
        /// </summary>
        AddIncomingStreamsRequestParameter = 18,
    }
}
