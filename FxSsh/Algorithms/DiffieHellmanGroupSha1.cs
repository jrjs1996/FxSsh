using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class DiffieHellmanGroupSha1 : KexAlgorithm {
        private readonly DiffieHellman exchangeAlgorithm;

        public DiffieHellmanGroupSha1(DiffieHellman algorithm) {
            Contract.Requires(algorithm != null);

            this.exchangeAlgorithm = algorithm;
            this.HashAlgorithm = new SHA1CryptoServiceProvider();
        }

        public override byte[] CreateKeyExchange() {
            return this.exchangeAlgorithm.CreateKeyExchange();
        }

        public override byte[] DecryptKeyExchange(byte[] exchangeData) {
            return this.exchangeAlgorithm.DecryptKeyExchange(exchangeData);
        }
    }
}