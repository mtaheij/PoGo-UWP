using System.Threading.Tasks;
using Google.Protobuf;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public class Player : BaseRpc
    {
        public Player(Client client) : base(client)
        {
            Client = client;
        }

        public void UpdatePlayerLocation(double latitude, double longitude, double accuracy)
        {
            SetCoordinates(latitude, longitude, accuracy);
        }

        internal void SetCoordinates(double lat, double lng, double accuracy)
        {
            Client.CurrentLatitude = lat;
            Client.CurrentLongitude = lng;
            Client.CurrentAccuracy = accuracy;
        }

        public async Task<GetPlayerResponse> GetPlayer()
        {
            return await PostProtoPayload<Request, GetPlayerResponse>(RequestType.GetPlayer, new GetPlayerMessage());
        }

        public async Task<GetPlayerProfileResponse> GetPlayerProfile(string playerName)
        {
            return
                await
                    PostProtoPayload<Request, GetPlayerProfileResponse>(RequestType.GetPlayerProfile,
                        new GetPlayerProfileMessage
                        {
                            PlayerName = playerName
                        });
        }

        public async Task<CheckAwardedBadgesResponse> GetNewlyAwardedBadges()
        {
            return
                await
                    PostProtoPayload<Request, CheckAwardedBadgesResponse>(RequestType.CheckAwardedBadges,
                        new CheckAwardedBadgesMessage());
        }

        public async Task<CollectDailyBonusResponse> CollectDailyBonus()
        {
            return
                await
                    PostProtoPayload<Request, CollectDailyBonusResponse>(RequestType.CollectDailyBonus,
                        new CollectDailyBonusMessage());
        }

        public async Task<CollectDailyDefenderBonusResponse> CollectDailyDefenderBonus()
        {
            return
                await
                    PostProtoPayload<Request, CollectDailyDefenderBonusResponse>(RequestType.CollectDailyDefenderBonus,
                        new CollectDailyDefenderBonusMessage());
        }

        public async Task<EquipBadgeResponse> EquipBadge(BadgeType type)
        {
            return
                await
                    PostProtoPayload<Request, EquipBadgeResponse>(RequestType.EquipBadge,
                        new EquipBadgeMessage {BadgeType = type});
        }

        public async Task<LevelUpRewardsResponse> GetLevelUpRewards(int level)
        {
            return
                await
                    PostProtoPayload<Request, LevelUpRewardsResponse>(RequestType.LevelUpRewards,
                        new LevelUpRewardsMessage
                        {
                            Level = level
                        });
        }

        public async Task<SetAvatarResponse> SetAvatar(PlayerAvatar playerAvatar)
        {
            return await PostProtoPayload<Request, SetAvatarResponse>(RequestType.SetAvatar, new SetAvatarMessage
            {
                PlayerAvatar = playerAvatar
            });
        }

        public async Task<SetContactSettingsResponse> SetContactSetting(ContactSettings contactSettings)
        {
            return
                await
                    PostProtoPayload<Request, SetContactSettingsResponse>(RequestType.SetContactSettings,
                        new SetContactSettingsMessage
                        {
                            ContactSettings = contactSettings
                        });
        }

        public async Task<SetPlayerTeamResponse> SetPlayerTeam(TeamColor teamColor)
        {
            return 
                await
                    PostProtoPayload<Request, SetPlayerTeamResponse>(RequestType.SetPlayerTeam, new SetPlayerTeamMessage
                    {
                        Team = teamColor
                    });
        }

        public async Task<SetBuddyPokemonResponse> SetBuddyPokemon(ulong id)
        {
            return await PostProtoPayload<Request, SetBuddyPokemonResponse>(RequestType.SetBuddyPokemon, new SetBuddyPokemonMessage
            {
                PokemonId = id
            });
        }

        public async Task<GetBuddyWalkedResponse> GetBuddyWalked()
        {
            return await PostProtoPayload<Request, GetBuddyWalkedResponse>(RequestType.GetBuddyWalked, new GetBuddyWalkedMessage
            { });
        }

        public async Task<MarkTutorialCompleteResponse> MarkTutorialComplete(TutorialState[] completed_tutorials, bool send_marketing_emails, bool send_push_notifications)
        {
            return await PostProtoPayload<Request, MarkTutorialCompleteResponse>(RequestType.MarkTutorialComplete, new MarkTutorialCompleteMessage
            {
                TutorialsCompleted = { completed_tutorials },
                SendMarketingEmails = send_marketing_emails,
                SendPushNotifications = send_push_notifications
            });
        }
    }
}