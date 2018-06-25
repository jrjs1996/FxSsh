using System.Security.Cryptography;
using System.Text;

namespace FxSsh.Algorithms {
    public class RsaKey : PublicKeyAlgorithm {
        private readonly RSACryptoServiceProvider algorithm = new RSACryptoServiceProvider();

        public RsaKey(string key = null)
                : base(key) {
        }

        public override string Name => "ssh-rsa";

        public override void ImportKey(byte[] bytes) {
            this.algorithm.ImportCspBlob(bytes);
        }

        public override byte[] ExportKey() {
            return this.algorithm.ExportCspBlob(true);
        }

        public override void LoadKeyAndCertificatesData(byte[] data) {
            using (var worker = new SshDataWorker(data)) {
                if (worker.ReadString(Encoding.ASCII) != this.Name)
                    throw new CryptographicException("Key and certificates were not created with this algorithm.");

                var args = new RSAParameters {
                    Exponent = worker.ReadMpint(),
                    Modulus = worker.ReadMpint()
                };

                this.algorithm.ImportParameters(args);
            }
        }

        public override byte[] CreateKeyAndCertificatesData() {
            using (var worker = new SshDataWorker()) {
                var args = this.algorithm.ExportParameters(false);

                worker.Write(this.Name, Encoding.ASCII);
                worker.WriteMpint(args.Exponent);
                worker.WriteMpint(args.Modulus);

                return worker.ToByteArray();
            }
        }

        public override bool VerifyData(byte[] data, byte[] signature) {
            return this.algorithm.VerifyData(data, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        public override bool VerifyHash(byte[] hash, byte[] signature) {
            return this.algorithm.VerifyHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        public override byte[] SignData(byte[] data) {
            return this.algorithm.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        public override byte[] SignHash(byte[] hash) {
            return this.algorithm.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }
    }
}