using System.Diagnostics.Contracts;

namespace FxSsh.Services {
    public class SessionRequestedArgs {
        public SessionRequestedArgs(SessionChannel channel, string command, UserauthArgs userauthArgs) {
            Contract.Requires(channel != null);
            Contract.Requires(command != null);
            Contract.Requires(userauthArgs != null);

            this.Channel = channel;
            this.CommandText = command;
            this.AttachedUserauthArgs = userauthArgs;
        }

        public SessionChannel Channel { get; }

        public string CommandText { get; }

        public UserauthArgs AttachedUserauthArgs { get; }
    }
}