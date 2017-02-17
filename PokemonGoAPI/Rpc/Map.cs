using System;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public class Map : BaseRpc
    {
        Client _client;
        public Map(Client client) : base(client)
        {
            _client = client;
        }

        public async
            Task
                <
                    Tuple
                        <GetMapObjectsResponse, CheckChallengeResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse,
                            DownloadSettingsResponse>> GetMapObjects()
        {
            #region Messages

            var getMapObjectsMessage = new GetMapObjectsMessage
            {
                CellId = {S2Helper.GetNearbyCellIds(Client.CurrentLongitude, Client.CurrentLatitude)},
                SinceTimestampMs = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                Latitude = Client.CurrentLatitude,
                Longitude = Client.CurrentLongitude
            };
            var checkChallengeMessage = new CheckChallengeMessage();
            var getHatchedEggsMessage = new GetHatchedEggsMessage();
            var getInventoryMessage = new GetInventoryMessage
            {
                LastTimestampMs = DateTime.UtcNow.ToUnixTime() // Should be timestamp of last inventory response, no?
            };
            var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = _client.Download.DownloadSettingsHash
            };

            #endregion

            var request = await RequestBuilder.GetRequestEnvelopeNecro(
                new Request
                {
                    RequestType = RequestType.GetMapObjects,
                    RequestMessage = getMapObjectsMessage.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.CheckChallenge,
                    RequestMessage = checkChallengeMessage.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.GetHatchedEggs,
                    RequestMessage = getHatchedEggsMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = getInventoryMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.CheckAwardedBadges,
                    RequestMessage = checkAwardedBadgesMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });

            var response = await PostProtoPayload
                        <Request, GetMapObjectsResponse, CheckChallengeResponse, GetHatchedEggsResponse, GetInventoryResponse,
                            CheckAwardedBadgesResponse, DownloadSettingsResponse>(request);


            /* Temporary inserted here from 2.0 - there is better mechanism */
            _client.ProcessMessages<GetMapObjectsResponse, CheckChallengeResponse, GetHatchedEggsResponse, GetInventoryResponse,
                            CheckAwardedBadgesResponse, DownloadSettingsResponse>(response);

            _client.Download.DownloadSettingsHash = response?.Item6?.Hash ?? "";

            return response;
        }

        public async Task<GetIncensePokemonResponse> GetIncensePokemons()
        {
            var message = new GetIncensePokemonMessage
            {
                PlayerLatitude = Client.CurrentLatitude,
                PlayerLongitude = Client.CurrentLongitude
            };

            return await PostProtoPayload<Request, GetIncensePokemonResponse>(RequestType.GetIncensePokemon, message);
        }
    }
}