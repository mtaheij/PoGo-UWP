using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.ViewModels;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings;
using POGOProtos.Settings.Master;
using Q42.WinRT.Data;
using Template10.Common;
using Template10.Utils;
using Windows.Devices.Sensors;
using Newtonsoft.Json;
using PokemonGo_UWP.Utils.Helpers;
using System.Collections.Specialized;
using Windows.UI.Popups;
using System.ComponentModel;
using PokemonGo_UWP.Views;
using POGOLib.Official.Util.Hash;
using Google.Protobuf.Collections;
using POGOLib.Official.Net;
using POGOLib.Official;
using POGOLib.Official.Extensions;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Pokemon;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using Google.Protobuf;
using POGOLib.Official.Logging;
using PokemonGo_UWP.Utils.Settings;
using PokemonGo_UWP.Enums;
using POGOLib.Official.Net.Authentication;
using POGOLib.Official.Net.Authentication.Data;
using Template10.Services.NavigationService;
using POGOProtos.Data.Battle;
using PokemonGo_UWP.Exceptions;

namespace PokemonGo_UWP.Utils
{
    /// <summary>
    ///     Static class containing game's state and wrapped client methods to update data
    /// </summary>
    public static class GameClient
    {
        #region Client Vars

        private static ISettings _clientSettings;

        private static Session _session;

        public static bool IsInitialized { get; private set; }
        public static bool LoggedIn { get; set; }

        /// <summary>
        /// Handles updates for applied items.
        /// </summary>
        private class AppliedItemsHeartbeat
        {
            /// <summary>
            ///     Determines whether we can keep heartbeating.
            /// </summary>
            private bool _keepHeartbeating = true;

            /// <summary>
            /// Timer used to update applied item
            /// </summary>
            private DispatcherTimer _appliedItemUpdateTimer;

            /// <summary>
            /// Inits heartbeat
            /// </summary>
            internal async Task StartDispatcher()
            {
                _keepHeartbeating = true;
                if (_appliedItemUpdateTimer == null)
                {
                    await DispatcherHelper.RunInDispatcherAndAwait((Action)(() =>
                    {
                        _appliedItemUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                        _appliedItemUpdateTimer.Tick += this._appliedItemUpdateTimer_Tick;
                        _appliedItemUpdateTimer.Start();
                    }));
                }
            }

            private void _appliedItemUpdateTimer_Tick(object sender, object e)
            {
                Task.Run(async () =>
                    await DispatcherHelper.RunInDispatcherAndAwait((Action)(() =>
                    {
                        foreach (AppliedItemWrapper appliedItem in AppliedItems)
                        {
                            if (appliedItem.IsExpired)
                            {
                                AppliedItems.Remove(appliedItem);
                                break;
                            }
                            appliedItem.Update(appliedItem.WrappedData);
                        }
                    })));
            }

            /// <summary>
            /// Stops heartbeat
            /// </summary>
            internal void StopDispatcher()
            {
                _keepHeartbeating = false;
            }
        }
        #endregion

        #region Game Vars

        /// <summary>
        ///     App's current version
        /// </summary>
        public static string CurrentVersion
        {
            get
            {
                var currentVersion = Package.Current.Id.Version;
                return $"v{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";
            }
        }

        /// <summary>
        ///     Settings downloaded from server
        /// </summary>
        public static GlobalSettings GameSetting { get; private set; }

        /// <summary>
        ///     Player's data, we use it just for the username
        /// </summary>
        public static PlayerData PlayerData => _session.Player.Data;

        /// <summary>
        ///     Stats for the current player, including current level and experience related stuff
        /// </summary>
        public static PlayerStats PlayerStats { get; private set; }

        /// <summary>
        ///     Contains infos about level up rewards
        /// </summary>
        public static InventoryDelta InventoryDelta { get; private set; }

        public static bool IsIncenseActive
        {
            get { return AppliedItems.Count(x => x.ItemType == ItemType.Incense && !x.IsExpired) > 0; }
        }

        public static bool IsXpBoostActive
        {
            get { return AppliedItems.Count(x => x.ItemType == ItemType.XpBoost && !x.IsExpired) > 0; }
        }

        public static ObservableCollection<int> AdwardedXP { get; set; } = new ObservableCollection<int>();

        public static void AddGameXP(int AwardedXP)
        {
            AdwardedXP.Add(AwardedXP);
        }

        #region Collections

        /// <summary>
        ///		Collection of applied items
        /// </summary>
        public static ObservableCollection<AppliedItemWrapper> AppliedItems { get; set; } = new ObservableCollection<AppliedItemWrapper>();

        /// <summary>
        ///     Collection of Pokemon in 1 step from current position
        /// </summary>
        public static ObservableCollection<MapPokemonWrapper> CatchablePokemons { get; set; } = new ObservableCollection<MapPokemonWrapper>();

        /// <summary>
        ///     Collection of Pokemon in 2 steps from current position
        /// </summary>
        public static ObservableCollection<NearbyPokemonWrapper> NearbyPokemons { get; set; } = new ObservableCollection<NearbyPokemonWrapper>();

        /// <summary>
        ///     Collection of lured Pokemon
        /// </summary>
        public static ObservableCollection<LuredPokemon> LuredPokemons { get; set; } = new ObservableCollection<LuredPokemon>();

        /// <summary>
        ///     Collection of incense Pokemon
        /// </summary>
        public static ObservableCollection<IncensePokemon> IncensePokemons { get; set; } = new ObservableCollection<IncensePokemon>();

        /// <summary>
        ///     Collection of Pokestops in the current area
        /// </summary>
        public static ObservableCollection<FortDataWrapper> NearbyPokestops { get; set; } = new ObservableCollection<FortDataWrapper>();

        /// <summary>
        ///     Collection of Gyms in the current area
        /// </summary>
        public static ObservableCollection<FortDataWrapper> NearbyGyms { get; set; } = new ObservableCollection<FortDataWrapper>();

        /// <summary>
        ///     Stores Items in the current inventory
        /// </summary>
        public static ObservableCollection<ItemData> ItemsInventory { get; set; } = new ObservableCollection<ItemData>();

        /// <summary>
        ///     Stores Items that can be used to catch a Pokemon
        /// </summary>
        public static ObservableCollection<ItemData> CatchItemsInventory { get; set; } = new ObservableCollection<ItemData>();

        /// <summary>
        ///     Stores Incubators in the current inventory
        /// </summary>
        public static ObservableCollection<EggIncubator> IncubatorsInventory { get; set; } = new ObservableCollection<EggIncubator>();

        /// <summary>
        ///     Stores Pokemons in the current inventory
        /// </summary>
        public static ObservableCollectionPlus<PokemonData> PokemonsInventory { get; set; } = new ObservableCollectionPlus<PokemonData>();

        /// <summary>
        ///     Stores Eggs in the current inventory
        /// </summary>
        public static ObservableCollection<PokemonData> EggsInventory { get; set; } = new ObservableCollection<PokemonData>();

        /// <summary>
        ///     Stores player's current Pokedex
        /// </summary>
        public static ObservableCollectionPlus<PokedexEntry> PokedexInventory { get; set; } = new ObservableCollectionPlus<PokedexEntry>();

        /// <summary>
        ///     Stores player's current candies
        /// </summary>
        public static ObservableCollection<Candy> CandyInventory { get; set; } = new ObservableCollection<Candy>();

        #endregion

        #region Templates from server

        /// <summary>
        ///     Stores extra useful data for the Pokedex, like Pokemon type and other stuff that is missing from PokemonData
        /// </summary>
        public static IEnumerable<PokemonSettings> PokemonSettings { get; private set; } = new List<PokemonSettings>();

        /// <summary>
        ///     Stores upgrade costs (candy, stardust) per each level
        /// </summary>
        //public static Dictionary<int, object[]> PokemonUpgradeCosts { get; private set; } = new Dictionary<int, object[]>();

        /// <summary>
        /// Upgrade settings per each level
        /// </summary>
        public static PokemonUpgradeSettings PokemonUpgradeSettings { get; private set; }

        /// <summary>
        ///     Stores data about Pokemon Go settings
        /// </summary>
        public static IEnumerable<MoveSettings> MoveSettings { get; private set; } = new List<MoveSettings>();
        public static IEnumerable<BadgeSettings> BadgeSettings { get; private set; } = new List<BadgeSettings>();
        public static IEnumerable<GymBattleSettings> BattleSettings { get; private set; } = new List<GymBattleSettings>();
        public static IEnumerable<EncounterSettings> EncounterSettings { get; private set; } = new List<EncounterSettings>();
        public static IEnumerable<GymLevelSettings> GymLevelSettings { get; private set; } = new List<GymLevelSettings>();
        public static IEnumerable<IapSettings> IapSettings { get; private set; } = new List<IapSettings>();
        public static IEnumerable<ItemSettings> ItemSettings { get; private set; } = new List<ItemSettings>();
        public static IEnumerable<PlayerLevelSettings> PlayerLevelSettings { get; private set; } = new List<PlayerLevelSettings>();
        public static IEnumerable<QuestSettings> QuestSettings { get; private set; } = new List<QuestSettings>();
        public static IEnumerable<IapItemDisplay> IapItemDisplay { get; private set; } = new List<IapItemDisplay>();
        public static IEnumerable<MoveSequenceSettings> MoveSequenceSettings { get; private set; } = new List<MoveSequenceSettings>();
        public static IEnumerable<CameraSettings> CameraSettings { get; private set; } = new List<CameraSettings>();
        #endregion

        #endregion

        #region Constructor

        static GameClient()
        {
                PokedexInventory.CollectionChanged += PokedexInventory_CollectionChanged;
                AppliedItems.CollectionChanged += AppliedItems_CollectionChanged;
            // TODO: Investigate whether or not this needs to be unsubscribed when the app closes.
        }

        public static void SetCredentialsFromSettings()
        {
            var credentials = SettingsService.Instance.UserCredentials;
            if (credentials != null)
            {
                credentials.RetrievePassword();
                _clientSettings = new Console.Settings()
                {
                    AuthType = SettingsService.Instance.LastLoginService,
                    PtcUsername = SettingsService.Instance.LastLoginService == AuthType.Ptc ? credentials.UserName : null,
                    PtcPassword = SettingsService.Instance.LastLoginService == AuthType.Ptc ? credentials.Password : null,
                    GoogleUsername = SettingsService.Instance.LastLoginService == AuthType.Google ? credentials.UserName : null,
                    GooglePassword = SettingsService.Instance.LastLoginService == AuthType.Google ? credentials.Password : null,
                };
            }
        }

        /// <summary>
        /// When new items are added to the Pokedex, reset the Nearby Pokemon so their state can be re-run.
        /// </summary>
        /// <remarks>
        /// This exists because the Nearby Pokemon are Map objects, and are loaded before Inventory. If you don't do this,
        /// the first Nearby items are always shown as "new to the Pokedex" until they disappear, regardless of if they are
        /// ACTUALLY new.
        /// </remarks>
        private static void PokedexInventory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    // advancedrei: This is a total order-of-operations hack.
                    var nearby = NearbyPokemons.ToList();
                    NearbyPokemons.Clear();
                    NearbyPokemons.AddRange(nearby);
                });
            }
        }

        private static void AppliedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    OnAppliedItemStarted?.Invoke(null, (AppliedItemWrapper)e.NewItems[0]);
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    OnAppliedItemExpired?.Invoke(null, (AppliedItemWrapper)e.OldItems[0]);
                }
            });
        }


        #endregion

        #region Game Logic

        #region Login/Logout

        /// <summary>
        /// Saves the new AccessToken to settings.
        /// </summary>
        private static void SaveAccessToken()
        {
            SettingsService.Instance.AccessTokenString = JsonConvert.SerializeObject(_session.AccessToken);
        }

        /// <summary>
        /// Gets current AccessToken
        /// </summary>
        /// <returns></returns>
        public static AccessToken GetAccessToken()
        {
            try // If the current Accesstoken is in an old format (pre-POGOLib version), invalidate it and let the user start over by logging in
            {
                var tokenString = SettingsService.Instance.AccessTokenString;
                return tokenString == null ? null : JsonConvert.DeserializeObject<AccessToken>(SettingsService.Instance.AccessTokenString);
            }
            catch (Exception)
            {
                SettingsService.Instance.AccessTokenString = null;
                SettingsService.Instance.UserCredentials = null;
                return null;
            }
        }

        /// <summary>
        /// Creates and initializes POGOLib session
        /// </summary>
        private async static Task CreateSession(Geoposition pos)
        {
            if (_session != null)
            {
                _session.AccessTokenUpdated -= SessionOnAccessTokenUpdated;
                _session.Player.Inventory.Update -= InventoryOnUpdate;
                _session.Map.Update -= MapOnUpdate;
                _session.RpcClient.CheckChallengeReceived -= SessionOnCheckChallengeReceived;
                _session.RpcClient.HatchedEggsReceived -= SessionOnHatchedEggsReceived;
                _session.RpcClient.CheckAwardedBadgesReceived -= SessionOnCheckAwardedBadgesReceived;
            }

            Configuration.IgnoreHashVersion = false;
            Configuration.Hasher = new PokeHashHasher(SettingsService.Instance.PokehashAuthKey);

            // Login
            ILoginProvider loginProvider;
            switch (_clientSettings.AuthType)
            {
                case AuthType.Google:
                    loginProvider = new GoogleLoginProvider(_clientSettings.GoogleUsername, _clientSettings.GooglePassword);
                    break;
                case AuthType.Ptc:
                    loginProvider = new PtcLoginProvider(_clientSettings.PtcUsername, _clientSettings.PtcPassword);
                    break;
                default:
                    throw new ArgumentException("Login provider must be either \"google\" or \"ptc\".");
            }

            var locRandom = new Random();
            var initLat = pos.Coordinate.Latitude + locRandom.NextDouble(-0.000030, 0.000030);
            var initLong = pos.Coordinate.Longitude + locRandom.NextDouble(-0.000030, 0.000030);

            try
            {
                AccessToken accessToken = GetAccessToken();
                if (accessToken != null && !accessToken.IsExpired)
                {
                    _session = Login.GetSession(loginProvider, accessToken, initLat, initLong);
                }
                else
                {
                    _session = await Login.GetSession(loginProvider, initLat, initLong);
                    SaveAccessToken();
                }
            }
            catch (PtcLoginException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {

            }

            _session.AccessTokenUpdated += SessionOnAccessTokenUpdated;
            _session.Player.Inventory.Update += InventoryOnUpdate;
            _session.Map.Update += MapOnUpdate;
            _session.RpcClient.CheckChallengeReceived += SessionOnCheckChallengeReceived;
            _session.RpcClient.HatchedEggsReceived += SessionOnHatchedEggsReceived;
            _session.RpcClient.CheckAwardedBadgesReceived += SessionOnCheckAwardedBadgesReceived;

            await Task.CompletedTask;
        }

        /// <summary>
        ///     Sets things up if we didn't come from the login page
        /// </summary>
        /// <returns></returns>
        public static async Task InitializeSession()
        {
            DataCache.Init();

            var credentials = SettingsService.Instance.UserCredentials;
            credentials.RetrievePassword();
            _clientSettings = new Console.Settings
            {
                AuthType = SettingsService.Instance.LastLoginService,
                PtcUsername = SettingsService.Instance.LastLoginService == AuthType.Ptc ? credentials.UserName : null,
                PtcPassword = SettingsService.Instance.LastLoginService == AuthType.Ptc ? credentials.Password : null,
                GoogleUsername = SettingsService.Instance.LastLoginService == AuthType.Google ? credentials.UserName : null,
                GooglePassword = SettingsService.Instance.LastLoginService == AuthType.Google ? credentials.Password : null,
            };

            Geoposition pos = await GetInitialLocation();
            if (pos == null)
            {
                return;
            }

            try
            {
                await CreateSession(pos);
            }
            catch (PtcLoginException ex)
            {
                throw ex;
            }

            // Get the game settings from the session
            await GetGameSettings();

            IsInitialized = true;
        }

        private static void SessionOnAccessTokenUpdated(object sender, EventArgs eventArgs)
        {
            SaveAccessToken();

            Logger.Info("Saved access token to file.");
        }

        private static async void SessionOnCheckChallengeReceived(object sender, CheckChallengeResponse e)
        {
            if (e.ShowChallenge && !String.IsNullOrWhiteSpace(e.ChallengeUrl) && e.ChallengeUrl.Length > 5)
            {
                // Captcha is shown in checkChallengeResponse.ChallengeUrl
                Logger.Warn($"ChallengeURL: {e.ChallengeUrl}");
                // breakpoint here to manually resolve Captcha in a browser
                // after that set token to str variable from browser (see screenshot)
                Logger.Warn("Pause");

                //GOTO THE REQUIRED PAGE
                if (BootStrapper.Current.NavigationService.CurrentPageType != typeof(ChallengePage))
                {
                    await DispatcherHelper.RunInDispatcherAndAwait(() =>
                    {
                        // We are not in UI thread probably, so run this via dispatcher
                        BootStrapper.Current.NavigationService.Navigate(typeof(ChallengePage), e.ChallengeUrl);
                    });
                }
            }
        }

        private static void SessionOnHatchedEggsReceived(object sender, GetHatchedEggsResponse hatchedEggResponse)
        {
            OnEggHatched?.Invoke(sender, hatchedEggResponse);
        }

        private static void SessionOnCheckAwardedBadgesReceived(object sender, CheckAwardedBadgesResponse e)
        {
            OnAwardedBadgesReceived?.Invoke(sender, e);
        }

        private static void InventoryOnUpdate(object sender, EventArgs eventArgs)
        {
            Inventory inventory = sender as Inventory;
            UpdateLocalInventory(inventory);
            Logger.Info("Inventory was updated.");
        }

        private async static void MapOnUpdate(object sender, EventArgs eventArgs)
        {
            if (_isSessionEnabled)
            {
                Map map = sender as Map;
                await UpdateMapObjects(map);
                Logger.Info("Map was updated.");
            }
        }

        /// <summary>
        ///     Starts a PTC session for the given user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>true if login worked</returns>
        public static async Task<bool> DoPtcLogin(string username, string password)
        {
            _clientSettings = new Console.Settings
            {
                PtcUsername = username,
                PtcPassword = password,
                AuthType = AuthType.Ptc
            };

            Geoposition pos = await GetInitialLocation();
            if (pos == null)
            {
                return false;
            }

            try
            {
                await CreateSession(pos);
            }
            catch (PtcLoginException ex)
            {
                return false;
            }
            catch (Exception ex)
            {

            }

            SettingsService.Instance.LastLoginService = AuthType.Ptc;
            SettingsService.Instance.UserCredentials = new PasswordCredential(nameof(SettingsService.Instance.UserCredentials), username, password);

            // Get the game settings from the session
            await GetGameSettings();

            // Return true if login worked, meaning that we have a token
            return true;

        }

        /// <summary>
        ///     Starts a Google session for the given user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>true if login worked</returns>
        public static async Task<bool> DoGoogleLogin(string email, string password)
        {
            _clientSettings = new Console.Settings
            {
                GoogleUsername = email,
                GooglePassword = password,
                AuthType = AuthType.Google
            };

            Geoposition pos = await GetInitialLocation();
            if (pos == null)
            {
                return false;
            }

            try
            {
                await CreateSession(pos);
            }
            catch (PtcLoginException ex)
            {
                return false;
            }
            catch (Exception ex)
            {

            }

            SettingsService.Instance.LastLoginService = AuthType.Google;
            SettingsService.Instance.UserCredentials = new PasswordCredential(nameof(SettingsService.Instance.UserCredentials), email, password);

            // Get the game settings from the session
            await GetGameSettings();

            // Return true if login worked, meaning that we have a token
            return true;
        }

        /// <summary>
        ///     Logs the user out by clearing data and timers
        /// </summary>
        public static void DoLogout()
        {
            // Stop activities
            _session.Shutdown();

            // Clear stored token
            SettingsService.Instance.AccessTokenString = null;
            SettingsService.Instance.UserCredentials = null;
            LocationServiceHelper.Instance.PropertyChanged -= LocationHelperPropertyChanged;
        }

        #endregion

        #region Data Updating
        private static Compass _compass;

        private static AppliedItemsHeartbeat _heartbeat;

        public static event EventHandler<GetHatchedEggsResponse> OnEggHatched;
        public static event EventHandler<CheckAwardedBadgesResponse> OnAwardedBadgesReceived;
        public static event EventHandler<AppliedItemWrapper> OnAppliedItemExpired;
        public static event EventHandler<AppliedItemWrapper> OnAppliedItemStarted;

        #region GameSettings

        public static async Task GetGameSettings()
        {
            Busy.SetBusy(true, "Getting game settings");

            // Momentarily start the session, to retrieve game settings, inventory and player
            if (!await _session.StartupAsync())
            {
                throw new Exception("Session couldn't start up.");
            }

            // Copy the Game Settings and Player Stats locally
            GameSetting = _session.GlobalSettings;
            PlayerStats = _session.Player.Stats;
            PokemonsInventory.AddRange(_session.Player.Inventory.InventoryItems.Select(item => item.InventoryItemData.PokemonData)
                .Where(item => item != null && item.PokemonId > 0), true);

            // As soon as we have copied the required information, pause the session. It will be restarted on the game map page
            //_session.Pause();
            _isSessionEnabled = true;

            Busy.SetBusy(false);

            await Task.CompletedTask;
        }

        public static async Task LoadGameSettings(bool ForceRefresh = false)
        {
            GameSetting = _session.GlobalSettings;

            // The itemtemplates can be upated since a new release, how can we detect this to enable a force refresh here?
            await UpdateItemTemplates(ForceRefresh);
        }
        #endregion

        #region Compass Stuff
        /// <summary>
        /// We fire this event when the current compass position changes
        /// </summary>
        public static event EventHandler<CompassReading> HeadingUpdated;
        private static void compass_ReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
        {
            HeadingUpdated?.Invoke(sender, args.Reading);
        }
        #endregion

        /// <summary>
        ///     Starts the timer to update map objects and the handler to update position
        /// </summary>
        public static async Task InitializeDataUpdate()
        {
            // Get the game settings, they contain information about pokemons, moves, and a lot more...
            await LoadGameSettings();

            #region Compass management
            SettingsService.Instance.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(SettingsService.Instance.MapAutomaticOrientationMode))
                {
                    switch (SettingsService.Instance.MapAutomaticOrientationMode)
                    {
                        case MapAutomaticOrientationModes.Compass:
                            _compass = Compass.GetDefault();
                            _compass.ReportInterval = Math.Max(_compass.MinimumReportInterval, 50);
                            _compass.ReadingChanged += compass_ReadingChanged;
                            break;
                        case MapAutomaticOrientationModes.None:
                        case MapAutomaticOrientationModes.GPS:
                        default:
                            if (_compass != null)
                            {
                                _compass.ReadingChanged -= compass_ReadingChanged;
                                _compass = null;
                            }
                            break;
                    }
                }
            };
            //Trick to trigger the PropertyChanged for MapAutomaticOrientationMode ;)
            SettingsService.Instance.MapAutomaticOrientationMode = SettingsService.Instance.MapAutomaticOrientationMode;
            #endregion
            Busy.SetBusy(true, Resources.CodeResources.GetString("GettingGpsSignalText"));
            await LocationServiceHelper.Instance.InitializeAsync();

            // Update geolocator settings based on server
            LocationServiceHelper.Instance.UpdateMovementThreshold(GameSetting.MapSettings.GetMapObjectsMinDistanceMeters);
            LocationServiceHelper.Instance.PropertyChanged += LocationHelperPropertyChanged;

            if (_heartbeat == null)
                _heartbeat = new AppliedItemsHeartbeat();
            await _heartbeat.StartDispatcher();

            // Update before starting timer
            Busy.SetBusy(true, Resources.CodeResources.GetString("GettingUserDataText"));
            UpdateInventory();
            if (PlayerData != null && PlayerStats != null)
                Busy.SetBusy(false);
        }

        private static void LocationHelperPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if(e.PropertyName==nameof(LocationServiceHelper.Instance.Geoposition))
			{
                if (_lastPlayerLocationUpdate == null || _lastPlayerLocationUpdate.AddSeconds((int)GameClient.GameSetting.MapSettings.GetMapObjectsMinRefreshSeconds) < DateTime.Now)
                {
                    // Updating player's position
                    var position = LocationServiceHelper.Instance.Geoposition.Coordinate.Point.Position;
                    if (_session != null)
                    {
                        _lastPlayerLocationUpdate = DateTime.Now;
                        _session.Player.SetCoordinates(position.Latitude, position.Longitude, LocationServiceHelper.Instance.Geoposition.Coordinate.Accuracy);
                    }
                }
			}
		}

        private static DateTime _lastPlayerLocationUpdate;
        private static bool _isSessionEnabled = true;

        /// <summary>
        ///     Toggles the update timer based on the isEnabled value
        /// </summary>
        /// <param name="isEnabled"></param>
        public static async void ToggleUpdateTimer(bool isEnabled = true)
        {
            if (_session == null) return;

            Logger.Info($"Called ToggleUpdateTimer({isEnabled})");
            if (isEnabled)
            {
                _isSessionEnabled = true;
                //_session.Resume();
            }
            else
            {
                _isSessionEnabled = false;
                //_session.Pause();
            }
        }

        /// <summary>
        ///     Updates catcheable and nearby Pokemons + Pokestops.
        ///     We're using a single method so that we don't need two separate calls to the server, making things faster.
        /// </summary>
        /// <returns></returns>
        private static async Task UpdateMapObjects(Map map)
        {
            await WindowWrapper.Current().Dispatcher.DispatchAsync(async() =>
            {
                // update catchable pokemons
                var newCatchablePokemons = map.Cells.SelectMany(x => x.CatchablePokemons).Select(item => new MapPokemonWrapper(item)).ToArray();
                Logger.Info($"Found {newCatchablePokemons.Length} catchable pokemons");
                CatchablePokemons.UpdateWith(newCatchablePokemons, x => x,
                    (x, y) => x.EncounterId == y.EncounterId);

                // update nearby pokemons
                var newNearByPokemons = map.Cells.SelectMany(x => x.NearbyPokemons).ToArray();
                Logger.Info($"Found {newNearByPokemons.Length} nearby pokemons");
                // for this collection the ordering is important, so we follow a slightly different update mechanism
                NearbyPokemons.UpdateByIndexWith(newNearByPokemons, x => new NearbyPokemonWrapper(x));

                // update poke stops on map
                var newPokeStops = map.Cells
                    .SelectMany(x => x.Forts)
                    .Where(x => x.Type == FortType.Checkpoint)
                    .ToArray();
                Logger.Info($"Found {newPokeStops.Length} nearby PokeStops");
                NearbyPokestops.UpdateWith(newPokeStops, x => new FortDataWrapper(x), (x, y) => x.Id == y.Id);

                // update gyms on map
                var newGyms = map.Cells
                    .SelectMany(x => x.Forts)
                    .Where(x => x.Type == FortType.Gym)
                    .ToArray();
                Logger.Info($"Found {newGyms.Length} nearby Gyms");
                NearbyGyms.UpdateWith(newGyms, x => new FortDataWrapper(x), (x, y) => x.Id == y.Id);

                // Update LuredPokemon
                var newLuredPokemon = newPokeStops.Where(item => item.LureInfo != null).Select(item => new LuredPokemon(item.LureInfo, item.Latitude, item.Longitude)).ToArray();
                Logger.Info($"Found {newLuredPokemon.Length} lured Pokemon");
                LuredPokemons.UpdateByIndexWith(newLuredPokemon, x => x);

                // Update IncensePokemon
                if (IsIncenseActive)
                {
                    var incensePokemonResponse = await GetIncensePokemons(LocationServiceHelper.Instance.Geoposition);
                    if (incensePokemonResponse.Result == GetIncensePokemonResponse.Types.Result.IncenseEncounterAvailable)
                    {
                        IncensePokemon[] newIncensePokemon;
                        newIncensePokemon = new IncensePokemon[1];
                        newIncensePokemon[0] = new IncensePokemon(incensePokemonResponse, incensePokemonResponse.Latitude, incensePokemonResponse.Longitude);
                        Logger.Info($"Found incense Pokemon {incensePokemonResponse.PokemonId}");
                        IncensePokemons.UpdateByIndexWith(newIncensePokemon, x => x);
                    }
                }
                Logger.Info("Finished updating map objects");

                // Update BuddyPokemon Stats
                if (PlayerData.BuddyPokemon.Id != 0)
                {
                    var buddyWalkedResponse = await GetBuddyWalked();
                    if (buddyWalkedResponse.Success)
                    {
                        Logger.Info($"BuddyWalked CandyID: {buddyWalkedResponse.FamilyCandyId}, CandyCount: {buddyWalkedResponse.CandyEarnedCount}");
                    };
                }
            });
        }

        private static async Task<Geoposition> GetInitialLocation()
        {
            Busy.SetBusy(true, Resources.CodeResources.GetString("GettingGpsSignalText"));

            try
            {
                await LocationServiceHelper.Instance.InitializeAsync();
            }
            catch (Exception)
            {
                throw new LocationException();
            }

            return LocationServiceHelper.Instance.Geoposition;
        }

        #endregion

        #region Map & Position

        /// <summary>
        ///		Gets updated incense Pokemon data based on provided position
        /// </summary>
        /// <param name="geoposition"></param>
        /// <returns></returns>
        private static async Task<GetIncensePokemonResponse> GetIncensePokemons(Geoposition geoposition)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetIncensePokemon,
                RequestMessage = new GetIncensePokemonMessage
                {
                    PlayerLatitude = geoposition.Coordinate.Latitude,
                    PlayerLongitude = geoposition.Coordinate.Longitude
                }.ToByteString()
            });
            var getIncensePokemonResponse = GetIncensePokemonResponse.Parser.ParseFrom(response);

            return getIncensePokemonResponse;
        }

        #endregion

        #region Player Data & Inventory

        /// <summary>
        ///     List of items that can be used when trying to catch a Pokemon
        /// </summary>
        public static readonly List<ItemId> CatchItemIds = new List<ItemId>
        {
            ItemId.ItemPokeBall,
            ItemId.ItemGreatBall,
            ItemId.ItemBlukBerry,
            ItemId.ItemMasterBall,
            ItemId.ItemNanabBerry,
            ItemId.ItemPinapBerry,
            ItemId.ItemRazzBerry,
            ItemId.ItemUltraBall,
            ItemId.ItemWeparBerry
        };

        /// <summary>
        /// List of items, that can be used from the normal ItemsInventoryPage
        /// </summary>
        public static readonly List<ItemId> NormalUseItemIds = new List<ItemId>
        {
            ItemId.ItemPotion,
            ItemId.ItemSuperPotion,
            ItemId.ItemHyperPotion,
            ItemId.ItemMaxPotion,
            ItemId.ItemRevive,
            ItemId.ItemMaxRevive,
            ItemId.ItemLuckyEgg,
            ItemId.ItemIncenseOrdinary,
            ItemId.ItemIncenseSpicy,
            ItemId.ItemIncenseCool,
            ItemId.ItemIncenseFloral,
        };

        /// <summary>
        ///     Gets user's profile
        /// </summary>
        /// <returns></returns>
        public async static Task<GetPlayerProfileResponse> UpdateProfile()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetPlayerProfile,
                RequestMessage = new GetPlayerProfileMessage
                {
                }.ToByteString()
            });
            var getPlayerProfileResponse = GetPlayerProfileResponse.Parser.ParseFrom(response);

            return getPlayerProfileResponse;
        }

        public static async Task<GetPlayerProfileResponse> GetPlayerProfile(string playerName)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetPlayerProfile,
                RequestMessage =
                new GetPlayerProfileMessage
                {
                    PlayerName = playerName,
                }.ToByteString()
            });
            var getPlayerProfileResponse = GetPlayerProfileResponse.Parser.ParseFrom(response);

            return getPlayerProfileResponse;
        }

        /// <summary>
        ///     Gets player's inventoryDelta
        /// </summary>
        /// <returns></returns>
        public static async Task<LevelUpRewardsResponse> UpdatePlayerStats(bool checkForLevelUp = false)
        {
            var tmpStats = _session.Player.Inventory.InventoryItems.FirstOrDefault(i => i?.InventoryItemData?.PlayerStats != null)?
                .InventoryItemData?.PlayerStats;

            if (checkForLevelUp && (PlayerStats == null || PlayerStats.Level != tmpStats.Level))
            {
                PlayerStats = tmpStats;
                var levelUpResponse = await GetLevelUpRewards(tmpStats.Level);
                //await UpdateInventory();

                // Set busy to false because initial loading may have left it going until we had PlayerStats
                Busy.SetBusy(false);
                return levelUpResponse;
            }
            PlayerStats = tmpStats;

            // Set busy to false because initial loading may have left it going until we had PlayerStats
            Busy.SetBusy(false);
            return null;
        }

        /// <summary>
        ///     Gets player's inventoryDelta
        /// </summary>
        /// <returns></returns>
        public static async Task<GetInventoryResponse> GetInventory()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetInventory,
                RequestMessage =
                new GetInventoryMessage
                {
                    LastTimestampMs = 0
                }.ToByteString()
            });
            var getInventoryResponse = GetInventoryResponse.Parser.ParseFrom(response);

            return getInventoryResponse;
        }

        /// <summary>
        ///     Gets the rewards after leveling up
        /// </summary>
        /// <returns></returns>
        public static async Task<LevelUpRewardsResponse> GetLevelUpRewards(int newLevel)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.LevelUpRewards,
                RequestMessage = new LevelUpRewardsMessage
                {
                    Level = newLevel
                }.ToByteString()
            });
            var getLevelUpRewardsResponse = LevelUpRewardsResponse.Parser.ParseFrom(response);

            return getLevelUpRewardsResponse;
        }

        public static async Task<DownloadItemTemplatesResponse> GetItemTemplates()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.DownloadItemTemplates,
                RequestMessage = new DownloadItemTemplatesMessage
                {
                     PageOffset = 0,
                     Paginate = false,
                     PageTimestamp = 0

                }.ToByteString()
            });
            var downloadItemTemplatesResponse = DownloadItemTemplatesResponse.Parser.ParseFrom(response);

            return downloadItemTemplatesResponse;
        }

        /// <summary>
        ///     Pokedex extra data doesn't change so we can just call this method once.
        /// </summary>
        /// <returns></returns>
        private static async Task UpdateItemTemplates(bool ForceRefresh)
        {
            // Get all the templates
            var itemTemplates = await DataCache.GetAsync("itemTemplates", async () => (await GetItemTemplates()).ItemTemplates, DateTime.Now.AddMonths(1), ForceRefresh);

            // Update Pokedex data
            PokemonSettings = await DataCache.GetAsync(nameof(PokemonSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(
                    item => item.PokemonSettings != null && item.PokemonSettings.FamilyId != PokemonFamilyId.FamilyUnset)
                    .Select(item => item.PokemonSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            PokemonUpgradeSettings = await DataCache.GetAsync(nameof(PokemonUpgradeSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.First(item => item.PokemonUpgrades != null).PokemonUpgrades;
            }, DateTime.Now.AddMonths(1), ForceRefresh);


            // Update Moves data
            MoveSettings = await DataCache.GetAsync(nameof(MoveSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.MoveSettings != null)
                                    .Select(item => item.MoveSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            BadgeSettings = await DataCache.GetAsync(nameof(BadgeSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.BadgeSettings != null)
                                    .Select(item => item.BadgeSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            BattleSettings = await DataCache.GetAsync(nameof(BattleSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.BattleSettings != null)
                                    .Select(item => item.BattleSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            EncounterSettings = await DataCache.GetAsync(nameof(EncounterSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.EncounterSettings != null)
                                    .Select(item => item.EncounterSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            GymLevelSettings = await DataCache.GetAsync(nameof(GymLevelSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.GymLevel != null)
                                    .Select(item => item.GymLevel);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            IapSettings = await DataCache.GetAsync(nameof(IapSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.IapSettings != null)
                                    .Select(item => item.IapSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            ItemSettings = await DataCache.GetAsync(nameof(ItemSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.ItemSettings != null)
                                    .Select(item => item.ItemSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            PlayerLevelSettings = await DataCache.GetAsync(nameof(PlayerLevelSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.PlayerLevel != null)
                                    .Select(item => item.PlayerLevel);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            QuestSettings = await DataCache.GetAsync(nameof(QuestSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.QuestSettings != null)
                                    .Select(item => item.QuestSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            CameraSettings = await DataCache.GetAsync(nameof(CameraSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.Camera != null)
                                    .Select(item => item.Camera);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            IapItemDisplay = await DataCache.GetAsync(nameof(IapItemDisplay), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.IapItemDisplay != null)
                                    .Select(item => item.IapItemDisplay);
            }, DateTime.Now.AddMonths(1), ForceRefresh);

            MoveSequenceSettings = await DataCache.GetAsync(nameof(MoveSequenceSettings), async () =>
            {
                await Task.CompletedTask;
                return itemTemplates.Where(item => item.MoveSequenceSettings != null)
                                    .Select(item => item.MoveSequenceSettings);
            }, DateTime.Now.AddMonths(1), ForceRefresh);
        }

        public static void UpdateLocalInventory(Inventory inventory)
        {
            var fullInventory = inventory.InventoryItems;

            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                // Update items
                ItemsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.Item != null)
                    .GroupBy(item => item.InventoryItemData.Item)
                    .Select(item => item.First().InventoryItemData.Item), true);
                CatchItemsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.Item != null &&
                                                                 CatchItemIds.Contains(item.InventoryItemData.Item.ItemId))
                        .GroupBy(item => item.InventoryItemData.Item)
                        .Select(item => item.First().InventoryItemData.Item), true);
                AppliedItems.AddRange(fullInventory.Where(item => item.InventoryItemData?.AppliedItems != null)
                        .SelectMany(item => item.InventoryItemData.AppliedItems.Item)
                        .Select(item => new AppliedItemWrapper(item)), true);

                // Update incbuators
                IncubatorsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.EggIncubators != null)
                    .SelectMany(item => item.InventoryItemData.EggIncubators.EggIncubator)
                    .Where(item => item != null), true);

                // Update Pokedex
                PokedexInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.PokedexEntry != null)
                    .Select(item => item.InventoryItemData.PokedexEntry), true);

                // Update Pokemons
                PokemonsInventory.AddRange(fullInventory.Select(item => item.InventoryItemData?.PokemonData)
                    .Where(item => item != null && item.PokemonId > 0), true);

                // Any pokemons removed?
                var removedPokemons = fullInventory.Select(item => item.DeletedItem?.PokemonId)
                    .Where(item => item != null);

                if (removedPokemons.Count() > 0)
                {
                    foreach (var removedPokemon in removedPokemons)
                    {
                        foreach (PokemonData pokemon in PokemonsInventory)
                        {
                            if (pokemon.Id == removedPokemon)
                            {
                                PokemonsInventory.Remove(pokemon);
                                break;
                            }
                        }
                    }
                }

                EggsInventory.AddRange(fullInventory.Select(item => item.InventoryItemData?.PokemonData)
                    .Where(item => item != null && item.IsEgg), true);

                // Update candies
                CandyInventory.AddRange(from item in fullInventory
                                        where item.InventoryItemData?.Candy != null
                                        where item.InventoryItemData?.Candy.FamilyId != PokemonFamilyId.FamilyUnset
                                        group item by item.InventoryItemData?.Candy.FamilyId into family
                                        select new Candy
                                        {
                                            FamilyId = family.FirstOrDefault().InventoryItemData.Candy.FamilyId,
                                            Candy_ = family.FirstOrDefault().InventoryItemData.Candy.Candy_
                                        }, true);

            });
        }

        /// <summary>
        ///     Updates inventory data
        /// </summary>
        public static async void UpdateInventory(bool ForceUpdate = false)
        {
            var fullInventory = _session.Player.Inventory.InventoryItems;

            // Get ALL the items again
            if (ForceUpdate)
            {
                var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = new GetInventoryMessage
                    {

                    }.ToByteString()
                });
                var getInventoryResponse = GetInventoryResponse.Parser.ParseFrom(response);
                fullInventory = getInventoryResponse.InventoryDelta.InventoryItems;
            }

            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                // Update items
                ItemsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.Item != null)
                    .GroupBy(item => item.InventoryItemData.Item)
                    .Select(item => item.First().InventoryItemData.Item), true);
                CatchItemsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.Item != null && CatchItemIds.Contains(item.InventoryItemData.Item.ItemId))
                        .GroupBy(item => item.InventoryItemData.Item)
                        .Select(item => item.First().InventoryItemData.Item), true);
                AppliedItems.AddRange(fullInventory.Where(item => item.InventoryItemData?.AppliedItems != null)
                        .SelectMany(item => item.InventoryItemData.AppliedItems.Item)
                        .Select(item => new AppliedItemWrapper(item)), true);

                // Update incbuators
                IncubatorsInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.EggIncubators != null)
                    .SelectMany(item => item.InventoryItemData.EggIncubators.EggIncubator)
                    .Where(item => item != null), true);

                // Update Pokedex
                PokedexInventory.AddRange(fullInventory.Where(item => item.InventoryItemData?.PokedexEntry != null)
                    .Select(item => item.InventoryItemData.PokedexEntry), true);

                // Update Pokemons
                PokemonsInventory.AddRange(fullInventory.Select(item => item.InventoryItemData?.PokemonData)
                    .Where(item => item != null && item.PokemonId > 0), true);

                // Any pokemons removed?
                var removedPokemons = fullInventory.Select(item => item.DeletedItem?.PokemonId)
                    .Where(item => item != null);

                if (removedPokemons.Count() > 0)
                {
                    foreach (var removedPokemon in removedPokemons)
                    {
                        foreach (PokemonData pokemon in PokemonsInventory)
                        {
                            if (pokemon.Id == removedPokemon)
                            {
                                PokemonsInventory.Remove(pokemon);
                                break;
                            }
                        }
                    }
                }

                EggsInventory.AddRange(fullInventory.Select(item => item.InventoryItemData?.PokemonData)
                    .Where(item => item != null && item.IsEgg), true);

                // Update candies
                CandyInventory.AddRange(from item in fullInventory
                                        where item.InventoryItemData?.Candy != null
                                        where item.InventoryItemData?.Candy.FamilyId != PokemonFamilyId.FamilyUnset
                                        group item by item.InventoryItemData?.Candy.FamilyId into family
                                        select new Candy
                                        {
                                            FamilyId = family.FirstOrDefault().InventoryItemData.Candy.FamilyId,
                                            Candy_ = family.FirstOrDefault().InventoryItemData.Candy.Candy_
                                        }, true);

            });
        }

        public static async Task<GetBuddyWalkedResponse> GetBuddyWalked()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetBuddyWalked,
                RequestMessage = new GetBuddyWalkedMessage
                {
                    
                }.ToByteString()
            });
            var getBuddyWalkedResponse = GetBuddyWalkedResponse.Parser.ParseFrom(response);

            return getBuddyWalkedResponse;
        }

        public static async Task<CheckAwardedBadgesResponse> GetNewlyAwardedBadges()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.CheckAwardedBadges,
                RequestMessage = new CheckAwardedBadgesMessage
                {
                }.ToByteString()
            });
            var checkAwardedBadgesResponse = CheckAwardedBadgesResponse.Parser.ParseFrom(response);

            return checkAwardedBadgesResponse;
        }

        public static async Task<CollectDailyBonusResponse> CollectDailyBonus()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.CollectDailyBonus,
                RequestMessage = new CollectDailyBonusMessage
                {
                }.ToByteString()
            });
            var collectDailyBonusResponse = CollectDailyBonusResponse.Parser.ParseFrom(response);

            return collectDailyBonusResponse;
        }

        public static async Task<CollectDailyDefenderBonusResponse> CollectDailyDefenderBonus()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.CollectDailyDefenderBonus,
                RequestMessage = new CollectDailyDefenderBonusMessage
                {
                }.ToByteString()
            });
            var collectDailyDefenderBonusResponse = CollectDailyDefenderBonusResponse.Parser.ParseFrom(response);

            return collectDailyDefenderBonusResponse;
        }

        public static async Task<EquipBadgeResponse> EquipBadge(BadgeType type)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.EquipBadge,
                RequestMessage = new EquipBadgeMessage
                {
                    BadgeType = type
                }.ToByteString()
            });
            var equipBadgeResponse = EquipBadgeResponse.Parser.ParseFrom(response);

            return equipBadgeResponse;
        }

        public static async Task<SetAvatarResponse> SetAvatar(PlayerAvatar playerAvatar)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.SetAvatar,
                RequestMessage = new SetAvatarMessage
                {
                    PlayerAvatar = playerAvatar
                }.ToByteString()
            });
            var setAvatarResponse = SetAvatarResponse.Parser.ParseFrom(response);

            return setAvatarResponse;
        }

        public static async Task<SetContactSettingsResponse> SetContactSetting(ContactSettings contactSettings)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.SetContactSettings,
                RequestMessage = new SetContactSettingsMessage
                {
                    ContactSettings = contactSettings
                }.ToByteString()
            });
            var setContactSettingsResponse = SetContactSettingsResponse.Parser.ParseFrom(response);

            return setContactSettingsResponse;
        }

        public static async Task<SetPlayerTeamResponse> SetPlayerTeam(TeamColor teamColor)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.SetPlayerTeam,
                RequestMessage = new SetPlayerTeamMessage
                {
                    Team = teamColor
                }.ToByteString()
            });
            var setPlayerTeamResponse = SetPlayerTeamResponse.Parser.ParseFrom(response);

            return setPlayerTeamResponse;
        }

        public static async Task<EncounterTutorialCompleteResponse> EncounterTutorialComplete(PokemonId pokemonId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.EncounterTutorialComplete,
                RequestMessage = new EncounterTutorialCompleteMessage
                {
                    PokemonId = pokemonId
                }.ToByteString()
            });
            var encounterTutorialCompleteResponse = EncounterTutorialCompleteResponse.Parser.ParseFrom(response);

            return encounterTutorialCompleteResponse;
        }

        public static async Task<MarkTutorialCompleteResponse> MarkTutorialComplete(TutorialState[] completed_tutorials, bool send_marketing_emails, bool send_push_notifications)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.MarkTutorialComplete,
                RequestMessage = new MarkTutorialCompleteMessage
                {
                    TutorialsCompleted = { completed_tutorials },
                    SendMarketingEmails = send_marketing_emails,
                    SendPushNotifications = send_push_notifications
                }.ToByteString()
            });
            var markTutorialCompleteResponse = MarkTutorialCompleteResponse.Parser.ParseFrom(response);

            return markTutorialCompleteResponse;
        }
        #endregion

        #region Pokemon Handling

        #region Pokedex

        /// <summary>
        ///     Gets extra data for the current pokemon
        /// </summary>
        /// <param name="pokemonId"></param>
        /// <returns></returns>
        public static PokemonSettings GetExtraDataForPokemon(PokemonId pokemonId)
        {
            // In case we have not retrieved the game settings yet, do it now.
            if (PokemonSettings.Count() == 0)
            {
                LoadGameSettings().Wait();
            }
            return PokemonSettings.First(pokemon => pokemon.PokemonId == pokemonId);
        }

        public static IEnumerable<PokemonData> GetFavoritePokemons()
        {
            return PokemonsInventory.Where(i => i.Favorite == 1);
        }

        public static IEnumerable<PokemonData> GetDeployedPokemons()
        {
            return PokemonsInventory.Where(i => !string.IsNullOrEmpty(i.DeployedFortId));
        }

        #endregion

        #region Catching

        /// <summary>
        ///     Encounters the selected Pokemon
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <returns></returns>
        public static async Task<EncounterResponse> EncounterPokemon(ulong encounterId, string spawnpointId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.Encounter,
                RequestMessage = new EncounterMessage
                {
                    EncounterId = encounterId,
                    SpawnPointId = spawnpointId,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var encounterResponse = EncounterResponse.Parser.ParseFrom(response);

            return encounterResponse;
        }

        /// <summary>
        ///     Encounters the selected lured Pokemon
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <returns></returns>
        public static async Task<DiskEncounterResponse> EncounterLurePokemon(ulong encounterId, string fortId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.DiskEncounter,
                RequestMessage = new DiskEncounterMessage
                {
                    EncounterId = encounterId,
                    FortId = fortId,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var diskEncounterResponse = DiskEncounterResponse.Parser.ParseFrom(response);

            return diskEncounterResponse;
        }

        /// <summary>
        ///		Encounters the selected incense pokemon
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <returns></returns>
        public static async Task<IncenseEncounterResponse> EncounterIncensePokemon(ulong encounterId, string encounterLocation)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.IncenseEncounter,
                RequestMessage = new IncenseEncounterMessage
                {
                    EncounterId = encounterId,
                    EncounterLocation = encounterLocation
                }.ToByteString()
            });
            var incenseEncounterResponse = IncenseEncounterResponse.Parser.ParseFrom(response);

            return incenseEncounterResponse;
        }

        /// <summary>
        ///     Executes Pokemon catching
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <param name="captureItem"></param>
        /// <param name="hitPokemon"></param>
        /// <returns></returns>
        public static async Task<CatchPokemonResponse> CatchPokemon(ulong encounterId, string spawnpointId, ItemId captureItem, bool hitPokemon = true)
        {
            var random = new Random();
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.CatchPokemon,
                RequestMessage = new CatchPokemonMessage
                {
                    EncounterId = encounterId,
                    Pokeball = captureItem,
                    SpawnPointId = spawnpointId,
                    HitPokemon = hitPokemon,
                    NormalizedReticleSize = random.NextDouble() * 1.95D,
                    SpinModifier = random.NextDouble(),
                    NormalizedHitPosition = 1
                }.ToByteString()
            });
            var catchPokemonResponse = CatchPokemonResponse.Parser.ParseFrom(response);

            return catchPokemonResponse;
        }

        /// <summary>
        ///     Throws a capture item to the Pokemon
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <param name="captureItem"></param>
        /// <returns></returns>
        public static async Task<UseItemCaptureResponse> UseCaptureItem(ulong encounterId, string spawnpointId, ItemId captureItem)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemCapture,
                RequestMessage = new UseItemCaptureMessage
                {
                    EncounterId = encounterId,
                    ItemId = captureItem,
                    SpawnPointId = spawnpointId
                }.ToByteString()
            });
            var useItemCaptureResponse = UseItemCaptureResponse.Parser.ParseFrom(response);

            return useItemCaptureResponse;
        }

        /// <summary>
        ///     New method to throws an item to the Pokemon
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="spawnpointId"></param>
        /// <param name="captureItem"></param>
        /// <returns></returns>
        public static async Task<UseItemEncounterResponse> UseItemEncounter(ulong encounterId, string spawnpointId, ItemId captureItem)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemEncounter,
                RequestMessage = new UseItemEncounterMessage
                {
                    EncounterId = encounterId,
                    Item = captureItem,
                    SpawnPointGuid = spawnpointId
                }.ToByteString()
            });
            var useItemEncounterResponse = UseItemEncounterResponse.Parser.ParseFrom(response);

            return useItemEncounterResponse;
        }

        #endregion

        #region Power Up & Evolving & Transfer & Favorite

        /// <summary>
        ///
        /// </summary>
        /// <param name="pokemon"></param>
        /// <returns></returns>
        public static async Task<UpgradePokemonResponse> PowerUpPokemon(PokemonData pokemon)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UpgradePokemon,
                RequestMessage = new UpgradePokemonMessage
                {
                    PokemonId = pokemon.Id
                }.ToByteString()
            });
            var upgradePokemonResponse = UpgradePokemonResponse.Parser.ParseFrom(response);

            return upgradePokemonResponse;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pokemon"></param>
        /// <returns></returns>
        public static async Task<EvolvePokemonResponse> EvolvePokemon(PokemonData pokemon)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.EvolvePokemon,
                RequestMessage = new EvolvePokemonMessage
                {
                    PokemonId = pokemon.Id
                }.ToByteString()
            });
            var evolvePokemonResponse = EvolvePokemonResponse.Parser.ParseFrom(response);

            return evolvePokemonResponse;
        }

        /// <summary>
        /// Transfers the Pokemon
        /// </summary>
        /// <param name="pokemonId"></param>
        /// <returns></returns>
        public static async Task<ReleasePokemonResponse> TransferPokemon(ulong pokemonId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.ReleasePokemon,
                RequestMessage = new ReleasePokemonMessage
                {
                    PokemonId = pokemonId
                }.ToByteString()
            });
            var releasePokemonResponse = ReleasePokemonResponse.Parser.ParseFrom(response);

            return releasePokemonResponse;
        }

        /// <summary>
        /// Transfers multiple Pokemons at once
        /// </summary>
        /// <param name="pokemonIds"></param>
        /// <returns></returns>
        public static async Task<ReleasePokemonResponse> TransferPokemons(ulong[] pokemonIds)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.ReleasePokemon,
                RequestMessage = new ReleasePokemonMessage
                {
                    PokemonIds = { pokemonIds }
                }.ToByteString()
            });
            var releasePokemonResponse = ReleasePokemonResponse.Parser.ParseFrom(response);

            return releasePokemonResponse;
        }

        /// <summary>
        /// Favourites/Unfavourites the Pokemon
        /// </summary>
        /// <param name="pokemonId"></param>
        /// <param name="isFavorite"></param>
        /// <returns></returns>
        public static async Task<SetFavoritePokemonResponse> SetFavoritePokemon(ulong pokemonId, bool isFavorite)
        {
            // Cast ulong to long...
            long pokeId = (long)pokemonId;

            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.SetFavoritePokemon,
                RequestMessage = new SetFavoritePokemonMessage
                {
                    PokemonId = pokeId,
                    IsFavorite = isFavorite
                }.ToByteString()
            });
            var setFavoritePokemonResponse = SetFavoritePokemonResponse.Parser.ParseFrom(response);

            return setFavoritePokemonResponse;
        }

        public static async Task<SetBuddyPokemonResponse> SetBuddyPokemon(ulong id)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.SetBuddyPokemon,
                RequestMessage = new SetBuddyPokemonMessage
                {
                    PokemonId = id
                }.ToByteString()
            });
            var setBuddyPokemonResponse = SetBuddyPokemonResponse.Parser.ParseFrom(response);

            return setBuddyPokemonResponse;
        }

        public static async Task<NicknamePokemonResponse> SetPokemonNickName(ulong pokemonId, string nickName)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.NicknamePokemon,
                RequestMessage = new NicknamePokemonMessage
                {
                    PokemonId = pokemonId,
                    Nickname = nickName
                }.ToByteString()
            });
            var nicknamePokemonResponse = NicknamePokemonResponse.Parser.ParseFrom(response);

            return nicknamePokemonResponse;
        }

        #endregion

        #endregion

        #region Pokestop Handling

        /// <summary>
        ///     Gets fort data for the given Id
        /// </summary>
        /// <param name="pokestopId"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public static async Task<FortDetailsResponse> GetFort(string pokestopId, double latitude, double longitude)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.FortDetails,
                RequestMessage = new FortDetailsMessage
                {
                    FortId = pokestopId,
                    Latitude = latitude,
                    Longitude = longitude
                }.ToByteString()
            });
            var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(response);

            return fortDetailsResponse;
        }

        /// <summary>
        ///     Searches the given fort
        /// </summary>
        /// <param name="pokestopId"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public static async Task<FortSearchResponse> SearchFort(string pokestopId, double latitude, double longitude)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.FortSearch,
                RequestMessage = new FortSearchMessage
                {
                    FortId = pokestopId,
                    FortLatitude = latitude,
                    FortLongitude = longitude,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var fortSearchResponse = FortSearchResponse.Parser.ParseFrom(response);

            return fortSearchResponse;
        }

        public static async Task<AddFortModifierResponse> AddFortModifier(string pokestopId, ItemId modifierType)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.AddFortModifier,
                RequestMessage = new AddFortModifierMessage
                {
                    FortId = pokestopId,
                    ModifierType = modifierType,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var addFortModifierResponse = AddFortModifierResponse.Parser.ParseFrom(response);

            return addFortModifierResponse;
        }
        #endregion

        #region Gym Handling

        /// <summary>
        ///     Gets the details for the given Gym
        /// </summary>
        /// <param name="gymid"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public static async Task<GetGymDetailsResponse> GetGymDetails(string gymid, double latitude, double longitude)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.GetGymDetails,
                RequestMessage = new GetGymDetailsMessage
                {
                    GymId = gymid,
                    GymLatitude = latitude,
                    GymLongitude = longitude,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var getGymDetailsResponse = GetGymDetailsResponse.Parser.ParseFrom(response);

            return getGymDetailsResponse;
        }

        /// <summary>
        ///     Deploys a pokemon to the given Gym
        /// </summary>
        /// <param name="fortId"></param>
        /// <param name="pokemonId"></param>
        /// <returns></returns>
        public static async Task<FortDeployPokemonResponse> FortDeployPokemon(string fortId, ulong pokemonId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.FortDeployPokemon,
                RequestMessage = new FortDeployPokemonMessage
                {
                    PokemonId = pokemonId,
                    FortId = fortId,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var fortDeployPokemonResponse = FortDeployPokemonResponse.Parser.ParseFrom(response);

            return fortDeployPokemonResponse;
        }

        /// <summary>
        ///     Start a gym battle using a set of pokemons
        /// </summary>
        /// <param name="gymId"></param>
        /// <param name="defendingPokemonId"></param>
        /// <param name="attackingPokemonIds"></param>
        /// <returns></returns>
        public static async Task<StartGymBattleResponse> StartGymBattle(string gymId, ulong defendingPokemonId, IEnumerable<ulong>attackingPokemonIds)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.StartGymBattle,
                RequestMessage = new StartGymBattleMessage
                {
                    GymId = gymId,
                    DefendingPokemonId = defendingPokemonId,
                    AttackingPokemonIds = {attackingPokemonIds},
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var startGymBattleResponse = StartGymBattleResponse.Parser.ParseFrom(response);

            return startGymBattleResponse;
        }

        public static async Task<AttackGymResponse> AttackGym(string gymId, string battleId, List<BattleAction> battleActions, BattleAction lastRetrievedAction)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.AttackGym,
                RequestMessage = new AttackGymMessage
                {
                    GymId = gymId,
                    BattleId = battleId,
                    AttackActions = {battleActions},
                    LastRetrievedAction = lastRetrievedAction,
                    PlayerLatitude = _session.Player.Latitude,
                    PlayerLongitude = _session.Player.Longitude
                }.ToByteString()
            });
            var attackGymResponse = AttackGymResponse.Parser.ParseFrom(response);

            return attackGymResponse;
        }

        /// The following _client.Fort methods need implementation:
        /// FortRecallPokemon -> Do we need to implement this? Pokemons can't be recalled by the player, do they?

        #endregion

        #region Items Handling

        /// <summary>
        ///     Uses the given incense item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async Task<UseIncenseResponse> UseIncense(ItemId item)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseIncense,
                RequestMessage = new UseIncenseMessage
                {
                    IncenseType = item
                }.ToByteString()
            });
            var useIncenseResponse = UseIncenseResponse.Parser.ParseFrom(response);

            return useIncenseResponse;
        }

        /// <summary>
        ///     Uses the given XpBoost item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async Task<UseItemXpBoostResponse> UseXpBoost(ItemId item)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemXpBoost,
                RequestMessage = new UseItemXpBoostMessage
                {
                    ItemId = item
                }.ToByteString()
            });
            var useItemXpBoostResponse = UseItemXpBoostResponse.Parser.ParseFrom(response);

            return useItemXpBoostResponse;
        }

        public static async Task<UseItemReviveResponse> UseItemRevive(ItemId item, ulong pokemonId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemRevive,
                RequestMessage = new UseItemReviveMessage
                {
                    PokemonId = pokemonId,
                    ItemId = item
                }.ToByteString()
            });
            var useItemReviveResponse = UseItemReviveResponse.Parser.ParseFrom(response);

            return useItemReviveResponse;
        }

        public static async Task<UseItemPotionResponse> UseItemPotion(ItemId item, ulong pokemonId)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemPotion,
                RequestMessage = new UseItemPotionMessage
                {
                    PokemonId = pokemonId,
                    ItemId = item
                }.ToByteString()
            });
            var useItemPotionResponse = UseItemPotionResponse.Parser.ParseFrom(response);

            return useItemPotionResponse;
        }

        /// <summary>
        ///     Recycles the given amount of the selected item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static async Task<RecycleInventoryItemResponse> RecycleItem(ItemId item, int amount)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.RecycleInventoryItem,
                RequestMessage = new RecycleInventoryItemMessage
                {
                    Count = amount,
                    ItemId = item
                }.ToByteString()
            });
            var recycleInventoryItemResponse = RecycleInventoryItemResponse.Parser.ParseFrom(response);

            return recycleInventoryItemResponse;
        }

        #endregion

        #region Eggs Handling

        /// <summary>
        ///     Uses the selected incubator on the given egg
        /// </summary>
        /// <param name="incubator"></param>
        /// <param name="egg"></param>
        /// <returns></returns>
        public static async Task<UseItemEggIncubatorResponse> UseEggIncubator(EggIncubator incubator, PokemonData egg)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.UseItemEggIncubator,
                RequestMessage = new UseItemEggIncubatorMessage
                {
                    ItemId = incubator.Id,
                    PokemonId = egg.Id
                }.ToByteString()
            });
            var useItemEggIncubatorResponse = UseItemEggIncubatorResponse.Parser.ParseFrom(response);

            return useItemEggIncubatorResponse;
        }

        /// <summary>
        ///     Gets the incubator used by the given egg
        /// </summary>
        /// <param name="egg"></param>
        /// <returns></returns>
        public static EggIncubator GetIncubatorFromEgg(PokemonData egg)
        {
            return IncubatorsInventory.FirstOrDefault(item => item.Id == null ? false : item.Id.Equals(egg.EggIncubatorId));
        }

        #endregion

        #region Download

        public static string DownloadSettingsHash { get; set; }

        public static async Task<DownloadSettingsResponse> DownloadSettings()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.DownloadSettings,
                RequestMessage = new DownloadSettingsMessage
                {
                    Hash = DownloadSettingsHash
                }.ToByteString()
            });
            var downloadSettingsResponse = DownloadSettingsResponse.Parser.ParseFrom(response);
            DownloadSettingsHash = downloadSettingsResponse?.Hash ?? "";

            return downloadSettingsResponse;
        }
        #endregion

        #region Misc

        /// <summary>
        ///     Verifies challenge
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<VerifyChallengeResponse> VerifyChallenge(string token)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.VerifyChallenge,
                RequestMessage = new VerifyChallengeMessage
                {
                    Token = token
                }.ToByteString()
            });
            var verifyChallengeResponse = VerifyChallengeResponse.Parser.ParseFrom(response);

            return verifyChallengeResponse;
        }

        /// <summary>
        ///     Claims codename
        /// </summary>
        /// <param name="codename"></param>
        /// <returns></returns>
        public static async Task<ClaimCodenameResponse> ClaimCodename(string codename)
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.ClaimCodename,
                RequestMessage = new ClaimCodenameMessage
                {
                    Codename = codename
                }.ToByteString()
            });
            var claimCodenameResponse = ClaimCodenameResponse.Parser.ParseFrom(response);

            return claimCodenameResponse;
        }

        /// <summary>
        ///     Sends an echo
        /// </summary>
        /// <returns></returns>
        public static async Task<EchoResponse> SendEcho()
        {
            var response = await _session.RpcClient.SendRemoteProcedureCallAsync(new Request
            {
                RequestType = RequestType.Echo,
                RequestMessage = new EchoMessage
                {
                }.ToByteString()
            });
            var echoResponse = EchoResponse.Parser.ParseFrom(response);

            return echoResponse;
        }
        #endregion

        #endregion
    }
}
