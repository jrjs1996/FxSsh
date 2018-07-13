using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FxSsh.Algorithms;
using FxSsh.Messages;
using FxSsh.Messages.Connection;
using FxSsh.Messages.Userauth;
using FxSsh.Services;
using JetBrains.Annotations;

namespace FxSsh {
    public class Session {

        internal Dictionary<AuthenticationMethod, bool> AuthenticationMethods;

        public IClientKeyRepository clientKeyRepository;

        private const byte carriageReturn = 0x0d;

        private const byte lineFeed = 0x0a;

        internal const int MaximumSshPacketSize = LocalChannelDataPacketSize;

        internal const int InitialLocalWindowSize = LocalChannelDataPacketSize * 32;

        internal const int LocalChannelDataPacketSize = 1024 * 32;

        private static readonly RandomNumberGenerator rng = new RNGCryptoServiceProvider();

        private static readonly Dictionary<byte, Type> messagesMetadata;

        internal static readonly Dictionary<string, Func<KexAlgorithm>> KeyExchangeAlgorithms =
                new Dictionary<string, Func<KexAlgorithm>>();

        internal static readonly Dictionary<string, Func<string, PublicKeyAlgorithm>> PublicKeyAlgorithms =
                new Dictionary<string, Func<string, PublicKeyAlgorithm>>();

        internal static readonly Dictionary<string, Func<CipherInfo>> EncryptionAlgorithms =
                new Dictionary<string, Func<CipherInfo>>();

        internal static readonly Dictionary<string, Func<HmacInfo>> HmacAlgorithms =
                new Dictionary<string, Func<HmacInfo>>();

        internal static readonly Dictionary<string, Func<CompressionAlgorithm>> CompressionAlgorithms =
                new Dictionary<string, Func<CompressionAlgorithm>>();

        private readonly object locker = new object();

        private readonly Socket socket;

        internal IPAddress remoteAddress {
            get {
                var remoteEndpoint = this.socket.RemoteEndPoint as IPEndPoint;
                return remoteEndpoint.Address;
            }
        }

#if DEBUG
        private readonly TimeSpan timeout = TimeSpan.FromDays(1);
#else
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
#endif

        private readonly Dictionary<string, string> hostKey;

        private uint outboundPacketSequence;

        private uint inboundPacketSequence;

        private uint outboundFlow;

        private uint inboundFlow;

        private Algorithms algorithms;

        private ExchangeContext exchangeContext;

        private readonly List<SshService> services = new List<SshService>();

        private readonly ConcurrentQueue<Message> blockedMessages = new ConcurrentQueue<Message>();

        private readonly EventWaitHandle hasBlockedMessagesWaitHandle = new ManualResetEvent(true);

        public string ServerVersion { get; }

        public string ClientVersion { get; private set; }

        public byte[] SessionId { get; private set; }

        public string Username => this.GetService<UserauthService>().Username;

        public EndPoint RemoteEndPoint => this.socket.RemoteEndPoint;

        public T GetService<T>() where T : SshService {
            return (T) this.services.FirstOrDefault(x => x is T);
        }

        static Session() {
            KeyExchangeAlgorithms.Add("diffie-hellman-group14-sha1", () => new DiffieHellmanGroupSha1(new DiffieHellman(2048)));
            KeyExchangeAlgorithms.Add("diffie-hellman-group1-sha1", () => new DiffieHellmanGroupSha1(new DiffieHellman(1024)));

            PublicKeyAlgorithms.Add("ssh-rsa", x => new RsaKey(x));
            PublicKeyAlgorithms.Add("ssh-dss", x => new DssKey(x));

            EncryptionAlgorithms.Add("aes128-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 128, CipherModeEx.Ctr));
            EncryptionAlgorithms.Add("aes192-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 192, CipherModeEx.Ctr));
            EncryptionAlgorithms.Add("aes256-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 256, CipherModeEx.Ctr));
            EncryptionAlgorithms.Add("aes128-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 128, CipherModeEx.Cbc));
            EncryptionAlgorithms.Add("3des-cbc", () => new CipherInfo(new TripleDESCryptoServiceProvider(), 192, CipherModeEx.Cbc));
            EncryptionAlgorithms.Add("aes192-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 192, CipherModeEx.Cbc));
            EncryptionAlgorithms.Add("aes256-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 256, CipherModeEx.Cbc));

            HmacAlgorithms.Add("hmac-md5", () => new HmacInfo(new HMACMD5(), 128));
            HmacAlgorithms.Add("hmac-sha1", () => new HmacInfo(new HMACSHA1(), 160));

            CompressionAlgorithms.Add("none", () => new NoCompression());

            messagesMetadata = (from t in typeof(Message).Assembly.GetTypes()
                                 let attrib = (MessageAttribute) t.GetCustomAttributes(typeof(MessageAttribute), false).FirstOrDefault()
                                 where attrib != null
                                 select new {attrib.Number, Type = t})
                    .ToDictionary(x => x.Number, x => x.Type);
        }

        public Session(Socket socket, Dictionary<string, string> hostKey) {
            Contract.Requires(socket != null);
            Contract.Requires(hostKey != null);

            this.socket = socket;
            this.hostKey = hostKey.ToDictionary(s => s.Key, s => s.Value);
            this.ServerVersion = "SSH-2.0-FxSsh";
        }

        public Session([NotNull] Socket socket,[NotNull] Dictionary<string, string> hostKey,
                       [NotNull] IReadOnlyCollection<AuthenticationMethod> authenticationMethods,
                       [NotNull] IClientKeyRepository clientKeyRepository) {

            this.socket = socket;
            this.hostKey = hostKey.ToDictionary(s => s.Key, s => s.Value);
            this.ServerVersion = "SSH-2.0-FxSsh";
            this.clientKeyRepository = clientKeyRepository;

            this.AuthenticationMethods = new Dictionary<AuthenticationMethod, bool>();
            foreach (var authenticationMethod in authenticationMethods) {
                this.AuthenticationMethods.Add(authenticationMethod, false);
            }
        }

        public event EventHandler<EventArgs> Disconnected;

        public event EventHandler<SshService> ServiceRegistered;

        internal void EstablishConnection() {
            this.SetSocketOptions();

            this.SocketWriteProtocolVersion();
            this.ClientVersion = this.SocketReadProtocolVersion();
            if (!Regex.IsMatch(this.ClientVersion, "SSH-2.0-.+")) {
                throw new SshConnectionException(
                        $"Not supported for client SSH version {this.ClientVersion}. This server only supports SSH v2.0.",
                        DisconnectReason.ProtocolVersionNotSupported);
            }

            this.ConsiderReExchange(true);

            try {
                while (this.socket != null && this.socket.Connected) {
                    var message = this.ReceiveMessage();
                    this.HandleMessageCore(message);
                }
            } finally {
                foreach (var service in this.services) {
                    service.CloseService();
                }
            }
        }

        public void Disconnect() {
            this.Disconnect(DisconnectReason.ByApplication, "Connection terminated by the server.");
        }

        public void Disconnect(DisconnectReason reason, string description) {
            if (reason == DisconnectReason.ByApplication) {
                var message = new DisconnectMessage(reason, description);
                this.TrySendMessage(message);
            }

            try {
                this.socket.Disconnect(true);
                this.socket.Dispose();
            } catch {
                // ignored
            }

            this.Disconnected?.Invoke(this, EventArgs.Empty);
        }

        #region Socket operations

        private void SetSocketOptions() {
            const int socketBufferSize = 2 * MaximumSshPacketSize;
            this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, socketBufferSize);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, socketBufferSize);
        }

        private string SocketReadProtocolVersion() {
            // http://tools.ietf.org/html/rfc4253#section-4.2
            var buffer = new byte[255];
            var dummy = new byte[255];
            var pos = 0;

            while (pos < buffer.Length) {
                var ar = this.socket.BeginReceive(buffer, pos, buffer.Length - pos, SocketFlags.Peek, null, null);
                this.WaitHandle(ar);
                var len = this.socket.EndReceive(ar ?? throw new InvalidOperationException());

                for (var i = 0; i < len; i++, pos++) {
                    if (pos <= 0 || buffer[pos - 1] != carriageReturn || buffer[pos] != lineFeed) continue;
                    this.socket.Receive(dummy, 0, i + 1, SocketFlags.None);
                    return Encoding.ASCII.GetString(buffer, 0, pos - 1);
                }
                this.socket.Receive(dummy, 0, len, SocketFlags.None);
            }
            throw new SshConnectionException("Could't read the protocal version", DisconnectReason.ProtocolError);
        }

        private void SocketWriteProtocolVersion() {
            this.SocketWrite(Encoding.ASCII.GetBytes(this.ServerVersion + "\r\n"));
        }

        private byte[] SocketRead(int length) {
            var pos = 0;
            var buffer = new byte[length];

            while (pos < length) {
                try {
                    var ar = this.socket.BeginReceive(buffer, pos, length - pos, SocketFlags.None, null, null);
                    this.WaitHandle(ar);
                    var len = this.socket.EndReceive(ar ?? throw new InvalidOperationException());
                    if (len == 0 && this.socket.Available == 0)
                        Thread.Sleep(50);

                    pos += len;
                } catch (SocketException exp) {
                    if (exp.SocketErrorCode == SocketError.WouldBlock ||
                        exp.SocketErrorCode == SocketError.IOPending ||
                        exp.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                        Thread.Sleep(30);
                    } else
                        throw new SshConnectionException("Connection lost", DisconnectReason.ConnectionLost);
                }
            }

            return buffer;
        }

        private void SocketWrite(byte[] data) {
            var pos = 0;
            var length = data.Length;

            while (pos < length) {
                try {
                    var ar = this.socket.BeginSend(data, pos, length - pos, SocketFlags.None, null, null);
                    this.WaitHandle(ar);
                    pos += this.socket.EndSend(ar ?? throw new InvalidOperationException());
                } catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                        Thread.Sleep(30);
                    } else
                        throw new SshConnectionException("Connection lost", DisconnectReason.ConnectionLost);
                }
            }
        }

        private void WaitHandle(IAsyncResult ar) {
            if (!ar.AsyncWaitHandle.WaitOne(this.timeout))
                throw new SshConnectionException($"Socket operation has timed out after {this.timeout.TotalMilliseconds:F0} milliseconds.",
                                                 DisconnectReason.ConnectionLost);
        }

        #endregion

        #region Message operations

        private Message ReceiveMessage() {
            var useAlg = this.algorithms != null;

            var blockSize = (byte) (useAlg ? Math.Max(8, this.algorithms.ClientEncryption.BlockBytesSize) : 8);
            var firstBlock = this.SocketRead(blockSize);
            if (useAlg)
                firstBlock = this.algorithms.ClientEncryption.Transform(firstBlock);

            var packetLength = firstBlock[0] << 24 | firstBlock[1] << 16 | firstBlock[2] << 8 | firstBlock[3];
            var paddingLength = firstBlock[4];
            var bytesToRead = packetLength - blockSize + 4;

            var followingBlocks = this.SocketRead(bytesToRead);
            if (useAlg)
                followingBlocks = this.algorithms.ClientEncryption.Transform(followingBlocks);

            var fullPacket = firstBlock.Concat(followingBlocks).ToArray();
            var data = fullPacket.Skip(5).Take(packetLength - paddingLength).ToArray();
            if (useAlg) {
                var clientMac = this.SocketRead(this.algorithms.ClientHmac.DigestLength);
                var mac = this.ComputeHmac(this.algorithms.ClientHmac, fullPacket, this.inboundPacketSequence);
                if (!clientMac.SequenceEqual(mac)) {
                    throw new SshConnectionException("Invalid MAC", DisconnectReason.MacError);
                }

                data = this.algorithms.ClientCompression.Decompress(data);
            }

            var typeNumber = data[0];
            var implemented = messagesMetadata.ContainsKey(typeNumber);
            var message = implemented
                                  ? (Message) Activator.CreateInstance(messagesMetadata[typeNumber])
                                  : new UnimplementedMessage {SequenceNumber = this.inboundPacketSequence, UnimplementedMessageType = typeNumber};

            if (implemented)
                message.Load(data);

            lock (this.locker) {
                this.inboundPacketSequence++;
                this.inboundFlow += (uint) packetLength;
            }

            this.ConsiderReExchange();

            return message;
        }

        internal void SendMessage(Message message) {
            Contract.Requires(message != null);

            if (this.exchangeContext != null
                && message.MessageType > 4 && (message.MessageType < 20 || message.MessageType > 49)) {
                this.blockedMessages.Enqueue(message);
                return;
            }

            this.hasBlockedMessagesWaitHandle.WaitOne();
            this.SendMessageInternal(message);
        }

        private void SendMessageInternal(Message message) {
            var useAlg = this.algorithms != null;

            var blockSize = (byte) (useAlg ? Math.Max(8, this.algorithms.ServerEncryption.BlockBytesSize) : 8);
            var payload = message.GetPacket();
            if (useAlg)
                payload = this.algorithms.ServerCompression.Compress(payload);

            // http://tools.ietf.org/html/rfc4253
            // 6.  Binary Packet Protocol
            // the total length of (packet_length || padding_length || payload || padding)
            // is a multiple of the cipher block size or 8,
            // padding length must between 4 and 255 bytes.
            var paddingLength = (byte) (blockSize - (payload.Length + 5) % blockSize);
            if (paddingLength < 4)
                paddingLength += blockSize;

            var packetLength = (uint) payload.Length + paddingLength + 1;

            var padding = new byte[paddingLength];
            rng.GetBytes(padding);

            using (var worker = new SshDataWorker()) {
                worker.Write(packetLength);
                worker.Write(paddingLength);
                worker.Write(payload);
                worker.Write(padding);

                payload = worker.ToByteArray();
            }

            if (useAlg) {
                var mac = this.ComputeHmac(this.algorithms.ServerHmac, payload, this.outboundPacketSequence);
                payload = this.algorithms.ServerEncryption.Transform(payload).Concat(mac).ToArray();
            }

            this.SocketWrite(payload);

            lock (this.locker) {
                this.outboundPacketSequence++;
                this.outboundFlow += packetLength;
            }

            this.ConsiderReExchange();
        }

        private void ConsiderReExchange(bool force = false) {
            var kex = false;
            lock (this.locker)
                if (this.exchangeContext == null
                    && (force || this.inboundFlow + this.outboundFlow > 1024 * 1024 * 512)) // 0.5 GiB
                {
                    this.exchangeContext = new ExchangeContext();
                    kex = true;
                }

            if (kex) {
                var kexInitMessage = this.LoadKexInitMessage();
                this.exchangeContext.ServerKexInitPayload = kexInitMessage.GetPacket();

                this.SendMessage(kexInitMessage);
            }
        }

        private void ContinueSendBlockedMessages() {
            if (this.blockedMessages.Count <= 0) return;
            while (this.blockedMessages.TryDequeue(out var message)) {
                this.SendMessageInternal(message);
            }
        }

        internal bool TrySendMessage(Message message) {
            Contract.Requires(message != null);

            try {
                this.SendMessage(message);
                return true;
            } catch {
                return false;
            }
        }

        internal Message LoadKexInitMessage() {
            var message = new KeyExchangeInitMessage {
                KeyExchangeAlgorithms = KeyExchangeAlgorithms.Keys.ToArray(),
                ServerHostKeyAlgorithms = PublicKeyAlgorithms.Keys.ToArray(),
                EncryptionAlgorithmsClientToServer = EncryptionAlgorithms.Keys.ToArray(),
                EncryptionAlgorithmsServerToClient = EncryptionAlgorithms.Keys.ToArray(),
                MacAlgorithmsClientToServer = HmacAlgorithms.Keys.ToArray(),
                MacAlgorithmsServerToClient = HmacAlgorithms.Keys.ToArray(),
                CompressionAlgorithmsClientToServer = CompressionAlgorithms.Keys.ToArray(),
                CompressionAlgorithmsServerToClient = CompressionAlgorithms.Keys.ToArray(),
                LanguagesClientToServer = new[] {""},
                LanguagesServerToClient = new[] {""},
                FirstKexPacketFollows = false,
                Reserved = 0
            };

            return message;
        }

        #endregion

        #region Handle messages

        private void HandleMessageCore(Message message) {
            typeof(Session)
                    .GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {message.GetType()}, null)?
                    .Invoke(this, new object[] {message});
        }

        private void HandleMessage(ChannelOpenConfirmationMessage message) {
            
        }

        private void HandleMessage(GlobalRequestMessage message) {

            var service = this.GetService<ConnectionService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(ChannelDataMessage message) {
            var service = this.GetService<ConnectionService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(ChannelRequestMessage message) {
            var service = this.GetService<ConnectionService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(ChannelOpenMessage message) {
            var service = this.GetService<ConnectionService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(IgnoreMessage message) {
        }

        private void HandleMessage(RequestMessage message) {
            var service = this.GetService<UserauthService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(DisconnectMessage message) {
            this.Disconnect(message.ReasonCode, message.Description);
        }

        private void HandleMessage(KeyExchangeInitMessage message) {
            this.ConsiderReExchange(true);

            this.exchangeContext.KeyExchange = this.ChooseAlgorithm(KeyExchangeAlgorithms.Keys.ToArray(), message.KeyExchangeAlgorithms);
            this.exchangeContext.PublicKey = this.ChooseAlgorithm(PublicKeyAlgorithms.Keys.ToArray(), message.ServerHostKeyAlgorithms);
            this.exchangeContext.ClientEncryption = this.ChooseAlgorithm(EncryptionAlgorithms.Keys.ToArray(), message.EncryptionAlgorithmsClientToServer);
            this.exchangeContext.ServerEncryption = this.ChooseAlgorithm(EncryptionAlgorithms.Keys.ToArray(), message.EncryptionAlgorithmsServerToClient);
            this.exchangeContext.ClientHmac = this.ChooseAlgorithm(HmacAlgorithms.Keys.ToArray(), message.MacAlgorithmsClientToServer);
            this.exchangeContext.ServerHmac = this.ChooseAlgorithm(HmacAlgorithms.Keys.ToArray(), message.MacAlgorithmsServerToClient);
            this.exchangeContext.ClientCompression = this.ChooseAlgorithm(CompressionAlgorithms.Keys.ToArray(), message.CompressionAlgorithmsClientToServer);
            this.exchangeContext.ServerCompression = this.ChooseAlgorithm(CompressionAlgorithms.Keys.ToArray(), message.CompressionAlgorithmsServerToClient);

            this.exchangeContext.ClientKexInitPayload = message.GetPacket();
        }

        private void HandleMessage(KeyExchangeDhInitMessage message) {
            var kexAlg = KeyExchangeAlgorithms[this.exchangeContext.KeyExchange]();
            var hostKeyAlg = PublicKeyAlgorithms[this.exchangeContext.PublicKey](this.hostKey[this.exchangeContext.PublicKey]);
            var clientCipher = EncryptionAlgorithms[this.exchangeContext.ClientEncryption]();
            var serverCipher = EncryptionAlgorithms[this.exchangeContext.ServerEncryption]();
            var serverHmac = HmacAlgorithms[this.exchangeContext.ServerHmac]();
            var clientHmac = HmacAlgorithms[this.exchangeContext.ClientHmac]();

            var clientExchangeValue = message.E;
            var serverExchangeValue = kexAlg.CreateKeyExchange();
            var sharedSecret = kexAlg.DecryptKeyExchange(clientExchangeValue);
            var hostKeyAndCerts = hostKeyAlg.CreateKeyAndCertificatesData();
            var exchangeHash = this.ComputeExchangeHash(kexAlg, hostKeyAndCerts, clientExchangeValue, serverExchangeValue, sharedSecret);

            if (this.SessionId == null)
                this.SessionId = exchangeHash;

            var clientCipherIv = this.ComputeEncryptionKey(kexAlg, exchangeHash, clientCipher.BlockSize >> 3, sharedSecret, 'A');
            var serverCipherIv = this.ComputeEncryptionKey(kexAlg, exchangeHash, serverCipher.BlockSize >> 3, sharedSecret, 'B');
            var clientCipherKey = this.ComputeEncryptionKey(kexAlg, exchangeHash, clientCipher.KeySize >> 3, sharedSecret, 'C');
            var serverCipherKey = this.ComputeEncryptionKey(kexAlg, exchangeHash, serverCipher.KeySize >> 3, sharedSecret, 'D');
            var clientHmacKey = this.ComputeEncryptionKey(kexAlg, exchangeHash, clientHmac.KeySize >> 3, sharedSecret, 'E');
            var serverHmacKey = this.ComputeEncryptionKey(kexAlg, exchangeHash, serverHmac.KeySize >> 3, sharedSecret, 'F');

            this.exchangeContext.NewAlgorithms = new Algorithms {
                KeyExchange = kexAlg,
                PublicKey = hostKeyAlg,
                ClientEncryption = clientCipher.Cipher(clientCipherKey, clientCipherIv, false),
                ServerEncryption = serverCipher.Cipher(serverCipherKey, serverCipherIv, true),
                ClientHmac = clientHmac.Hmac(clientHmacKey),
                ServerHmac = serverHmac.Hmac(serverHmacKey),
                ClientCompression = CompressionAlgorithms[this.exchangeContext.ClientCompression](),
                ServerCompression = CompressionAlgorithms[this.exchangeContext.ServerCompression](),
            };

            var reply = new KeyExchangeDhReplyMessage {
                HostKey = hostKeyAndCerts,
                F = serverExchangeValue,
                Signature = hostKeyAlg.CreateSignatureData(exchangeHash),
            };

            this.SendMessage(reply);
            this.SendMessage(new NewKeysMessage());
        }

        private void HandleMessage(NewKeysMessage message) {
            this.hasBlockedMessagesWaitHandle.Reset();

            lock (this.locker) {
                this.inboundFlow = 0;
                this.outboundFlow = 0;
                this.algorithms = this.exchangeContext.NewAlgorithms;
                this.exchangeContext = null;
            }

            this.ContinueSendBlockedMessages();
            this.hasBlockedMessagesWaitHandle.Set();
        }

        private void HandleMessage(Message message) {
            this.SendMessage(message);
        }

        private void HandleMessage(ServiceRequestMessage message) {
            var service = this.RegisterService(message.ServiceName);
            if (service == null)
                throw new SshConnectionException($"Service \"{message.ServiceName}\" not available.",
                                                 DisconnectReason.ServiceNotAvailable);
            this.SendMessage(new ServiceAcceptMessage(message.ServiceName));
        }

        private void HandleMessage(UserauthServiceMessage message) {
            var service = this.GetService<UserauthService>();
            service?.HandleMessageCore(message);
        }

        private void HandleMessage(ConnectionServiceMessage message) {
            var service = this.GetService<ConnectionService>();
            service?.HandleMessageCore(message);
        }

        #endregion

        public void StartReverseConnection() {
            var connectionService = this.GetService<ConnectionService>();

            var channel = connectionService.AddChannel();

            string clientAddress = ((IPEndPoint)this.RemoteEndPoint).Address.MapToIPv4().ToString();
            uint clientPort = (uint)((IPEndPoint)this.RemoteEndPoint).Port;

            this.SendMessage(new ForwardedTcpipMessage(channel.ServerChannelId, channel.ClientInitialWindowSize,
                                                               channel.ClientMaxPacketSize, connectionService.ForwardAddress,
                                                               connectionService.ForwardPort, clientAddress, clientPort));
        }

        private string ChooseAlgorithm(string[] serverAlgorithms, string[] clientAlgorithms) {
            foreach (var client in clientAlgorithms)
                foreach (var server in serverAlgorithms)
                    if (client == server)
                        return client;

            throw new SshConnectionException("Failed to negotiate algorithm.", DisconnectReason.KeyExchangeFailed);
        }

        private byte[] ComputeExchangeHash(KexAlgorithm kexAlg, byte[] hostKeyAndCerts, byte[] clientExchangeValue, byte[] serverExchangeValue, byte[] sharedSecret) {
            using (var worker = new SshDataWorker()) {
                worker.Write(this.ClientVersion, Encoding.ASCII);
                worker.Write(this.ServerVersion, Encoding.ASCII);
                worker.WriteBinary(this.exchangeContext.ClientKexInitPayload);
                worker.WriteBinary(this.exchangeContext.ServerKexInitPayload);
                worker.WriteBinary(hostKeyAndCerts);
                worker.WriteMpint(clientExchangeValue);
                worker.WriteMpint(serverExchangeValue);
                worker.WriteMpint(sharedSecret);

                return kexAlg.ComputeHash(worker.ToByteArray());
            }
        }

        private byte[] ComputeEncryptionKey(KexAlgorithm kexAlg, byte[] exchangeHash, int blockSize, byte[] sharedSecret, char letter) {
            var keyBuffer = new byte[blockSize];
            var keyBufferIndex = 0;
            byte[] currentHash = null;

            while (keyBufferIndex < blockSize) {
                using (var worker = new SshDataWorker()) {
                    worker.WriteMpint(sharedSecret);
                    worker.Write(exchangeHash);

                    if (currentHash == null) {
                        worker.Write((byte) letter);
                        worker.Write(this.SessionId);
                    } else {
                        worker.Write(currentHash);
                    }

                    currentHash = kexAlg.ComputeHash(worker.ToByteArray());
                }

                var currentHashLength = Math.Min(currentHash.Length, blockSize - keyBufferIndex);
                Array.Copy(currentHash, 0, keyBuffer, keyBufferIndex, currentHashLength);

                keyBufferIndex += currentHashLength;
            }

            return keyBuffer;
        }

        private byte[] ComputeHmac(HmacAlgorithm alg, byte[] payload, uint seq) {
            using (var worker = new SshDataWorker()) {
                worker.Write(seq);
                worker.Write(payload);

                return alg.ComputeHash(worker.ToByteArray());
            }
        }

        internal SshService RegisterService(string serviceName, UserauthArgs auth = null) {
            Contract.Requires(serviceName != null);

            SshService service = null;
            switch (serviceName) {
                case "ssh-userauth":
                    if (this.GetService<UserauthService>() == null)
                        service = new UserauthService(this);
                    break;
                case "ssh-connection":
                    if (auth != null && this.GetService<ConnectionService>() == null)
                        service = new ConnectionService(this, auth);
                    break;
            }
            if (service == null) return null;
            this.ServiceRegistered?.Invoke(this, service);

            this.services.Add(service);
            return service;
        }

        private class Algorithms {
            public KexAlgorithm KeyExchange;

            public PublicKeyAlgorithm PublicKey;

            public EncryptionAlgorithm ClientEncryption;

            public EncryptionAlgorithm ServerEncryption;

            public HmacAlgorithm ClientHmac;

            public HmacAlgorithm ServerHmac;

            public CompressionAlgorithm ClientCompression;

            public CompressionAlgorithm ServerCompression;
        }

        private class ExchangeContext {
            public string KeyExchange;

            public string PublicKey;

            public string ClientEncryption;

            public string ServerEncryption;

            public string ClientHmac;

            public string ServerHmac;

            public string ClientCompression;

            public string ServerCompression;

            public byte[] ClientKexInitPayload;

            public byte[] ServerKexInitPayload;

            public Algorithms NewAlgorithms;
        }
    }
}