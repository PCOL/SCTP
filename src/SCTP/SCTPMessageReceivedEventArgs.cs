namespace SCTP
{
    using System;

    public class SCTPMessageReceivedEventArgs
        : EventArgs
    {
        public SCTPMessage Message { get; private set; }

        public SCTPMessageReceivedEventArgs(SCTPMessage message)
        {
            this.Message = message;
        }
    }
}
