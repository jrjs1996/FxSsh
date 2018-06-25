using System.Text;

namespace FxSsh.Messages {
    [Message("SSH_MSG_SERVICE_ACCEPT", messageNumber)]
    public class ServiceAcceptMessage : Message {
        private const byte messageNumber = 6;

        public ServiceAcceptMessage(string name) {
            this.ServiceName = name;
        }

        public string ServiceName { get; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.ServiceName, Encoding.ASCII);
        }
    }
}