using System;
using System.Linq;
using System.Text;

namespace FxSsh.Messages.Userauth {
    public class PublicKeyRequestMessage : RequestMessage {
        public bool HasSignature { get; private set; }

        public string KeyAlgorithmName { get; private set; }

        public byte[] PublicKey { get; private set; }

        public byte[] Signature { get; private set; }

        public byte[] PayloadWithoutSignature { get; private set; }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);

            if (this.MethodName != "publickey")
                throw new ArgumentException($"Method name {this.MethodName} is not valid.");

            this.HasSignature = reader.ReadBoolean();
            this.KeyAlgorithmName = reader.ReadString(Encoding.ASCII);
            this.PublicKey = reader.ReadBinary();

            if (!this.HasSignature) return;
            this.Signature = reader.ReadBinary();
            this.PayloadWithoutSignature = this.RawBytes.Take(this.RawBytes.Length - this.Signature.Length - 5).ToArray();
        }
    }
}