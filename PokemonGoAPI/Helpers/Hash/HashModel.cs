using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGoAPI.Helpers.Hash
{
    public class HashResponseContent
    {
        public uint LocationAuthHash { get; set; }
        public uint LocationHash { get; set; }

        // Note: These are actually "unsigned" values. They are sent as signed values simply due to JSON format specifications. 
        //       You should re-cast these to unsigned variants (or leave them as-is in their byte form)
        public List<long> RequestHashes { get; set; }
    }

    public class HashRequestContent
    {
        /// <summary>
        ///     The timestamp for the packet being sent to Niantic. This much match what you use in the SignalLog and RpcRequest
        ///     protos! (EpochTimestampMS)
        /// </summary>
        public ulong Timestamp { get; set; }

        /// <summary>
        /// The Latitude field from your ClientRpc request envelope. (The one you will be sending to Niantic)
        /// For safety reasons, this should also match your last LocationUpdate entry in the SignalLog
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// The Longitude field from your ClientRpc request envelope. (The one you will be sending to Niantic)
        /// For safety reasons, this should also match your last LocationUpdate entry in the SignalLog
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// The Altitude field from your ClientRpc request envelope. (The one you will be sending to Niantic)
        /// For safety reasons, this should also match your last LocationUpdate entry in the SignalLog
        /// </summary>
        public double Altitude { get; set; }

        /// <summary>
        ///     The Niantic-specific auth ticket data.
        /// </summary>
        public byte[] AuthTicket { get; set; }

        /// <summary>
        ///     Also known as the "replay check" field. (Field 22 in SignalLog)
        /// </summary>
        public byte[] SessionData { get; set; }

        /// <summary>
        ///     A collection of the request data to be hashed.
        /// </summary>
        public List<byte[]> Requests { get; set; } = new List<byte[]>();
    }
}
