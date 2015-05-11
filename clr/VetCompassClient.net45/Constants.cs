using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VetCompass.Client
{
    public static class Constants
    {
        /// <summary>
        ///     Header key for the request date
        /// </summary>
        public const string VetCompass_Date_Header = "vetcompass-request-date";

        /// <summary>
        ///     Header key for HMAC authorisation
        /// </summary>
        public const string VetCompass_HMAC_Header = "vetcompass-hmac-authorisation";

        /// <summary>
        /// Header key for the client id
        /// </summary>
        public const string VetCompass_clientid_Header =  "vetcompass-clientid";
    }
}
