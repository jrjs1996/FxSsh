using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace FxSsh
{
    public interface IClientKeyRepository { 
        [NotNull] byte[] GetKeyForClient([NotNull] string clientName);
    }
}
