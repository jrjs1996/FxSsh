using System.Text;

namespace FxSsh.Messages.Userauth {
    [Message("SSH_MSG_USERAUTH_PK_OK", messageNumber)]
    public class PublicKeyOkMessage : UserauthServiceMessage {
        private const byte messageNumber = 60;

        public string KeyAlgorithmName { get; set; }

        public byte[] PublicKey { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.KeyAlgorithmName, Encoding.ASCII);
            writer.WriteBinary(this.PublicKey);
        }
    }
}