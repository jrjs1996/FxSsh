using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FxSsh.Exceptions;
using JetBrains.Annotations;
using Renci.SshNet.Messages.Connection;

namespace FxSsh {

    public class SshServer : IDisposable {

        public List<AuthenticationMethod> AuthenticationMethods;

        private IClientKeyRepository clientKeyRepository;

        private IPEndPoint localEndPoint;

        private readonly object _lock = new object();

        private List<SshClient> clients = new List<SshClient>();

        private readonly Dictionary<string, string> hostKey = new Dictionary<string, string>();

        private bool isDisposed;

        private bool started;

        private TcpListener listenser;

        public SshServer(IPAddress address, int port) {
            this.localEndPoint = new IPEndPoint(address, port);
            this.AuthenticationMethods = new List<AuthenticationMethod>();

            // Temporary fix. Figure out why connections aren't working without this so
            // servers can use a key for each client
            this.AddHostKey("ssh-rsa", "BwIAAACkAABSU0EyAAQAAAEAAQADKjiW5UyIad8ITutLjcdtejF4wPA1dk1JFHesDMEhU9pGUUs+HPTmSn67ar3UvVj/1t/+YK01FzMtgq4GHKzQHHl2+N+onWK4qbIAMgC6vIcs8u3d38f3NFUfX+lMnngeyxzbYITtDeVVXcLnFd7NgaOcouQyGzYrHBPbyEivswsnqcnF4JpUTln29E1mqt0a49GL8kZtDfNrdRSt/opeexhCuzSjLPuwzTPc6fKgMc6q4MBDBk53vrFY2LtGALrpg3tuydh3RbMLcrVyTNT+7st37goubQ2xWGgkLvo+TZqu3yutxr1oLSaPMSmf9bTACMi5QDicB3CaWNe9eU73MzhXaFLpNpBpLfIuhUaZ3COlMazs7H9LCJMXEL95V6ydnATf7tyO0O+jQp7hgYJdRLR3kNAKT0HU8enE9ZbQEXG88hSCbpf1PvFUytb1QBcotDy6bQ6vTtEAZV+XwnUGwFRexERWuu9XD6eVkYjA4Y3PGtSXbsvhwgH0mTlBOuH4soy8MV4dxGkxM8fIMM0NISTYrPvCeyozSq+NDkekXztFau7zdVEYmhCqIjeMNmRGuiEo8ppJYj4CvR1hc8xScUIw7N4OnLISeAdptm97ADxZqWWFZHno7j7rbNsq5ysdx08OtplghFPx4vNHlS09LwdStumtUel5oIEVMYv+yWBYSPPZBcVY5YFyZFJzd0AOkVtUbEbLuzRs5AtKZG01Ip/8+pZQvJvdbBMLT1BUvHTrccuRbY03SHIaUM3cTUc=");
            this.AddHostKey("ssh-dss", "BwIAAAAiAABEU1MyAAQAAG+6KQWB+crih2Ivb6CZsMe/7NHLimiTl0ap97KyBoBOs1amqXB8IRwI2h9A10R/v0BHmdyjwe0c0lPsegqDuBUfD2VmsDgrZ/i78t7EJ6Sb6m2lVQfTT0w7FYgVk3J1Deygh7UcbIbDoQ+refeRNM7CjSKtdR+/zIwO3Qub2qH+p6iol2iAlh0LP+cw+XlH0LW5YKPqOXOLgMIiO+48HZjvV67pn5LDubxru3ZQLvjOcDY0pqi5g7AJ3wkLq5dezzDOOun72E42uUHTXOzo+Ct6OZXFP53ZzOfjNw0SiL66353c9igBiRMTGn2gZ+au0jMeIaSsQNjQmWD+Lnri39n0gSCXurDaPkec+uaufGSG9tWgGnBdJhUDqwab8P/Ipvo5lS5p6PlzAQAAACqx1Nid0Ea0YAuYPhg+YolsJ/ce");
        }

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

        private bool connectingClient = false;
        
        [NotNull]
        public Stream Connect(string clientName, int portNumber) {
            var client = this.GetConnectedClients().FirstOrDefault(c => c.Name == clientName);
            if (client == null)
                throw new SshClientNotFoundException();
            this.connectingClient = true;
            try {
                return client.Connect(portNumber);
            } catch (Exception exception) {
                throw new SshClientConnectionException();
            }
                     
        }

        //public SshStream ConnectSsh(string clientName) {
        //    var client = this.clients.First(c => c.Name == clientName);
        //    return new SshStream(client.);
        //}

        [NotNull]
        public ImmutableArray<SshClient> GetConnectedClients() {
            return this.clients.ToImmutableArray();
        }

        public void SetClientKeyRepository([NotNull] IClientKeyRepository clientKeyRep) {
            this.clientKeyRepository = clientKeyRep;
        }

        public void Start() {
            lock (this._lock) {
                this.CheckDisposed();
                if (this.started)
                    throw new InvalidOperationException("The server is already started.");

                this.listenser = Equals(this.localEndPoint.Address, IPAddress.IPv6Any)
                                         ? TcpListener.Create(this.localEndPoint.Port) // dual stack
                                         : new TcpListener(this.localEndPoint.Address, this.localEndPoint.Port);
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

                foreach (var client in this.clients) {
                    try {
                        client.DisconnectSession();
                    } catch {
                        // ignored
                    }
                }
            }
        }

        public void AddHostKey([NotNull]string type, [NotNull]string xml) {

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
            if (this.connectingClient){
                this.connectingClient = false;
                return;
            }
            try {
                var socket = this.listenser.EndAcceptSocket(ar);
                Task.Run(() => {
                    var session = new Session(socket, this.hostKey, this.AuthenticationMethods, this.clientKeyRepository);
                    session.Disconnected += (ss, ee) => {
                        lock (this._lock) this.clients.Remove(this.clients.FirstOrDefault(c => c.Session == session));
                    };
                    lock (this._lock) {
                        var client = new SshClient(session);
                        this.clients.Add(client);
                    }
                        
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