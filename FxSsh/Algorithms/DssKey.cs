using System.Security.Cryptography;
using System.Text;

namespace FxSsh.Algorithms {
    public class DssKey : PublicKeyAlgorithm {
        private readonly DSACryptoServiceProvider algorithm = new DSACryptoServiceProvider();

        public DssKey(string key = null)
                : base(key) {
        }

        public override string Name => "ssh-dss";

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

                var args = new DSAParameters {
                    P = worker.ReadMpint(),
                    Q = worker.ReadMpint(),
                    G = worker.ReadMpint(),
                    Y = worker.ReadMpint()
                };

                this.algorithm.ImportParameters(args);
            }
        }

        public override byte[] CreateKeyAndCertificatesData() {
            using (var worker = new SshDataWorker()) {
                var args = this.algorithm.ExportParameters(false);

                worker.Write(this.Name, Encoding.ASCII);
                worker.WriteMpint(args.P);
                worker.WriteMpint(args.Q);
                worker.WriteMpint(args.G);
                worker.WriteMpint(args.Y);

                return worker.ToByteArray();
            }
        }

        public override bool VerifyData(byte[] data, byte[] signature) {
            return this.algorithm.VerifyData(data, signature);
        }

        public override bool VerifyHash(byte[] hash, byte[] signature) {
            return this.algorithm.VerifyHash(hash, "SHA1", signature);
        }

        public override byte[] SignData(byte[] data) {
            return this.algorithm.SignData(data);
        }

        public override byte[] SignHash(byte[] hash) {
            return this.algorithm.SignHash(hash, "SHA1");
        }
    }
}