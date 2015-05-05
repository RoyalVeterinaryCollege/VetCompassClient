using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace VetCompass.Client
{
    /// <summary>
    ///     This class prepares the webrequest for making an HMAC call
    /// </summary>
    /// <remarks>See http://www.thebuzzmedia.com/designing-a-secure-rest-api-without-oauth-authentication
    /// </remarks>
    public class HMACRequestHasher
    {
        /// <summary>
        ///     Creates a signature of the request to be hashed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="clientId"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public string MakeSignatureForHashing(WebRequest request, Guid clientId, string requestBody)
        {
            const string sep = "\n";
            var requestDate = request.Headers.Get(Constants.VetCompass_Date_Header);
            return 
                request.Method          + sep + 
                clientId                + sep + 
                request.ContentLength   + sep + 
                request.ContentType     + sep +
                requestDate             + sep + 
                requestBody;
        }

        /// <summary>
        ///     Adds the relevant HMAC headers to a request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="clientId"></param>
        /// <param name="sharedSecret"></param>
        public void HashRequest(WebRequest request, Guid clientId, string sharedSecret, string requestBody)
        {
            var sharedSecretKeyAsByteArray = ConvertToByteArray(sharedSecret);

            var unhashedSignature = MakeSignatureForHashing(request, clientId, requestBody);
            var byteEncoded = Encoding.UTF8.GetBytes(unhashedSignature);
            using (var hasher = new HMACSHA256(sharedSecretKeyAsByteArray))
            {
                var signature = Convert.ToBase64String(hasher.ComputeHash(byteEncoded));
                request.Headers.Add(Constants.VetCompass_HMAC_Header, string.Format("{0}:{1}", clientId, signature));
            }
        }

        private byte[] ConvertToByteArray(string sharedSecret)
        {
            var bytes = Encoding.UTF8.GetBytes(sharedSecret);
            var base64EncodedSharedSecretKey = Convert.ToBase64String(bytes);
            var sharedSecretKeyAsByteArray = Convert.FromBase64String(base64EncodedSharedSecretKey);
            return sharedSecretKeyAsByteArray;
        }
    }
}