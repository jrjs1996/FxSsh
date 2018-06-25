using FxSsh.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace FxSsh.Services
{
    public class MessageReceivedArgs
    {
        public MessageReceivedArgs(SessionChannel channel, byte[] data)
        {
            Contract.Requires(channel != null);
            Contract.Requires(data != null);

            Channel = channel;
            Data = data;
        }

        public SessionChannel Channel { get; private set; }
        public byte[] Data { get; private set; }
    }
}
