using System.Security.Cryptography;
using System.Text;

namespace FxSsh.Messages {
    [Message("SSH_MSG_KEXINIT", messageNumber)]
    public class KeyExchangeInitMessage : Message {
        private const byte messageNumber = 20;

        private static readonly RandomNumberGenerator rng = new RNGCryptoServiceProvider();

        public KeyExchangeInitMessage() {
            this.Cookie = new byte[16];
            rng.GetBytes(this.Cookie);
        }

        public byte[] Cookie { get; private set; }

        public string[] KeyExchangeAlgorithms { get; set; }

        public string[] ServerHostKeyAlgorithms { get; set; }

        public string[] EncryptionAlgorithmsClientToServer { get; set; }

        public string[] EncryptionAlgorithmsServerToClient { get; set; }

        public string[] MacAlgorithmsClientToServer { get; set; }

        public string[] MacAlgorithmsServerToClient { get; set; }

        public string[] CompressionAlgorithmsClientToServer { get; set; }

        public string[] CompressionAlgorithmsServerToClient { get; set; }

        public string[] LanguagesClientToServer { get; set; }

        public string[] LanguagesServerToClient { get; set; }

        public bool FirstKexPacketFollows { get; set; }

        public uint Reserved { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.Cookie = reader.ReadBinary(16);
            this.KeyExchangeAlgorithms = reader.ReadString(Encoding.ASCII).Split(',');
            this.ServerHostKeyAlgorithms = reader.ReadString(Encoding.ASCII).Split(',');
            this.EncryptionAlgorithmsClientToServer = reader.ReadString(Encoding.ASCII).Split(',');
            this.EncryptionAlgorithmsServerToClient = reader.ReadString(Encoding.ASCII).Split(',');
            this.MacAlgorithmsClientToServer = reader.ReadString(Encoding.ASCII).Split(',');
            this.MacAlgorithmsServerToClient = reader.ReadString(Encoding.ASCII).Split(',');
            this.CompressionAlgorithmsClientToServer = reader.ReadString(Encoding.ASCII).Split(',');
            this.CompressionAlgorithmsServerToClient = reader.ReadString(Encoding.ASCII).Split(',');
            this.LanguagesClientToServer = reader.ReadString(Encoding.ASCII).Split(',');
            this.LanguagesServerToClient = reader.ReadString(Encoding.ASCII).Split(',');
            this.FirstKexPacketFollows = reader.ReadBoolean();
            this.Reserved = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.Cookie);
            writer.Write(string.Join(",", this.KeyExchangeAlgorithms), Encoding.ASCII);
            writer.Write(string.Join(",", this.ServerHostKeyAlgorithms), Encoding.ASCII);
            writer.Write(string.Join(",", this.EncryptionAlgorithmsClientToServer), Encoding.ASCII);
            writer.Write(string.Join(",", this.EncryptionAlgorithmsServerToClient), Encoding.ASCII);
            writer.Write(string.Join(",", this.MacAlgorithmsClientToServer), Encoding.ASCII);
            writer.Write(string.Join(",", this.MacAlgorithmsServerToClient), Encoding.ASCII);
            writer.Write(string.Join(",", this.CompressionAlgorithmsClientToServer), Encoding.ASCII);
            writer.Write(string.Join(",", this.CompressionAlgorithmsServerToClient), Encoding.ASCII);
            writer.Write(string.Join(",", this.LanguagesClientToServer), Encoding.ASCII);
            writer.Write(string.Join(",", this.LanguagesServerToClient), Encoding.ASCII);
            writer.Write(this.FirstKexPacketFollows);
            writer.Write(this.Reserved);
        }
    }
}