using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTP
{
    public enum CauseCode
        : ushort
    {
        InvalidStreamIdentifier = 1,
        MissingMandatoryParameter= 2,
        StaleCookieError = 3,
        OutOfResource = 4,
        UnresolvableAddress = 5,
        UnrecognizedChunkType = 6,
        InvalidMandatoryParameter = 7,
        UnrecognizedParameters = 8,
        NoUserData = 9,
        CookieReceivedWhileShuttingDown = 10,
        RestartOfAnAssociationWithNewAddresses = 11,
        UserInitiatedAbort = 12,
        ProtocolViolation = 13,
    }
}
