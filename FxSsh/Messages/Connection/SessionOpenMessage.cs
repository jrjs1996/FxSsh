using System;

namespace FxSsh.Messages.Connection {
    public class SessionOpenMessage : ChannelOpenMessage {
        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);

            if (this.ChannelType != "session")
                throw new ArgumentException($"Channel type {this.ChannelType} is not valid.");
        }
    }
}