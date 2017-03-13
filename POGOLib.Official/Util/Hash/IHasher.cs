using POGOProtos.Networking.Envelopes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POGOLib.Official.Util.Hash
{
    public interface IHasher
    {

        /// <summary>
        /// The PokémonVersion this <see cref="IHasher"/> is made for.
        /// Please use API versioning of PokemonGo only (https://pgorelease.nianticlabs.com/plfe/version).
        /// </summary>
        Version PokemonVersion { get; }

        long Unknown25 { get; }

        Task<HashData> GetHashDataAsync(RequestEnvelope requestEnvelope, Signature signature, byte[] locationBytes, byte[][] requestsBytes, byte[] serializedTicket);

        byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs);

    }
}
