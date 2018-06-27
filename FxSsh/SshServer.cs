using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FxSsh {
    public class SshServer : IDisposable {
        private readonly object _lock = new object();

        private readonly List<Session> sessions = new List<Session>();

        private readonly Dictionary<string, string> hostKey = new Dictionary<string, string>();

        private bool isDisposed;

        private bool started;

        private TcpListener listenser;

        public SshServer()
                : this(new StartingInfo()) {
        }

        public SshServer(StartingInfo info) {
            Contract.Requires(info != null);

            this.StartingInfo = info;
        }

        public StartingInfo StartingInfo { get; }

        public event EventHandler<Session> ConnectionAccepted;

        public event EventHandler<Exception> ExceptionRasied;

        public Stream Connect(string clientName, uint port) {
            var authenticationMethods = new List<AuthenticationMethod>() {
                AuthenticationMethod.Password
            };
            var client = this.sessions.First(s => s.Username == clientName);
            return new Stream(client, "169.254.73.253", 22, this.hostKey, authenticationMethods);
        }

        public void Start() {
            lock (this._lock) {
                this.CheckDisposed();
                if (this.started)
                    throw new InvalidOperationException("The server is already started.");

                this.listenser = Equals(this.StartingInfo.LocalAddress, IPAddress.IPv6Any)
                                          ? TcpListener.Create(this.StartingInfo.Port) // dual stack
                                          : new TcpListener(this.StartingInfo.LocalAddress, this.StartingInfo.Port);
                this.listenser.ExclusiveAddressUse = false;
                this.listenser.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                this.listenser.Start();
                this.BeginAcceptSocket();

                this.started = true;
            }
        }

        public void Stop() {
            lock (this._lock) {
                this.CheckDisposed();
                if (!this.started)
                    throw new InvalidOperationException("The server is not started.");

                this.listenser.Stop();

                this.isDisposed = true;
                this.started = false;

                foreach (var session in this.sessions) {
                    try {
                        session.Disconnect();
                    } catch {
                        // ignored
                    }
                }
            }
        }

        public void AddHostKey(string type, string xml) {
            Contract.Requires(type != null);
            Contract.Requires(xml != null);

            if (!this.hostKey.ContainsKey(type))
                this.hostKey.Add(type, xml);
        }

        private void BeginAcceptSocket() {
            try  {
                this.listenser.BeginAcceptSocket(this.AcceptSocket, null);
            } catch (ObjectDisposedException) {
            } catch {
                if (this.started)
                    this.BeginAcceptSocket();
            }
        }

        private void AcceptSocket(IAsyncResult ar) {
            try {
                var socket = this.listenser.EndAcceptSocket(ar);
                Task.Run(() => {
                    var authenticationMethods = new List<AuthenticationMethod>() {
                        AuthenticationMethod.Password
                    };

                    var session = new Session(socket, this.hostKey, authenticationMethods);
                    session.Disconnected += (ss, ee) => {
                        lock (this._lock) this.sessions.Remove(session);
                    };
                    lock (this._lock)
                        this.sessions.Add(session);
                    try {
                        this.ConnectionAccepted?.Invoke(this, session);
                        session.EstablishConnection();
                    } catch (SshConnectionException ex) {
                        session.Disconnect(ex.DisconnectReason, ex.Message);
                        this.ExceptionRasied?.Invoke(this, ex);
                    } catch (Exception ex) {
                        session.Disconnect();
                        this.ExceptionRasied?.Invoke(this, ex);
                    }
                });
            } catch {
                // ignored
            } finally {
                this.BeginAcceptSocket();
            }
        }

        private void CheckDisposed() {
            if (this.isDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);
        }

        #region IDisposable

        public void Dispose() {
            lock (this._lock) {
                if (this.isDisposed)
                    return;
                this.Stop();
            }
        }

        #endregion
    }
}