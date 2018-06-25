using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class EncryptionAlgorithm {
        private readonly SymmetricAlgorithm algorithm;

        private readonly CipherModeEx mode;

        private readonly ICryptoTransform transform;

        public EncryptionAlgorithm(SymmetricAlgorithm algorithm, int keySize, CipherModeEx mode, byte[] key, byte[] iv, bool isEncryption) {
            Contract.Requires(algorithm != null);
            Contract.Requires(key != null);
            Contract.Requires(iv != null);
            Contract.Requires(keySize == key.Length << 3);

            algorithm.KeySize = keySize;
            algorithm.Key = key;
            algorithm.IV = iv;
            algorithm.Padding = PaddingMode.None;

            this.algorithm = algorithm;
            this.mode = mode;

            this.transform = this.CreateTransform(isEncryption);
        }

        public int BlockBytesSize => this.algorithm.BlockSize >> 3;

        public byte[] Transform(byte[] input) {
            var output = new byte[input.Length];
            this.transform.TransformBlock(input, 0, input.Length, output, 0);
            return output;
        }

        private ICryptoTransform CreateTransform(bool isEncryption) {
            switch (this.mode) {
                case CipherModeEx.Cbc:
                    this.algorithm.Mode = CipherMode.CBC;
                    return isEncryption
                                   ? this.algorithm.CreateEncryptor()
                                   : this.algorithm.CreateDecryptor();
                case CipherModeEx.Ctr:
                    return new CtrModeCryptoTransform(this.algorithm);
                default:
                    throw new InvalidEnumArgumentException($"Invalid mode: {this.mode}");
            }
        }
    }
}