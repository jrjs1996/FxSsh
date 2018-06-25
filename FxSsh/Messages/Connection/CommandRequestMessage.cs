using System.Text;

namespace FxSsh.Messages.Connection {
    public class CommandRequestMessage : ChannelRequestMessage {
        public string Command { get; private set; }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);

            this.Command = reader.ReadString(Encoding.ASCII);
        }
    }
}