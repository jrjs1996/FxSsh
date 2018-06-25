using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class CtrModeCryptoTransform : ICryptoTransform {
        private readonly SymmetricAlgorithm algorithm;

        private readonly ICryptoTransform transform;

        private readonly byte[] iv;

        private readonly byte[] block;

        public CtrModeCryptoTransform(SymmetricAlgorithm algorithm) {
            Contract.Requires(algorithm != null);

            algorithm.Mode = CipherMode.ECB;
            algorithm.Padding = PaddingMode.None;

            this.algorithm = algorithm;
            this.transform = algorithm.CreateEncryptor();
            this.iv = algorithm.IV;
            this.block = new byte[algorithm.BlockSize >> 3];
        }

        public bool CanReuseTransform => true;

        public bool CanTransformMultipleBlocks => true;

        public int InputBlockSize => this.algorithm.BlockSize;

        public int OutputBlockSize => this.algorithm.BlockSize;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
            var written = 0;
            var bytesPerBlock = this.InputBlockSize >> 3;

            for (var i = 0; i < inputCount; i += bytesPerBlock) {
                written += this.transform.TransformBlock(this.iv, 0, bytesPerBlock, this.block, 0);

                for (var j = 0; j < bytesPerBlock; j++)
                    outputBuffer[outputOffset + i + j] = (byte) (this.block[j] ^ inputBuffer[inputOffset + i + j]);

                var k = this.iv.Length;
                while (--k >= 0 && ++this.iv[k] == 0) {
                }
            }

            return written;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
            var output = new byte[inputCount];
            this.TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        public void Dispose() {
            this.transform.Dispose();
        }
    }
}