using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using FxSsh.Messages;
using FxSsh.Messages.Connection;

namespace FxSsh.Services {
    public class ConnectionService : SshService {
        private readonly object locker = new object();

        private readonly List<Channel> channels = new List<Channel>();

        private readonly UserauthArgs auth;

        private int serverChannelCounter = -1;

        private int forwardChannelCounter = -1;

        public ConnectionService(Session session, UserauthArgs auth)
                : base(session) {
            Contract.Requires(auth != null);

            this.auth = auth;
        }

        public event EventHandler<SessionRequestedArgs> CommandOpened;

        protected internal override void CloseService() {
            lock (this.locker)
                foreach (var channel in this.channels.ToArray()) {
                    channel.ForceClose();
                }
        }

        internal void HandleMessageCore(ConnectionServiceMessage message) {
            Contract.Requires(message != null);

            typeof(ConnectionService)
                    .GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {message.GetType()}, null)?
                    .Invoke(this, new object[] {message});
        }

        private void HandleMessage(ChannelOpenMessage message) {
            switch (message.ChannelType) {
                case "session":
                    var msg = Message.LoadFrom<SessionOpenMessage>(message);
                    this.HandleMessage(msg);
                    break;
                default:
                    this.Session.SendMessage(new ChannelOpenFailureMessage {
                        RecipientChannel = message.SenderChannel,
                        ReasonCode = ChannelOpenFailureReason.UnknownChannelType,
                        Description = $"Unknown channel type: {message.ChannelType}.",
                    });
                    throw new SshConnectionException($"Unknown channel type: {message.ChannelType}.");
            }
        }

        private void HandleMessage(ChannelRequestMessage message) {
            switch (message.RequestType) {
                case "env":
                    var envMsg = Message.LoadFrom<EnvironmentRequestMessage>(message);
                    this.HandleMessage(envMsg);
                    break;
                case "exec":
                    var execMsg = Message.LoadFrom<CommandRequestMessage>(message);
                    this.HandleMessage(execMsg);
                    break;
                case "pty-req":
                    var termMsg = Message.LoadFrom<TerminalRequestMessage>(message);
                    this.HandleMessage(termMsg);
                    break;
                case "shell":
                    var shellMsg = Message.LoadFrom<ShellRequestMessage>(message);
                    this.HandleMessage(shellMsg);
                    break;
                default:
                    if (message.WantReply)
                        this.Session.SendMessage(new ChannelFailureMessage {
                            RecipientChannel = this.FindChannelByServerId<Channel>(message.RecipientChannel).ClientChannelId
                        });
                    throw new SshConnectionException($"Unknown request type: {message.RequestType}.");
            }
        }

        private void HandleMessage(ChannelDataMessage message) {
            var channel = this.FindChannelByServerId<SessionChannel>(message.RecipientChannel); 
            channel.OnData(message.Data);
        }

        private void HandleMessage(ChannelWindowAdjustMessage message) {
            var channel = this.FindChannelByServerId<Channel>(message.RecipientChannel);
            channel.ClientAdjustWindow(message.BytesToAdd);
        }

        private void HandleMessage(ChannelEofMessage message) {
            var channel = this.FindChannelByServerId<Channel>(message.RecipientChannel);
            channel.OnEof();
        }

        private void HandleMessage(ChannelCloseMessage message) {
            var channel = this.FindChannelByServerId<Channel>(message.RecipientChannel);
            channel.OnClose();
        }

        private void HandleMessage(SessionOpenMessage message) {
            var channel = new SessionChannel(
                    this,
                    message.SenderChannel,
                    message.InitialWindowSize,
                    message.MaximumPacketSize,
                    (uint) Interlocked.Increment(ref this.serverChannelCounter));

            lock (this.locker)
                this.channels.Add(channel);

            var msg = new SessionOpenConfirmationMessage {
                RecipientChannel = channel.ClientChannelId,
                SenderChannel = channel.ServerChannelId,
                InitialWindowSize = channel.ServerInitialWindowSize,
                MaximumPacketSize = channel.ServerMaxPacketSize
            };

            this.Session.SendMessage(msg);
        }

        private void HandleMessage(CommandRequestMessage message) {
            var channel = this.FindChannelByServerId<SessionChannel>(message.RecipientChannel);

            if (message.WantReply)
                this.Session.SendMessage(new ChannelSuccessMessage {RecipientChannel = channel.ClientChannelId});

            if (this.CommandOpened == null) return;
            var args = new SessionRequestedArgs(channel, message.Command, this.auth);
            this.CommandOpened(this, args);
        }

        private void HandleMessage(TerminalRequestMessage message) {
            var channel = this.FindChannelByServerId<SessionChannel>(message.RecipientChannel);
            channel.Terminal = new Terminal(message.Terminal, message.TerminalWidthCharacters, message.TerminalHeightRows,
                                            message.TerminalWidthPixels, message.TerminalHeightPixels, message.EncodedTerminalModes,
                                            channel);

            this.Session.SendMessage(new ChannelSuccessMessage(channel.ClientChannelId));
            this.Session.SendMessage(new ChannelWindowAdjustMessage(channel.ClientChannelId, 2097152));
        }

        private void HandleMessage(ShellRequestMessage message) {
            var channel = this.FindChannelByServerId<SessionChannel>(message.RecipientChannel);

            if (message.WantReply)
                this.Session.SendMessage(new ChannelSuccessMessage {RecipientChannel = channel.ClientChannelId});

            if (this.CommandOpened == null) return;
            var args = new SessionRequestedArgs(channel, "shell", this.auth);
            this.CommandOpened(this, args);
        }

        private void HandleMessage(EnvironmentRequestMessage message) {
            var channel = this.FindChannelByServerId<SessionChannel>(message.RecipientChannel);
            channel.Terminal.SetEnvironmentVariable(message.VariableName, message.VariableValue);

            if (message.WantReply)
                this.Session.SendMessage(new ChannelSuccessMessage(channel.ClientChannelId));
        }

        private void HandleMessage(GlobalRequestMessage message) {
            switch (message.RequestType) {
                case "tcpip-forward":
                    var forwardMsg = Message.LoadFrom<TcpipForwardMessage>(message);
                    this.HandleMessage(forwardMsg);
                    break;
                case "cancel-tcpip-forward":
                    // Unimplemented
                    break;
                default:
                    break;

            }
        }

        private void HandleMessage(TcpipForwardMessage message) {
            var channel = new SessionChannel(
                    this,
                    (uint)Interlocked.Increment(ref this.forwardChannelCounter),
                    1048576,
                    16384,
                    (uint)Interlocked.Increment(ref this.serverChannelCounter));

            lock (this.locker)
                this.channels.Add(channel);
            this.Session.SendMessage(new RequestSuccessMessage());
            this.Session.SendMessage(new ForwardedTcpipMessage("forwarded-tcpip", channel.ServerChannelId, channel.ClientInitialWindowSize,
                                                               channel.ClientMaxPacketSize, message.Address, message.Port, "169.254.73.253", 22));
        }

        private T FindChannelByClientId<T>(uint id) where T : Channel {
            lock (this.locker) {
                if (!(this.channels.FirstOrDefault(x => x.ClientChannelId == id) is T channel))
                    throw new SshConnectionException($"Invalid client channel id {id}.",
                                                     DisconnectReason.ProtocolError);

                return channel;
            }
        }

        private T FindChannelByServerId<T>(uint id) where T : Channel {
            lock (this.locker) {
                if (!(this.channels.FirstOrDefault(x => x.ServerChannelId == id) is T channel))
                    throw new SshConnectionException($"Invalid server channel id {id}.",
                                                     DisconnectReason.ProtocolError);

                return channel;
            }
        }

        internal void RemoveChannel(Channel channel) {
            lock (this.locker) {
                this.channels.Remove(channel);
            }
        }
    }
}