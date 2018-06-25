using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class HmacAlgorithm {
        private readonly KeyedHashAlgorithm algorithm;

        public HmacAlgorithm(KeyedHashAlgorithm algorithm, int keySize, byte[] key) {
            Contract.Requires(algorithm != null);
            Contract.Requires(key != null);
            Contract.Requires(keySize == key.Length << 3);

            this.algorithm = algorithm;
            algorithm.Key = key;
        }

        public int DigestLength => this.algorithm.HashSize >> 3;

        public byte[] ComputeHash(byte[] input) {
            Contract.Requires(input != null);

            return this.algorithm.ComputeHash(input);
        }
    }
}