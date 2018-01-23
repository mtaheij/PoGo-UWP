using Microsoft.HockeyApp;
using NotificationsExtensions.Tiles;
using POGOProtos.Data;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Template10.Common;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.Phone.Devices.Notification;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using PokemonGo_UWP.Utils.Helpers;
using PokemonGo_UWP.Exceptions;
using POGOProtos.Networking.Responses;
using POGOLib.Official.Exceptions;
using Windows.System.Profile;
using PokemonGo_UWP.Utils.Game;

namespace PokemonGo_UWP
{
    /// Documentation on APIs used in this page:
    /// https://github.com/Windows-XAML/Template10/wiki
    [Bindable]
    sealed partial class App : BootStrapper
    {

        #region Private Members

        /// <summary>
        ///     We use it to notify that we found at least one catchable Pokemon in our area.
        /// </summary>
        private VibrationDevice _vibrationDevice;

        /// <summary>
        ///     Stores the current <see cref="DisplayRequest"/> instance for the app.
        /// </summary>
        private readonly DisplayRequest _displayRequest;

        private readonly Utils.Helpers.ProximityHelper _proximityHelper;

        #endregion

        #region Properties

        /// <summary>
        /// The TileUpdater instance for the app.
        /// </summary>
        public static TileUpdater LiveTileUpdater { get; private set; }

        /// <summary>
        /// Indicator that can be set from Web configuration file
        /// </summary>
        public static bool GymsAreDisabled = false;

        #endregion

        #region Constructor

        public App()
        {
            InitializeComponent();
            SplashFactory = e => new Splash(e);

            // ensure unobserved task exceptions (unawaited async methods returning Task or Task<T>) are handled
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // ensure general app exceptions are handled
            Application.Current.UnhandledException += App_UnhandledException;

            // Init HockeySDK
            if (!string.IsNullOrEmpty(ApplicationKeys.HockeyAppToken))
                HockeyClient.Current.Configure(ApplicationKeys.HockeyAppToken);

            // Set this in the instance constructor to prevent the creation of an unnecessary static constructor.
            _displayRequest = new DisplayRequest();

            // Initialize the Live Tile Updater.
            LiveTileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();

            // Init the proximity helper to turn the screen off when it's in your pocket
            _proximityHelper = new ProximityHelper();
            _proximityHelper.EnableDisplayAutoOff(false);
        }

        #endregion

        #region Event Handlers

        private static async void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            await ExceptionHandler.HandleException(e.Exception);
            // We should be logging these exceptions too so they can be tracked down.
            if (!string.IsNullOrEmpty(ApplicationKeys.HockeyAppToken))
                HockeyClient.Current.TrackException(e.Exception);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            GameClient.CurrentSession.Logger.Error(e.Exception.Message);
            if (!string.IsNullOrEmpty(ApplicationKeys.HockeyAppToken))
                HockeyClient.Current.TrackException(e.Exception);
        }

        private async void NetworkInformationOnNetworkStatusChanged(object sender)
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var tmpNetworkStatus = connectionProfile != null &&
                                  connectionProfile.GetNetworkConnectivityLevel() ==
                                  NetworkConnectivityLevel.InternetAccess;
            await WindowWrapper.Current().Dispatcher.DispatchAsync(() => {
                if (tmpNetworkStatus)
                {
                    GameClient.CurrentSession.Logger.Notice("Network is online");
                    Busy.SetBusy(false);
                }
                else
                {
                    GameClient.CurrentSession.Logger.Notice("Network is offline");
                    Busy.SetBusy(true, Utils.Resources.CodeResources.GetString("WaitingForNetworkText"));
                }
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PokemonsInventory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SettingsService.Instance.LiveTileMode == LiveTileModes.Off) return;
            // Using a Switch here because we might handle other changed events in other ways.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    List<PokemonData> pokemonList = e.NewItems?.Cast<PokemonData>().OrderByDescending(c => c.DisplayCp).ToList();
                    if (pokemonList != null)
                    {
                        UpdateLiveTile(pokemonList);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Vibrates and/or plays a sound when new pokemons are in the area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CatchablePokemons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            if (SettingsService.Instance.IsVibrationEnabled)
                _vibrationDevice?.Vibrate(TimeSpan.FromMilliseconds(500));
            AudioUtils.PlaySound(AudioUtils.POKEMON_FOUND_DING);
        }

        /// <summary>
        ///     And egg has hatched. This can be called from any page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="hatchedEggResponse"></param>
        /// <returns></returns>
        private void GameClient_OnEggHatched(object sender, GetHatchedEggsResponse hatchedEggResponse)
        {
            Task.Run(new Action(async () =>
            {
                for (var i = 0; i < hatchedEggResponse.PokemonId.Count; i++)
                {
                    GameClient.CurrentSession.Logger.Info("Egg Hatched");

                    var currentPokemon = hatchedEggResponse.HatchedPokemon[i];

                    if (currentPokemon == null)
                        continue;

                    await
                        new MessageDialog(string.Format(
                            Utils.Resources.CodeResources.GetString("EggHatchMessage"),
                            currentPokemon.PokemonId, hatchedEggResponse.StardustAwarded[i], hatchedEggResponse.CandyAwarded[i],
                            hatchedEggResponse.ExperienceAwarded[i])).ShowAsyncQueue();

                    if (i == 0)
                    {
                        WindowWrapper.Current().Dispatcher.Dispatch(() =>
                        {
                            BootStrapper.Current.NavigationService.Navigate(typeof(PokemonDetailPage), new SelectedPokemonNavModel()
                            {
                                SelectedPokemonId = currentPokemon.Id.ToString(),
                                ViewMode = PokemonDetailPageViewMode.ReceivedPokemon
                            });
                        });
                    }
                }
            }));
        }
        #endregion

        #region Application Lifecycle

        /// <summary>
        ///     Disable vibration on suspending
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="prelaunchActivated"></param>
        /// <returns></returns>
        public override Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {                        
            GameClient.PokemonsInventory.CollectionChanged -= PokemonsInventory_CollectionChanged;
            GameClient.CatchablePokemons.CollectionChanged -= CatchablePokemons_CollectionChanged;
            GameClient.OnEggHatched -= GameClient_OnEggHatched;

            NetworkInformation.NetworkStatusChanged -= NetworkInformationOnNetworkStatusChanged;            

            if (SettingsService.Instance.IsBatterySaverEnabled)
                _proximityHelper.EnableDisplayAutoOff(false);

            return base.OnSuspendingAsync(s, e, prelaunchActivated);
        }

        public override void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            if (SettingsService.Instance.IsBatterySaverEnabled)
                _proximityHelper.EnableDisplayAutoOff(true);
        }

        /// <summary>
        ///     This runs everytime the app is launched, even after suspension, so we use this to initialize stuff
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task OnInitializeAsync(IActivatedEventArgs args)
        {
            // If we have a phone contract, hide the status bar
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }

            // Enter into full screen mode
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Standard;            

            // Forces the display to stay on while we play
            //_displayRequest.RequestActive();
            WindowWrapper.Current().Window.VisibilityChanged += WindowOnVisibilityChanged;

            // Initialize Map styles
            await MapStyleHelpers.Initialize();

            // Turn the display off when the proximity stuff detects the display is covered (battery saver)
            if (SettingsService.Instance.IsBatterySaverEnabled)
                _proximityHelper.EnableDisplayAutoOff(true);

            // Init vibration device
            if (ApiInformation.IsTypePresent("Windows.Phone.Devices.Notification.VibrationDevice"))
            {
                _vibrationDevice = VibrationDevice.GetDefault();
            }

            // Check for network status
            NetworkInformation.NetworkStatusChanged += NetworkInformationOnNetworkStatusChanged;

            // Respond to changes in inventory and Pokemon in the immediate viscinity.
            GameClient.PokemonsInventory.CollectionChanged += PokemonsInventory_CollectionChanged;
            GameClient.CatchablePokemons.CollectionChanged += CatchablePokemons_CollectionChanged;
            GameClient.OnEggHatched += GameClient_OnEggHatched;

            await Task.CompletedTask;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="startKind"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (!CheckWindowsBuildVersion())
            {
                var dialog = new MessageDialog("The minimum required Windows version for this App is 10.0.14393 (Aniversary Update). PoGo-UWP will exit now");
                await dialog.ShowAsync();
                App.Current.Exit();
            }

            bool forceToMainPage = false;
            // Check for updates (ignore resume)
            if (startKind == StartKind.Launch)
            {
                var latestUpdateInfo = await UpdateManager.IsUpdateAvailable();

                while (latestUpdateInfo == null || latestUpdateInfo.Status == UpdateManager.UpdateStatus.NoInternet)
                {
                    var dialog = new MessageDialog("Do you want try to connect again?", "No internet connection");

                    dialog.Commands.Add(new UICommand(Utils.Resources.CodeResources.GetString("YesText")) { Id = 0 });
                    dialog.Commands.Add(new UICommand(Utils.Resources.CodeResources.GetString("NoText")) { Id = 1 });
                    dialog.DefaultCommandIndex = 0;
                    dialog.CancelCommandIndex = 1;

                    var result = await dialog.ShowAsyncQueue();

                    if ((int)result.Id != 0)
                        App.Current.Exit();
                    else
                        latestUpdateInfo = await UpdateManager.IsUpdateAvailable();
                }

                if (latestUpdateInfo.Status == UpdateManager.UpdateStatus.UpdateAvailable)
                {
                    var dialog =
                        new MessageDialog(string.Format(Utils.Resources.CodeResources.GetString("UpdatedVersionText"),
                            latestUpdateInfo.Version, latestUpdateInfo.Description));

                    dialog.Commands.Add(new UICommand(Utils.Resources.CodeResources.GetString("YesText")) { Id = 0 });
                    dialog.Commands.Add(new UICommand(Utils.Resources.CodeResources.GetString("NoText")) { Id = 1 });
                    dialog.DefaultCommandIndex = 0;
                    dialog.CancelCommandIndex = 1;

                    var result = await dialog.ShowAsyncQueue();

                    if ((int)result.Id == 0)
                    {
                        var t1 = UpdateManager.InstallUpdate();
                        forceToMainPage = true;
                    }
                }
                else if (latestUpdateInfo.Status == UpdateManager.UpdateStatus.UpdateForced)
                {
                    //start forced update
                    var t1 = UpdateManager.InstallUpdate();
                    forceToMainPage = true;
                }
                else if (latestUpdateInfo.Status == UpdateManager.UpdateStatus.NextVersionNotReady)
                {
                    var twoLines = Environment.NewLine + Environment.NewLine;
                    var dialog = new MessageDialog("Niantic has raised the minimum API level above what we have access to, so we've temporarily disabled the app to protect your account." + 
                        twoLines + "DO NOT attempt to bypass this check. Accounts that access lower APIs than the minimum WILL BE BANNED by Niantic." + twoLines + 
                        "An update will be ready soon. Please DO NOT open an issue on GitHub, you are seeing this message because we already know about it, and this is how we're telling you. " +
                        "Thank you for your patience." + twoLines + "- The PoGo-UWP Team");
                    dialog.Commands.Add(new UICommand("OK"));
                    dialog.DefaultCommandIndex = 0;
                    dialog.CancelCommandIndex = 1;

                    var result = await dialog.ShowAsyncQueue();

                    App.Current.Exit();
                }
            }

            var webConfigurationInfo = await WebConfigurationManager.GetWebConfiguration();
            if (webConfigurationInfo != null)
            {
                WebConfigurationInfo wci = (WebConfigurationInfo)webConfigurationInfo;
                GymsAreDisabled = wci.gymsaredisabled;
            }

            AsyncSynchronizationContext.Register();

            // Let the user know when there is no available PokehashKey, it will look like the game 'hangs'
            GameClient.PokehashSleeping += GameClient_PokehashSleeping;

            // See if there is a key for the PokeHash server, ask one from the user if there isn't
            if (String.IsNullOrEmpty(SettingsService.Instance.PokehashAuthKey))
            {
                HockeyClient.Current.TrackPageView("PokehashKeyPage");
                await NavigationService.NavigateAsync(typeof(PokehashKeyPage), GameMapNavigationModes.AppStart);

                return;
            }

            var currentAccessToken = GameClient.GetAccessToken();
            if (currentAccessToken == null || forceToMainPage)
            {
                HockeyClient.Current.TrackPageView("MainPage");
                await NavigationService.NavigateAsync(typeof(MainPage));
                return;
            }
            else
            {
                try
                {
                    await GameClient.InitializeSession();
                    if (GameClient.IsInitialized)
                    {
                        HockeyClient.Current.TrackPageView("GameMapPage");
                        NavigationService.Navigate(typeof(GameMapPage), GameMapNavigationModes.AppStart);
                    }
                }
                catch (PtcLoginException ex)
                {
                    var errorMessage = ex.Message ?? Utils.Resources.CodeResources.GetString("PtcLoginFailed");
                    ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                    dialog.Show();

                    HockeyClient.Current.TrackPageView("MainPage");
                    await NavigationService.NavigateAsync(typeof(MainPage));
                }
                catch (LocationException)
                {
                    ConfirmationDialog dialog = new Views.ConfirmationDialog(Utils.Resources.CodeResources.GetString("CouldNotGetLocation"));
                    dialog.Closed += (ss, ee) => { App.Current.Exit(); };
                    dialog.Show();
                }
                //catch (PokeHashException)
                //{
                //    var errorMessage = Utils.Resources.CodeResources.GetString("PokeHashException");
                //    ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                //    dialog.Closed += (ss, ee) => { Application.Current.Exit(); };
                //    dialog.Show();
                //}
                catch (HashVersionMismatchException ex)
                {
                    var errorMessage = ex.Message + Utils.Resources.CodeResources.GetString("PokeHashVersionMismatch");
                    ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                    dialog.Closed += (ss, ee) => { Application.Current.Exit(); };
                    dialog.Show();
                }
                catch (Exception ex)    // When the PokeHash server returns an error, it is not safe to continue. Ask for another PokeHash Key
                {
                    var errorMessage = ex.Message ?? Utils.Resources.CodeResources.GetString("HashingKeyExpired");
                    ConfirmationDialog dialog = new Views.ConfirmationDialog(errorMessage);
                    dialog.Show();

                    HockeyClient.Current.TrackPageView("PokehashKeyPage");
                    await NavigationService.NavigateAsync(typeof(PokehashKeyPage), GameMapNavigationModes.AppStart);
                }
            }

            await Task.CompletedTask;
        }

        private void GameClient_PokehashSleeping(object sender, int sleepTime)
        {
            var dialog = new MessageDialog($"There is no available Pokehash key found, the game will pause for {sleepTime} milliseconds");
            dialog.Commands.Add(new UICommand("OK"));
            dialog.DefaultCommandIndex = 0;

            var result = dialog.ShowAsyncQueue().Result;
        }

        private void WindowOnVisibilityChanged(object sender, VisibilityChangedEventArgs visibilityChangedEventArgs)
        {
            if (!visibilityChangedEventArgs.Visible)
                _displayRequest.RequestRelease();
            else
                _displayRequest.RequestActive();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pokemonList"></param>
        /// <remarks>
        /// advancedrei: The LiveTileUpdater is on teh App class, so this has to stay here for now. Might refactor later.
        /// </remarks>
        internal static void UpdateLiveTile(IList<PokemonData> pokemonList)
        {
            // Let's run this on a separate thread.
            Task.Run(() => {
                try
                {
                    TileContent tile = null;
                    var images = new List<string>();
                    var mode = SettingsService.Instance.LiveTileMode;

                    // Generate the images list for multi-image modes.
                    if (mode == LiveTileModes.People || mode == LiveTileModes.Photo)
                    {
                        images.AddRange(pokemonList.Select(c => new PokemonDataWrapper(c).ImageFileName));
                    }

                    foreach (ScheduledTileNotification scheduled in LiveTileUpdater.GetScheduledTileNotifications())
                    {
                        LiveTileUpdater.RemoveFromSchedule(scheduled);
                    }

                    if (mode != LiveTileModes.Peek)
                    {
                        LiveTileUpdater.EnableNotificationQueue(true);

                    }
                    else
                    {
                        LiveTileUpdater.EnableNotificationQueue(false);
                        LiveTileUpdater.Clear();
                    }

                    switch (mode)
                    {
                        case LiveTileModes.Off:
                            tile = LiveTileHelper.GetImageTile("Normal");
                            LiveTileUpdater.Update(new TileNotification(tile.GetXml()));
                            break;
                        case LiveTileModes.Official:
                            tile = LiveTileHelper.GetImageTile("Official");
                            LiveTileUpdater.Update(new TileNotification(tile.GetXml()));
                            break;
                        case LiveTileModes.Transparent:
                            tile = LiveTileHelper.GetImageTile("Transparent");
                            LiveTileUpdater.Update(new TileNotification(tile.GetXml()));
                            break;
                        case LiveTileModes.Peek:
                            foreach (PokemonData pokemonData in pokemonList)
                            {
                                if (LiveTileUpdater.GetScheduledTileNotifications().Count >= 300) return;
                                var peekTile = LiveTileHelper.GetPeekTile(new PokemonDataWrapper(pokemonData));
                                var scheduled = new ScheduledTileNotification(peekTile.GetXml(),
                                    DateTimeOffset.Now.AddSeconds((pokemonList.IndexOf(pokemonData) * 30) + 1));
                                LiveTileUpdater.AddToSchedule(scheduled);
                            }
                            break;
                        case LiveTileModes.People:
                            tile = LiveTileHelper.GetPeopleTile(images);
                            LiveTileUpdater.Update(new TileNotification(tile.GetXml()));
                            break;
                        case LiveTileModes.Photo:
                            tile = LiveTileHelper.GetPhotosTile(images);
                            LiveTileUpdater.Update(new TileNotification(tile.GetXml()));
                            break;
                    }
                    if (tile != null)
                    {
                        GameClient.CurrentSession.Logger.Debug(tile.GetContent());
                    }

                }
                catch (Exception ex)
                {
                    GameClient.CurrentSession.Logger.Debug(ex.Message);
                    HockeyClient.Current.TrackException(ex);
                }
            });
        }

        internal bool CheckWindowsBuildVersion()
        {
            // get the system family name
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            string SystemFamily = ai.DeviceFamily;

            // get the system version number
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            string SystemVersion = $"{v1}.{v2}.{v3}.{v4}";

            if (v1 < 10 || (v1 == 10 && v3 < 14393))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
