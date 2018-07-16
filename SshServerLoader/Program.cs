using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FxSsh;
using FxSsh.Services;
using FxSsh.Exceptions;

namespace SshServerLoader {
    class Program {
        static void Main() {
            var server = new SshServer(new IPEndPoint(IPAddress.Parse("169.254.73.253"), 22));
            var authenticationMethods = new List<FxSsh.AuthenticationMethod>();
            server.SetClientKeyRepository(new ClientKeyRepository());
            authenticationMethods.Add(AuthenticationMethod.PublicKey);
            // server.AuthenticationMethods = authenticationMethods;
            // Called when a client has connected to the server.
            server.ConnectionAccepted += ServerConnectionAccepted;

            server.Start();
            while (true) {
                var input = Console.ReadLine().Split(" ");
                var command = input[0];
                switch (command)
                {
                    case "Connect":
                        if (input.Length < 2) return;
                        Connect(server, input[1]);
                        break;
                    case "GetConnectedClients":
                        GetConnectedClients(server);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void GetConnectedClients(SshServer server) {
            var clients = server.GetConnectedClients();
            foreach (var c in clients) {
                Console.WriteLine(c.Name);
                var connections = c.Connections;
                foreach (var connection in connections) {
                    Console.WriteLine("---> " + connection.WhenConnected);
                }
            }
        }

        private static void Connect(SshServer server, string client) {
            try {
                using (Stream clientStream = server.Connect(client, 8000))
                {
                    /* Simple HTTP request. Remove this and put your own
                     operations here. */

                    Console.WriteLine("Connected");
                    string message = "GET  / HTTP/1.1\r\n" +
                                     "User - Agent: Server\r\n" +
                                     "Host: 169.254.73.20:8000\r\n\r\n";
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(message);
                    clientStream.Write(sendBuffer, 0, sendBuffer.Length);
                    byte[] headerBuffer = new byte[400];
                    clientStream.Read(headerBuffer, 0, 300);
                    Console.WriteLine(Encoding.UTF8.GetString(headerBuffer));
                    byte[] bodyBuffer = new byte[100];
                    clientStream.Read(bodyBuffer, 0, 100);
                    Console.WriteLine(Encoding.UTF8.GetString(bodyBuffer));
                    GetConnectedClients(server);
                }
            } catch (SshClientNotFoundException ex) {
                Console.WriteLine("Error the client was not found in the list of connected clients.");
            }

        }

        /* Callbacks for various points in an HTTP connection. These can be ignored or removed for now.
           They can be usefull so I'll write some information about them. */

        private static void ServerConnectionAccepted(object sender, Session e) {
            Console.WriteLine("Accepted a client.");

            e.ServiceRegistered += EServiceRegistered;
        }

        private static void EServiceRegistered(object sender, SshService e) {
            var session = (Session) sender;
            Console.WriteLine("Session {0} requesting {1}.",
                              BitConverter.ToString(session.SessionId).Replace("-", ""), e.GetType().Name);

            switch (e) {
                case UserauthService _: {
                    var service = (UserauthService) e;
                    service.Userauth += ServiceUserauth;
                    break;
                }
                case ConnectionService _: {
                    var service = (ConnectionService) e;
                    service.CommandOpened += ServiceCommandOpened;
                    break;
                }
            }
        }

        static void ServiceUserauth(object sender, UserauthArgs e) {
            Console.WriteLine("Client {0} fingerprint: {1}.", e.KeyAlgorithm, e.Fingerprint);
            e.Result = true;
        }

        static void ServiceCommandOpened(object sender, SessionRequestedArgs e) {
            Console.WriteLine("Channel {0} runs command: \"{1}\".", e.Channel.ServerChannelId, e.CommandText);
            //e.Channel.SendData(Encoding.ASCII.GetBytes(".]0;root@server: ~.root@server:~# "));
            e.Channel.DataReceived += ServiceDataReceived;
        }

        static void ServiceDataReceived(object sender, MessageReceivedArgs e) {
            
        }
    }

    public class ClientKeyRepository : IClientKeyRepository {
        public byte[] GetKeyForClient(string clientName) {
            if (clientName == "root") {
                return Encoding.ASCII.GetBytes("AAAAB3NzaC1yc2EAAAADAQABAAABAQDTzwJR525IP1ihCmtYVD8+nveIFCGrGUD6JIBHd31c7EnoxE5gXWhgUA0+GblaxvU0djLqCY+CyIezIBtrKnPbHfPfVfACttgLNCzQe4d6pxbdqg52lqKMqBTSvxkOivadbFGwbmWnaXbofYQYjLXWHicV/oX0of7pFiHPbx8mzXP3sll6thlihzQKuMa1POH8aNtVZKOAIHlEFHrJ3Wm5N184Jdem2mgoRirw09UPckEHbCNwnuGwarqn/jzEoGT8h12HkuMVKJrAEpjMSaCExRMH8jIwTJwRh3fj2Xs4sPlH1FfOIKSzH9nejI5CShZaVaZyrUGpK14GK2x2z8P9");
            } else return null;
        }
    }
}