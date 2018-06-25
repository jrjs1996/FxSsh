using System.Text;

namespace FxSsh.Messages {
    [Message("SSH_MSG_SERVICE_REQUEST", messageNumber)]
    public class ServiceRequestMessage : Message {
        private const byte messageNumber = 5;

        public string ServiceName { get; private set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.ServiceName = reader.ReadString(Encoding.ASCII);
        }
    }
}