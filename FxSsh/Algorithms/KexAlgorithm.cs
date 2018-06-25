using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    [ContractClass(typeof(KexAlgorithmContract))]
    public abstract class KexAlgorithm {
        protected HashAlgorithm HashAlgorithm;

        public abstract byte[] CreateKeyExchange();

        public abstract byte[] DecryptKeyExchange(byte[] exchangeData);

        public byte[] ComputeHash(byte[] input) {
            Contract.Requires(input != null);

            return this.HashAlgorithm.ComputeHash(input);
        }
    }
}