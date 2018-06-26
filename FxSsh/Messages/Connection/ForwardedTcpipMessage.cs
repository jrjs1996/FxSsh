using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Connection
{
    class ForwardedTcpipMessage : ChannelOpenMessage
    {
        public string ConnectedAddress { get; private set; }

        public uint ConnectedPort { get; private set; }

        public string OriginatorAddress { get; private set; }

        public uint OriginatorPort { get; private set; }

        public ForwardedTcpipMessage() { }

        public ForwardedTcpipMessage(string channelType, uint senderChannel,
                                  uint initialWindowSize, uint maxPacketSize,
                                  string connectedAddress, uint connectedPort,
                                  string originatorAddress, uint originatorPort)
        {
            this.ChannelType = channelType;
            this.SenderChannel = senderChannel;
            this.InitialWindowSize = initialWindowSize;
            this.MaximumPacketSize = maxPacketSize;
            this.ConnectedAddress = connectedAddress;
            this.ConnectedPort = connectedPort;
            this.OriginatorAddress = originatorAddress;
            this.OriginatorPort = originatorPort;
        }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);
            this.ConnectedAddress = reader.ReadString(Encoding.ASCII);
            this.ConnectedPort = reader.ReadUInt32();
            this.OriginatorAddress = reader.ReadString(Encoding.ASCII);
            this.OriginatorPort = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            base.OnGetPacket(writer);
            writer.Write(this.ConnectedAddress, Encoding.ASCII);
            writer.Write(this.ConnectedPort);
            writer.Write(this.OriginatorAddress, Encoding.ASCII);
            writer.Write(this.OriginatorPort);
        }
    }
}
