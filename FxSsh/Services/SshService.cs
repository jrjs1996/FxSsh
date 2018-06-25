using System.Diagnostics.Contracts;

namespace FxSsh.Services {
    public abstract class SshService {
        protected internal readonly Session Session;

        protected SshService(Session session) {
            Contract.Requires(session != null);

            this.Session = session;
        }

        protected internal abstract void CloseService();
    }
}