using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using FxSsh.Algorithms;

namespace FxSsh {
    public static class KeyUtils {
        public static string[] SupportedAlgorithms => new[] {"ssh-rsa", "ssh-dss"};

        public static string GetFingerprint(string sshkey) {
            Contract.Requires(sshkey != null);

            using (var md5 = new MD5CryptoServiceProvider()) {
                var bytes = Convert.FromBase64String(sshkey);
                bytes = md5.ComputeHash(bytes);
                return BitConverter.ToString(bytes).Replace('-', ':');
            }
        }

        private static PublicKeyAlgorithm GetKeyAlgorithm(string type) {
            Contract.Requires(type != null);

            switch (type) {
                case "ssh-rsa":
                    return new RsaKey();
                case "ssh-dss":
                    return new DssKey();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static string GeneratePrivateKey(string type) {
            Contract.Requires(type != null);

            var alg = GetKeyAlgorithm(type);
            var bytes = alg.ExportKey();
            return Convert.ToBase64String(bytes);
        }
    }
}