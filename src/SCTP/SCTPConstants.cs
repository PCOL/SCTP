namespace SCTP
{
    using System;

    /// <summary>
    /// Contains the various constant values for SCTP.
    /// </summary>
    internal static class SCTPConstants
    {
        /// <summary>
        /// The initial MTU size.
        /// </summary>
        public const int InitialMTUSize = 1280;

        /// <summary>
        /// The maximum burst value.
        /// </summary>
        public const int MaxBurst = 4;

        public const int AssociationMaxRetransmits = 10;

        public const int MaxInitRetransmits = 8;

        public const int DefaultReceiverWindowSize = 64 * 1024;

        public readonly static TimeSpan RTOInitial = TimeSpan.FromSeconds(3);

        public readonly static TimeSpan RTOMin = TimeSpan.FromSeconds(1);

        public readonly static TimeSpan RTOMax = TimeSpan.FromSeconds(60);

        internal readonly static TimeSpan ValidCookieLifetime = TimeSpan.FromSeconds(60);
    }
}