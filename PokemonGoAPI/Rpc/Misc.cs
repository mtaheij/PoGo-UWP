using System.Threading.Tasks;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public class Misc : BaseRpc
    {
        public Misc(Client client) : base(client)
        {
        }

        public async Task<ClaimCodenameResponse> ClaimCodename(string codename)
        {
            return
                await
                    PostProtoPayload<Request, ClaimCodenameResponse>(RequestType.ClaimCodename,
                        new ClaimCodenameMessage
                        {
                            Codename = codename
                        });
        }

        public async Task<EchoResponse> SendEcho()
        {
            return await PostProtoPayload<Request, EchoResponse>(RequestType.Echo, new EchoMessage());
        }

        public async Task<CheckChallengeResponse> CheckChallenge()
        {
            return await PostProtoPayload<Request, CheckChallengeResponse>(RequestType.CheckChallenge, new CheckChallengeMessage());
        }

        public async Task<VerifyChallengeResponse> VerifyChallenge(string token)
        {
            return await PostProtoPayload<Request, VerifyChallengeResponse>(RequestType.VerifyChallenge,
                new VerifyChallengeMessage
                {
                    Token = token
                });

        }

        public async Task<SfidaActionLogResponse> GetSfidaActionLog()
        {
            return await PostProtoPayload<Request, SfidaActionLogResponse>(RequestType.SfidaActionLog, new SfidaActionLogMessage());
        }
    }
}
