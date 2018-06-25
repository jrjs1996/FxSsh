﻿using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;

namespace FxSsh.Algorithms {
    public class CipherInfo {
        public CipherInfo(SymmetricAlgorithm algorithm, int keySize, CipherModeEx mode) {
            Contract.Requires(algorithm != null);
            Contract.Requires(algorithm.LegalKeySizes.Any(x =>
                                                          x.MinSize <= keySize && keySize <= x.MaxSize && keySize % x.SkipSize == 0));

            algorithm.KeySize = keySize;
            this.KeySize = algorithm.KeySize;
            this.BlockSize = algorithm.BlockSize;
            this.Cipher = (key, vi, isEncryption) => new EncryptionAlgorithm(algorithm, keySize, mode, key, vi, isEncryption);
        }

        public int KeySize { get; }

        public int BlockSize { get; }

        public Func<byte[], byte[], bool, EncryptionAlgorithm> Cipher { get; }
    }
}