using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Newtonsoft.Json;
using Google.Protobuf;
using POGOProtos.Data;
using POGOProtos.Data.Gym;
using POGOProtos.Enums;
using PokemonGo_UWP.Utils.Extensions;
using PokemonGo_UWP.Controls;
using Windows.UI.Xaml;

namespace PokemonGo_UWP.ViewModels
{
    public class EnterGymPageViewModel : ViewModelBase
    {
        #region Lifecycle Handlers

        /// <summary>
        /// </summary>
        /// <param name="parameter">FortData containing the Gym that we're visiting</param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode,
            IDictionary<string, object> suspensionState)
        {
            AudioUtils.PlaySound(AudioUtils.BEFORE_THE_FIGHT);

            if (suspensionState.Any())
            {
                // Recovering the state
                CurrentGym = JsonConvert.DeserializeObject<FortDataWrapper>((string)suspensionState[nameof(CurrentGym)]);
                CurrentGymInfo = JsonConvert.DeserializeObject<GetGymDetailsResponse>((string)suspensionState[nameof(CurrentGymInfo)]);
                CurrentGymState = CurrentGymInfo.GymState;
                GymMemberships.Clear();
                foreach (GymMembership membership in CurrentGymState.Memberships)
                {
                    GymMemberships.Add(membership);
                }
                CurrentEnterResponse = JsonConvert.DeserializeObject<GetGymDetailsResponse>((string)suspensionState[nameof(CurrentEnterResponse)]);
                RaisePropertyChanged(() => CurrentGym);
                RaisePropertyChanged(() => CurrentGymInfo);
                RaisePropertyChanged(() => CurrentGymState);
                RaisePropertyChanged(() => GymMemberships);
                RaisePropertyChanged(() => GymLevel);
                RaisePropertyChanged(() => GymPrestigeFull);
                RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                RaisePropertyChanged(() => TrainCommandVisibility);
                RaisePropertyChanged(() => FightCommandVisibility);
                RaisePropertyChanged(() => CurrentEnterResponse);
            }
            else
            {
                if (GameClient.PlayerStats.Level < 5)
                {
                    PlayerLevelInsufficient?.Invoke(this, null);
                }
                else
                {
                    // Navigating from game page, so we need to actually load the Gym
                    Busy.SetBusy(true, "Loading Gym");
                    CurrentGym = (FortDataWrapper)NavigationHelper.NavigationState[nameof(CurrentGym)];
                    NavigationHelper.NavigationState.Remove(nameof(CurrentGym));
                    Logger.Write($"Entering {CurrentGym.Id}");
                    CurrentGymInfo = await GameClient.GetGymDetails(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                    CurrentGymState = CurrentGymInfo.GymState;
                    GymMemberships.Clear();
                    foreach (GymMembership membership in CurrentGymState.Memberships)
                    {
                        GymMemberships.Add(membership);
                    }
                    RaisePropertyChanged(() => GymLevel);
                    RaisePropertyChanged(() => GymPrestigeFull);
                    RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                    RaisePropertyChanged(() => TrainCommandVisibility);
                    RaisePropertyChanged(() => FightCommandVisibility);
                    SelectedGymMember = GymMemberships[0];
                    Busy.SetBusy(false);

                    if (GameClient.PlayerData.Team == POGOProtos.Enums.TeamColor.Neutral)
                    {
                        PlayerTeamUnset?.Invoke(this, null);
                    }
                    else
                    {
                        GymLoaded?.Invoke(this, null);
                    }
                }
            }
        }

        /// <summary>
        ///     Save state before navigating
        /// </summary>
        /// <param name="suspensionState"></param>
        /// <param name="suspending"></param>
        /// <returns></returns>
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {
            AudioUtils.StopSound(AudioUtils.BEFORE_THE_FIGHT);

            if (suspending)
            {
                suspensionState[nameof(CurrentGym)] = JsonConvert.SerializeObject(CurrentGym);
                suspensionState[nameof(CurrentGymInfo)] = JsonConvert.SerializeObject(CurrentGymInfo);
                suspensionState[nameof(CurrentEnterResponse)] = JsonConvert.SerializeObject(CurrentEnterResponse);
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Game Management Vars

        /// <summary>
        ///     Gym that the user is visiting
        /// </summary>
        private FortDataWrapper _currentGym;

        /// <summary>
        ///     Infos on the current Gym
        /// </summary>
        private GetGymDetailsResponse _currentGymInfo;

        /// <summary>
        ///     Results of the current Gym enter
        /// </summary>
        private GetGymDetailsResponse _currentEnterResponse;

        /// <summary>
        ///     Info on the state of the current Gym
        /// </summary>
        private GymState _currentGymState;

        #endregion

        #region Bindable Game Vars

        /// <summary>
        ///     Gym that the user is visiting
        /// </summary>
        public FortDataWrapper CurrentGym
        {
            get { return _currentGym; }
            set { Set(ref _currentGym, value); }
        }

        /// <summary>
        ///     Infos on the current Gym
        /// </summary>
        public GetGymDetailsResponse CurrentGymInfo
        {
            get { return _currentGymInfo; }
            set { Set(ref _currentGymInfo, value); }
        }

        /// <summary>
        ///     Info on the state of the current Gym
        /// </summary>
        public GymState CurrentGymState
        {
            get { return _currentGymState; }
            set { Set(ref _currentGymState, value); }
        }

        /// <summary>
        ///     Results of the current Gym enter
        /// </summary>
        public GetGymDetailsResponse CurrentEnterResponse
        {
            get { return _currentEnterResponse; }
            set { Set(ref _currentEnterResponse, value); }
        }

        /// <summary>
        ///     Collection of Gym Memberships (Pokemons that are deployed to the gym)
        /// </summary>
        public ObservableCollection<GymMembership> GymMemberships { get; set; } = new ObservableCollection<GymMembership>();

        public GymMembership SelectedGymMember { get; set; }

        /// <summary>
        /// Amount of Memberships is the level of the gym
        /// </summary>
        public int GymLevel
        {
            get { return GymMemberships.Count; }
        }

        /// <summary>
        /// The maximum prestige is the amount of memberships * 2000
        /// </summary>
        public int GymPrestigeFull
        {
            get { return GymMemberships.Count * 2000; }
        }

        public Visibility DeployPokemonCommandVisibility
        {
            get { return CurrentGym?.OwnedByTeam == GameClient.PlayerData.Team ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility TrainCommandVisibility
        {
            get { return CurrentGym?.OwnedByTeam == GameClient.PlayerData.Team ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility FightCommandVisibility
        {
            get { return CurrentGym?.OwnedByTeam != GameClient.PlayerData.Team ? Visibility.Visible : Visibility.Collapsed; }
        }

        #endregion

        #region Game Logic

        #region Shared Logic

        private DelegateCommand _returnToGameScreen;

        /// <summary>
        ///     Going back to map page
        /// </summary>
        public DelegateCommand ReturnToGameScreen => _returnToGameScreen ?? (
            _returnToGameScreen =
                new DelegateCommand(
                    () => { NavigationService.Navigate(typeof(GameMapPage), GameMapNavigationModes.GymUpdate); },
                    () => true)
            );

        private DelegateCommand _abandonGym;

        /// <summary>
        ///     Going back to map page
        /// </summary>
        public DelegateCommand AbandonGym => _abandonGym ?? (
            _abandonGym = new DelegateCommand(() =>
            {
                // Re-enable update timer
                GameClient.ToggleUpdateTimer();
                NavigationService.GoBack();
            }, () => true)
            );

        #endregion

        #region Gym Handling

        #region Gym Events

        /// <summary>
        ///     Event fired when the Gym is successfully loaded
        /// </summary>
        public event EventHandler GymLoaded;

        /// <summary>
        ///     Event fired if the user was able to enter the Gym
        /// </summary>
        public event EventHandler EnterSuccess;

        /// <summary>
        ///     Event fired if the user tried to enter a Gym which is out of range
        /// </summary>
        public event EventHandler EnterOutOfRange;

        /// <summary>
        /// <summary>
        ///     Event fired if the Player's inventory is full and he can't get items from the Pokestop
        /// </summary>
        public event EventHandler EnterInventoryFull;

        /// <summary>
        ///     Event fired if the Player is not yet level 5
        /// </summary>
        public event EventHandler PlayerLevelInsufficient;

        /// <summary>
        ///     Event fired if the Player is level 5, but has not yet chosen a team
        /// </summary>
        public event EventHandler PlayerTeamUnset;

        /// <summary>
        ///     Event fired when the Player's team choice is accepted and set
        /// </summary>
        public event EventHandler<TeamColor> PlayerTeamSet;

        /// <summary>
        ///     Event fired when the EnterGymPage has to show the Pokemon selection control
        /// </summary>
        public event EventHandler AskForPokemonSelection;

        /// <summary>
        ///     Event fired when the Selection of a pokemon (to deploy) has been cancelled
        /// </summary>
        public event EventHandler PokemonSelectionCancelled;
        #endregion

        /// <summary>
        ///     Event fired when the Selected pokemon is deployed to the gym
        /// </summary>
        public event EventHandler PokemonDeployed;

        /// <summary>
        ///     Event fired when the deployment of a Pokemon went wrong
        /// </summary>
        public event EventHandler<FortDeployPokemonResponse.Types.Result> DeployPokemonError;

        private DelegateCommand _enterCurrentGym;

        /// <summary>
        ///     Enters the current Gym, don't know what to do then
        /// </summary>
        public DelegateCommand EnterCurrentGym => _enterCurrentGym ?? (
            _enterCurrentGym = new DelegateCommand(async () =>
            {
                Busy.SetBusy(true, "Entering Gym");
                Logger.Write($"Entering {CurrentGymInfo.Name} [ID = {CurrentGym.Id}]");
                CurrentEnterResponse =
                    await GameClient.GetGymDetails(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                Busy.SetBusy(false);
                switch (CurrentEnterResponse.Result)
                {
                    case GetGymDetailsResponse.Types.Result.Unset:
                        break;
                    case GetGymDetailsResponse.Types.Result.Success:
                        // Success, we play the animation and update inventory
                        Logger.Write("Entering Gym success");

                        // What to do when we are in the Gym?
                        EnterSuccess?.Invoke(this, null);
                        await GameClient.UpdateInventory();
                        break;
                    case GetGymDetailsResponse.Types.Result.ErrorNotInRange:
                        // Gym can't be used because it's out of range, there's nothing that we can do
                        Logger.Write("Entering Gym out of range");
                        EnterOutOfRange?.Invoke(this, null);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }, () => true));

        #region Set Player Team
        private DelegateCommand _setPlayerTeam;

        public TeamColor ChosenTeam { get; set; }

        public DelegateCommand SetPlayerTeam => _setPlayerTeam ?? (
            _setPlayerTeam = new DelegateCommand(async () =>
                {
                    Busy.SetBusy(true, "Setting player team");
                    Logger.Write($"Setting player team to { ChosenTeam }");
                    var response = await GameClient.SetPlayerTeam(ChosenTeam);
                    Busy.SetBusy(false);
                    switch (response.Status)
                    {
                        case SetPlayerTeamResponse.Types.Status.Unset:
                            break;
                        case SetPlayerTeamResponse.Types.Status.Success:
                            PlayerTeamSet?.Invoke(this, response.PlayerData.Team);
                            break;
                        case SetPlayerTeamResponse.Types.Status.Failure:
                            break;
                        case SetPlayerTeamResponse.Types.Status.TeamAlreadySet:
                            PlayerTeamSet?.Invoke(this, TeamColor.Yellow);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }));
        #endregion

        #region Deploy Pokemon
        /// <summary>
        /// Reference to Pokemon inventory
        /// </summary>
        public ObservableCollection<PokemonDataWrapper> PokemonInventory { get; private set; } =
            new ObservableCollection<PokemonDataWrapper>();

        /// <summary>
        /// Total amount of Pokemon in players inventory
        /// </summary>
        public int TotalPokemonCount
        {
            get { return PokemonInventory.Count; }
        }

        /// <summary>
        /// Player's profile, we use it just for the maximum ammount of pokemon
        /// </summary>
        private PlayerData _playerProfile;
        public PlayerData PlayerProfile
        {
            get { return _playerProfile; }
            set { Set(ref _playerProfile, value); }
        }

        public PokemonDataWrapper SelectedPokemon { get; set; }

        private DelegateCommand _deployPokemonCommand;

        public DelegateCommand DeployPokemonCommand => _deployPokemonCommand ?? (
            _deployPokemonCommand = new DelegateCommand(() =>
            {
                PokemonInventory = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                    .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                    .SortBySortingmode(CurrentPokemonSortingMode));

                PlayerProfile = GameClient.PlayerData;

                RaisePropertyChanged(() => PokemonInventory);
                RaisePropertyChanged(() => PlayerProfile);

                AskForPokemonSelection?.Invoke(this, null);
            }));

        /// <summary>
        /// Set the Selected pokemon and ask for a confirmation before deploying the pokémon
        /// </summary>
        private DelegateCommand _returnToGymCommand;

        public DelegateCommand ReturnToGymCommand =>
            _returnToGymCommand ?? (
            _returnToGymCommand = new DelegateCommand(() =>
            {
                PokemonSelectionCancelled?.Invoke(this, null);
            }));

        /// <summary>
        /// Set the Selected pokemon and ask for a confirmation before deploying the pokémon
        /// </summary>
        private DelegateCommand<PokemonDataWrapper> _selectPokemonCommand;

        public DelegateCommand<PokemonDataWrapper> SelectPokemonCommand =>
            _selectPokemonCommand ?? (
            _selectPokemonCommand = new DelegateCommand<PokemonDataWrapper>((selectedPokemon) =>
            {
                // Catch if the Pokémon is a Buddy, deploying is not permitted in this case
                // TODO: This isn't a MessageDialog in the original apps, implement error style (Shell needed)
                if (Convert.ToBoolean(selectedPokemon.IsBuddy))
                {
                    var cannotDeployDialog = new PoGoMessageDialog(Utils.Resources.CodeResources.GetString("CannotDeployBuddy"), "")
                    {
                        CoverBackground = true,
                        AnimationType = PoGoMessageDialogAnimation.Bottom
                    };
                    cannotDeployDialog.Show();
                    return;
                }

                SelectPokemon(selectedPokemon);

                // Ask for confirmation before deploying the Pokemons
                var dialog =
                    new PoGoMessageDialog("",
                        string.Format(Utils.Resources.CodeResources.GetString("DeployPokemonWarningText"), SelectedPokemon.Name))
                    {
                        AcceptText = Utils.Resources.CodeResources.GetString("YesText"),
                        CancelText = Utils.Resources.CodeResources.GetString("NoText"),
                        CoverBackground = true,
                        AnimationType = PoGoMessageDialogAnimation.Bottom
                    };

                dialog.AcceptInvoked += async (sender, e) =>
                {
                    // User confirmed deployment
                    try
                    {
                        ServerRequestRunning = true;
                        var fortDeployResponse = await GameClient.FortDeployPokemon(CurrentGym.Id, SelectedPokemon.Id);

                        switch (fortDeployResponse.Result)
                        {
                            case FortDeployPokemonResponse.Types.Result.NoResultSet:
                                break;
                            case FortDeployPokemonResponse.Types.Result.Success:
                                // Remove the deployed pokemon from the inventory on screen
                                PokemonInventory.Remove(SelectedPokemon);
                                RaisePropertyChanged(() => PokemonInventory);

                                // TODO: Implement message informing about success of deployment (Shell needed)
                                await GameClient.UpdateInventory();
                                await GameClient.UpdatePlayerStats();

                                // Reset to gym screen
                                PokemonDeployed?.Invoke(this, null);
                                break;

                            case FortDeployPokemonResponse.Types.Result.ErrorAlreadyHasPokemonOnFort:
                            case FortDeployPokemonResponse.Types.Result.ErrorFortDeployLockout:
                            case FortDeployPokemonResponse.Types.Result.ErrorFortIsFull:
                            case FortDeployPokemonResponse.Types.Result.ErrorNotInRange:
                            case FortDeployPokemonResponse.Types.Result.ErrorOpposingTeamOwnsFort:
                            case FortDeployPokemonResponse.Types.Result.ErrorPlayerBelowMinimumLevel:
                            case FortDeployPokemonResponse.Types.Result.ErrorPlayerHasNoNickname:
                            case FortDeployPokemonResponse.Types.Result.ErrorPlayerHasNoTeam:
                            case FortDeployPokemonResponse.Types.Result.ErrorPokemonIsBuddy:
                            case FortDeployPokemonResponse.Types.Result.ErrorPokemonNotFullHp:
                                DeployPokemonError?.Invoke(this, fortDeployResponse.Result);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    finally
                    {
                        ServerRequestRunning = false;
                    }

                };

                dialog.Show();

            }));

        private void SelectPokemon(PokemonDataWrapper selectedPokemon)
        {
            SelectedPokemon = selectedPokemon;
            RaisePropertyChanged(() => SelectedPokemon);
        }

        /// <summary>
        /// Sorting mode for current Pokemon view
        /// </summary>
        public PokemonSortingModes CurrentPokemonSortingMode
        {
            get { return SettingsService.Instance.PokemonSortingMode; }
            set
            {
                SettingsService.Instance.PokemonSortingMode = value;
                RaisePropertyChanged(() => CurrentPokemonSortingMode);

                // When this changes we need to sort the collection again
                UpdateSorting();
            }
        }

        #region Pokemon Inventory Handling

        /// <summary>
        /// Sort the PokemonInventory with the CurrentPokemonSortingMode 
        /// </summary>
        private void UpdateSorting()
        {
            PokemonInventory =
                new ObservableCollection<PokemonDataWrapper>(PokemonInventory.SortBySortingmode(CurrentPokemonSortingMode));

            RaisePropertyChanged(() => PokemonInventory);
        }

        #endregion

        #endregion

        #endregion

        /// <summary>
        /// Flag for an ongoing server request. Used to disable the controls
        /// </summary>
        private bool _serverRequestRunning;
        public bool ServerRequestRunning
        {
            get { return _serverRequestRunning; }
            set
            {
                Set(ref _serverRequestRunning, value);
                ReturnToGameScreen.RaiseCanExecuteChanged();
                DeployPokemonCommand.RaiseCanExecuteChanged();
                SelectPokemonCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}