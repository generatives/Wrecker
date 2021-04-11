using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct NetworkedEntity
    {
        public Guid Id { get; set; }

        public static NetworkedEntity NewNetworkedEntity()
        {
            return new NetworkedEntity()
            {
                Id = Guid.NewGuid()
            };
        }
    }
}
