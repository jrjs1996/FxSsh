using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using FxSsh.Messages.Connection;

namespace FxSsh.Services {
    public abstract class Channel {
        
        protected ConnectionService ConnectionService;

        protected EventWaitHandle SendingWindowWaitHandle = new ManualResetEvent(false);

        protected Channel(ConnectionService connectionService,
                       uint clientChannelId, uint clientInitialWindowSize, uint clientMaxPacketSize,
                       uint serverChannelId) {
            Contract.Requires(connectionService != null);

            this.ConnectionService = connectionService;

            this.ClientChannelId = clientChannelId;
            this.ClientInitialWindowSize = clientInitialWindowSize;
            this.ClientWindowSize = clientInitialWindowSize;
            this.ClientMaxPacketSize = clientMaxPacketSize;

            this.ServerChannelId = serverChannelId;
            this.ServerInitialWindowSize = Session.InitialLocalWindowSize;
            this.ServerWindowSize = Session.InitialLocalWindowSize;
            this.ServerMaxPacketSize = Session.LocalChannelDataPacketSize;

            this.WhenConnected = DateTime.Now;
        }

        public DateTime WhenConnected { get; }

        public uint ClientChannelId { get; }

        public uint ClientInitialWindowSize { get; }

        public uint ClientWindowSize { get; protected set; }

        public uint ClientMaxPacketSize { get; }

        public uint ServerChannelId { get; }

        public uint ServerInitialWindowSize { get; }

        public uint ServerWindowSize { get; protected set; }

        public uint ServerMaxPacketSize { get; }

        public bool ClientClosed { get; private set; }

        public bool ClientMarkedEof { get; private set; }

        public bool ServerClosed { get; private set; }

        public bool ServerMarkedEof { get; private set; }

        public event EventHandler EofReceived;

        public event EventHandler CloseReceived;

        public int ClientPort => ((IPEndPoint)this.ConnectionService.Session.RemoteEndPoint).Port;

        public void SendData(byte[] data) {
            Contract.Requires(data != null);

            var msg = new ChannelDataMessage {RecipientChannel = this.ClientChannelId};

            var total = (uint) data.Length;
            var offset = 0L;
            byte[] buf = null;
            do {
                var packetSize = Math.Min(Math.Min(this.ClientWindowSize, this.ClientMaxPacketSize), total);
                if (packetSize == 0) {
                    this.SendingWindowWaitHandle.WaitOne();
                    continue;
                }

                if (buf == null || packetSize != buf.Length)
                    buf = new byte[packetSize];
                Array.Copy(data, offset, buf, 0, packetSize);

                msg.Data = buf;
                this.ConnectionService.Session.SendMessage(msg);

                this.ClientWindowSize -= packetSize;
                total -= packetSize;
                offset += packetSize;
            } while (total > 0);
        }

        public void SendEof() {
            if (this.ServerMarkedEof)
                return;

            this.ServerMarkedEof = true;
            var msg = new ChannelEofMessage {RecipientChannel = this.ClientChannelId};
            this.ConnectionService.Session.SendMessage(msg);
        }

        public void SendClose(uint? exitCode = null) {
            if (this.ServerClosed)
                return;

            this.ServerClosed = true;
            if (exitCode.HasValue)
                this.ConnectionService.Session.SendMessage(new ExitStatusMessage {RecipientChannel = this.ClientChannelId, ExitStatus = exitCode.Value});
            this.ConnectionService.Session.SendMessage(new ChannelCloseMessage {RecipientChannel = this.ClientChannelId});

            this.CheckBothClosed();
        }

        internal void OnEof() {
            this.ClientMarkedEof = true;

            this.EofReceived?.Invoke(this, EventArgs.Empty);
        }

        internal void OnClose() {
            this.ClientClosed = true;

            this.CloseReceived?.Invoke(this, EventArgs.Empty);

            this.CheckBothClosed();
        }

        internal void ClientAdjustWindow(uint bytesToAdd) {
            this.ClientWindowSize += bytesToAdd;

            // pulse multithreadings in same time and unsignal until thread switched
            // don't try to use AutoResetEvent
            this.SendingWindowWaitHandle.Set();
            Thread.Sleep(1);
            this.SendingWindowWaitHandle.Reset();
        }

        protected void ServerAttemptAdjustWindow(uint messageLength) {
            this.ServerWindowSize -= messageLength;
            if (this.ServerWindowSize > this.ServerMaxPacketSize) return;
            this.ConnectionService.Session.SendMessage(new ChannelWindowAdjustMessage {
                RecipientChannel = this.ClientChannelId,
                BytesToAdd = this.ServerInitialWindowSize - this.ServerWindowSize
            });
            this.ServerWindowSize = this.ServerInitialWindowSize;
        }

        private void CheckBothClosed() {
            if (this.ClientClosed && this.ServerClosed) {
                this.ForceClose();
            }
        }

        internal void ForceClose() {
            this.ConnectionService.RemoveChannel(this);
            this.SendingWindowWaitHandle.Set();
            this.SendingWindowWaitHandle.Close();
        }
    }
}