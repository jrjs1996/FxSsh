using System;
using System.Text;

namespace FxSsh.Messages.Connection {
    class TerminalRequestMessage : ChannelRequestMessage {
        public string Terminal { get; private set; }

        public UInt32 TerminalWidthCharacters { get; private set; }

        public UInt32 TerminalHeightRows { get; private set; }

        public UInt32 TerminalWidthPixels { get; private set; }

        public UInt32 TerminalHeightPixels { get; private set; }

        public string EncodedTerminalModes { get; private set; }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);

            this.Terminal = reader.ReadString(Encoding.ASCII);
            this.TerminalWidthCharacters = reader.ReadUInt32();
            this.TerminalHeightRows = reader.ReadUInt32();
            this.TerminalWidthPixels = reader.ReadUInt32();
            this.TerminalHeightPixels = reader.ReadUInt32();
            this.EncodedTerminalModes = reader.ReadString(Encoding.ASCII);
        }
    }
}