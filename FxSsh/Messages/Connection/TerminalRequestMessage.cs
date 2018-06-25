using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Connection
{
    class TerminalRequestMessage : ChannelRequestMessage
    {
        public string Terminal { get; private set; }
        public UInt32 TerminalWidthCharacters { get; private set; }
        public UInt32 TerminalHeightRows { get; private set; }
        public UInt32 TerminalWidthPixels { get; private set; }
        public UInt32 TerminalHeightPixels { get; private set; }
        public string EncodedTerminalModes { get; private set; }

        protected override void OnLoad(SshDataWorker reader)
        {
            base.OnLoad(reader);

            Terminal = reader.ReadString(Encoding.ASCII);
            TerminalWidthCharacters = reader.ReadUInt32();
            TerminalHeightRows = reader.ReadUInt32();
            TerminalWidthPixels = reader.ReadUInt32();
            TerminalHeightPixels = reader.ReadUInt32();
            EncodedTerminalModes = reader.ReadString(Encoding.ASCII);
        }
    }
}
