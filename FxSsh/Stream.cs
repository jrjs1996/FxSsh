using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh
{
    public class Stream {
        private Session session;

        public Stream(Session session) {
            this.session = session;
        }

        ~Stream() {
            throw new NotImplementedException();
        }
    }
}
