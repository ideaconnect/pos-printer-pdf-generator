using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace POSPrinterPdfGenerator
{
    internal class RsaSignatureVerifier : IDisposable
    {
        private readonly RSA _rsa;
        private readonly string EncodedPublicKey = "MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEApzA5y+zCdS+MeNd/k4ra\r\nhKF8XRBlqcG5oYejcbBk746CWEsCTBcSq/JmiFSvuyeKjtpVgagdB4JIVo16Sw3r\r\n0mXM+yKFzq+u/subBRajG9LMEhhup+mlC+q2DN/vSg78gyxElv+6gZuq7p+yCJ7m\r\nQGwUa0pOtKtqE0KEir2H1ssjJSkPwjmT9yRDwWj6zadzmcgeoyP0Xp9GNHOtT6N1\r\n/5ROAfSpLMrZiCkLzsT12FnizCi+9UA7TOmfaX9Sn6fZB2MDUlONELMkib0+Qy3h\r\nCa2zkeLL6rdKEeShxhgtVYLIl7gcD+IydVGR9w+WJKlDGXA4MHRnPOFH4nm2pIAb\r\n+nFjxhJ5PiuBU7Mem8Ae8eeGkkLswzRTo8BRxv0PRFcz7OUVGzgV/AupcAAwxMU1\r\nfk/oXSAxn+vxTVyXCejAOrN45P7LOU9pjRJWSdYgnLnXdeegnF3p4Rc/m1RwBAM1\r\niGTFDXAUP777uIQqJx6JqLmb/rED0xaT+itxTSouhovvjZjf2MSyndB6IB62R3I6\r\nN2lKxs+oAOZxpf8doDExGMBk479L2FsMUOUZaSVIuBgKdt9XFkPPL4EoYotKVGUk\r\nywp4OL/U7CrWGRw22avpQ7xq6VlvvC+JIraHbXfNFtkLDGqDeArWxZiUhT0jQBpo\r\nXHCyAF2vlomQ2uTjIRU/5iECAwEAAQ==";

        /// <summary>
        /// Create a new instance of <see cref="RsaSignatureVerifier"/>.
        /// </summary>
        /// <param name="publicKeyPath">
        /// The path to the public key corresponding to the private key that was used to sign files.
        /// </param>
        public RsaSignatureVerifier()
        {
            _rsa = RSA.Create();

            byte[] pubKey = Convert.FromBase64String(EncodedPublicKey); 

            _rsa.ImportSubjectPublicKeyInfo(pubKey, out _);
        }

        /// <summary>
        /// Verifies the specified file using the specified <see cref="RSA"/> signature. The digest
        /// used is <see cref="HashAlgorithmName.SHA256"/>.
        /// </summary>
        /// <param name="fileToVerifyPath">The path of the file to verify.</param>
        /// <param name="fileSignaturePath">
        /// The path of the signature used to verify the specified file.
        /// </param>
        /// <returns>Whether the file was verified successfully.</returns>
        public bool Verify(string fileToVerifyPath, string fileSignaturePath)
        {
            var fileToVerifyStream = new FileStream(fileToVerifyPath, FileMode.Open);
            byte[] signatureBytes = File.ReadAllBytes(fileSignaturePath);
            return _rsa.VerifyData(fileToVerifyStream, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public void Dispose()
        {
            _rsa.Dispose();
        }
    }
}
