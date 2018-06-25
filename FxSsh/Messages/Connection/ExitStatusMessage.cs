namespace FxSsh.Messages.Connection {
    public class ExitStatusMessage : ChannelRequestMessage {
        public uint ExitStatus { get; set; }

        protected override void OnGetPacket(SshDataWorker writer) {
            this.RequestType = "exit-status";
            this.WantReply = false;

            base.OnGetPacket(writer);

            writer.Write(this.ExitStatus);
        }
    }
}