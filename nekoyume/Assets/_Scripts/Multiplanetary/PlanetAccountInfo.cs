#nullable enable

using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.GraphQL.GraphTypes;

namespace Nekoyume.Multiplanetary
{
    public class PlanetAccountInfo
    {
        public readonly PlanetId PlanetId;

        public Address? AgentAddress { get; private set; }
        public bool? IsAgentPledged { get; private set; }
        public IEnumerable<AvatarGraphType> AvatarGraphTypes => _avatarGraphTypes;

        private readonly AvatarGraphType[] _avatarGraphTypes;

        public PlanetAccountInfo(
            PlanetId planetId,
            Address? agentAddress,
            bool? isAgentPledged,
            params AvatarGraphType[] avatarGraphTypes)
        {
            PlanetId = planetId;
            AgentAddress = agentAddress;
            IsAgentPledged = isAgentPledged;
            _avatarGraphTypes = avatarGraphTypes;
        }
    }
}
