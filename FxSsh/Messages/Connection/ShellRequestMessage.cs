namespace FxSsh.Messages.Connection {
    class ShellRequestMessage : ChannelRequestMessage {
        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);
        }
    }
}