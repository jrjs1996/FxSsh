using System.Diagnostics.Contracts;

namespace FxSsh.Services {
    public class UserauthArgs {
        public UserauthArgs(string keyAlgorithm, string fingerprint, byte[] key) {
            Contract.Requires(keyAlgorithm != null);
            Contract.Requires(fingerprint != null);
            Contract.Requires(key != null);

            this.KeyAlgorithm = keyAlgorithm;
            this.Fingerprint = fingerprint;
            this.Key = key;
        }

        public string KeyAlgorithm { get; }

        public string Fingerprint { get;}

        public byte[] Key { get; }

        public bool Result { get; set; }
    }
}