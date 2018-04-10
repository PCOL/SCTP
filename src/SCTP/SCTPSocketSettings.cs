namespace SCTP
{
    /// <summary>
    /// Represents settings for an SCTP socket.
    /// </summary>
    public class SCTPSocketSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not message transmissions are reliable.
        /// </summary>
        public bool Reliable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the messages are received in oreder.
        /// </summary>
        public bool Ordered { get; set; }
    }
}
