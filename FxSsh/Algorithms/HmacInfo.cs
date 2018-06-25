using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class HmacInfo {
        public HmacInfo(KeyedHashAlgorithm algorithm, int keySize) {
            Contract.Requires(algorithm != null);

            this.KeySize = keySize;
            this.Hmac = key => new HmacAlgorithm(algorithm, keySize, key);
        }

        public int KeySize { get; }

        public Func<byte[], HmacAlgorithm> Hmac { get; }
    }
}