using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
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
using PokemonGo_UWP.Utils.Helpers;
using POGOLib.Official.Logging;
using POGOProtos.Data.Battle;
using System.Threading;
using POGOLib.Official.Net;
using System.Diagnostics;

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
                CurrentEnterResponse = JsonConvert.DeserializeObject<GetGymDetailsResponse>((string)suspensionState[nameof(CurrentEnterResponse)]);
                RaisePropertyChanged(() => GymLevel);
                RaisePropertyChanged(() => GymPrestigeFull);
                RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                RaisePropertyChanged(() => TrainCommandVisibility);
                RaisePropertyChanged(() => FightCommandVisibility);
                RaisePropertyChanged(() => DeployCommandButtonEnabled);
                RaisePropertyChanged(() => TrainCommandButtonEnabled);
                RaisePropertyChanged(() => BattleCommandButtonEnabled);
                RaisePropertyChanged(() => OutOfRangeMessageBorderVisibility);
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
                    Logger.Info($"Entering {CurrentGym.Id}");
                    CurrentGymInfo = await GameClient.GetGymDetails(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                    CurrentGymState = CurrentGymInfo.GymState;
                    RaisePropertyChanged(() => GymLevel);
                    RaisePropertyChanged(() => GymPrestigeFull);
                    RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                    RaisePropertyChanged(() => TrainCommandVisibility);
                    RaisePropertyChanged(() => FightCommandVisibility);
                    RaisePropertyChanged(() => DeployCommandButtonEnabled);
                    RaisePropertyChanged(() => TrainCommandButtonEnabled);
                    RaisePropertyChanged(() => BattleCommandButtonEnabled);
                    RaisePropertyChanged(() => OutOfRangeMessageBorderVisibility);
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

            await Task.CompletedTask;
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
        /// Amount of Memberships determines the level of the gym
        /// </summary>
        public int GymLevel
        {
            get
            {
                if (CurrentGymState == null) return 0;
                if (CurrentGymState.FortData == null) return 0;

                long points = CurrentGymState.FortData.GymPoints;
                if (points < 2000) return 1;
                if (points < 4000) return 2;
                if (points < 8000) return 3;
                if (points < 12000) return 4;
                if (points < 16000) return 5;
                if (points < 20000) return 6;
                if (points < 30000) return 7;
                if (points < 40000) return 8;
                if (points < 50000) return 9;
                return 10;
            }
        }

        /// <summary>
        /// The maximum prestige is determined from the number of memberships
        /// </summary>
        public int GymPrestigeFull
        {
            get
            {
                int level = GymLevel;

                if (level == 1) return 2000;
                if (level == 2) return 4000;
                if (level == 3) return 8000;
                if (level == 4) return 12000;
                if (level == 5) return 16000;
                if (level == 6) return 20000;
                if (level == 7) return 30000;
                if (level == 8) return 40000;
                if (level == 9) return 50000;
                if (level == 10) return 52000;
                return 0;
            }
        }

        private GymMembership _selectedGymMember;
        public GymMembership SelectedGymMember
        {
            get { return _selectedGymMember; }
        }

        public Visibility DeployPokemonCommandVisibility
        {
            get
            {
                if (CurrentGym?.OwnedByTeam != GameClient.PlayerData.Team) return Visibility.Collapsed;
                if (CurrentGymState.DeployLockout) return Visibility.Collapsed;
                
                return Visibility.Visible;
            }
        }

        public Visibility TrainCommandVisibility
        {
            get { return CurrentGym?.OwnedByTeam == GameClient.PlayerData.Team ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility FightCommandVisibility
        {
            get { return CurrentGym?.OwnedByTeam != GameClient.PlayerData.Team ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool DeployCommandButtonEnabled
        {
            get
            {
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                if (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) return false;

                if (GameClient.GetDeployedPokemons().Any(a => a.DeployedFortId.Equals(CurrentGym.Id))) return false;

                if (CurrentGym.OwnedByTeam == TeamColor.Neutral) return true;

                if (CurrentGym.OwnedByTeam == GameClient.PlayerData.Team &&
                    CurrentGym.GymPoints < GymPrestigeFull) return true;

                return false;
            }
        }

        public bool TrainCommandButtonEnabled
        {
            get
            {
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                if (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) return false;

                bool isDeployed = GameClient.GetDeployedPokemons().Count() > 0 ? GameClient.GetDeployedPokemons().Any(a => a.DeployedFortId.Equals(CurrentGym.Id)) : false;
                if (GymLevel > CurrentGymState.Memberships.Count && !isDeployed)
                    return true;

                if (CurrentGym.OwnedByTeam != GameClient.PlayerData.Team) return false;

                return true;
            }
        }

        public bool BattleCommandButtonEnabled
        {
            get
            {
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                if (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) return false;

                if (CurrentGym.OwnedByTeam == GameClient.PlayerData.Team) return false;

                return true;
            }
        }

        public Visibility OutOfRangeMessageBorderVisibility
        {
            get
            {
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);

                return (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ObservableCollection<GymMembership> GymMemberships
        {
            get
            {
                ObservableCollection<GymMembership> memberships = new ObservableCollection<GymMembership>();
                if (CurrentGymState == null) return memberships;
                foreach (GymMembership membership in CurrentGymState.Memberships)
                {
                    memberships.Add(membership);
                }
                return memberships;
            }
        }

        public PokemonData UltimatePokemon
        {
            get
            {
                PokemonData ultimatePokemon = new PokemonData();
                if (CurrentGymState != null)
                {
                    if (CurrentGymState.Memberships != null && CurrentGymState.Memberships.Count > 0)
                    {
                        GymMembership lastMembership = CurrentGymState.Memberships[CurrentGymState.Memberships.Count - 1];
                        ultimatePokemon = lastMembership.PokemonData;
                    }
                }
                return ultimatePokemon;
            }
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
        public event EventHandler<SelectionTarget> AskForPokemonSelection;

        /// <summary>
        ///     Event fired when the player wants to train in a Gym
        /// </summary>
        public event EventHandler AskForTrainingAttackTeam;

        /// <summary>
        ///     Event fired when the Selection of a pokemon (to deploy) has been cancelled
        /// </summary>
        public event EventHandler PokemonSelectionCancelled;
        #endregion

        /// <summary>
        ///     Event fired when the Selection of the attacking team has been cancelled
        /// </summary>
        public event EventHandler AttackTeamSelectionClosed;

        /// <summary>
        ///     Event fired when the Selected pokemon is deployed to the gym
        /// </summary>
        public event EventHandler PokemonDeployed;

        /// <summary>
        ///     Event fired when the deployment of a Pokemon went wrong
        /// </summary>
        public event EventHandler<FortDeployPokemonResponse.Types.Result> DeployPokemonError;

        /// <summary>
        ///     Event fired when the train command failed
        /// </summary>
        public event EventHandler<string> TrainError;

        /// <summary>
        ///     Event fired when a battle has started
        /// </summary>
        public event EventHandler BattleStarted;

        /// <summary>
        ///     Event fired when a battle has ended
        /// </summary>
        public event EventHandler BattleEnded;


        private DelegateCommand _enterCurrentGym;

        /// <summary>
        ///     Enters the current Gym, don't know what to do then
        /// </summary>
        public DelegateCommand EnterCurrentGym => _enterCurrentGym ?? (
            _enterCurrentGym = new DelegateCommand(async () =>
            {
                Busy.SetBusy(true, "Entering Gym");
                Logger.Info($"Entering {CurrentGymInfo.Name} [ID = {CurrentGym.Id}]");
                CurrentEnterResponse =
                    await GameClient.GetGymDetails(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                Busy.SetBusy(false);
                switch (CurrentEnterResponse.Result)
                {
                    case GetGymDetailsResponse.Types.Result.Unset:
                        break;
                    case GetGymDetailsResponse.Types.Result.Success:
                        // Success, we play the animation and update inventory
                        Logger.Info("Entering Gym success");

                        // What to do when we are in the Gym?
                        EnterSuccess?.Invoke(this, null);
                        //GameClient.UpdateInventory();
                        break;
                    case GetGymDetailsResponse.Types.Result.ErrorNotInRange:
                        // Gym can't be used because it's out of range, there's nothing that we can do
                        Logger.Info("Entering Gym out of range");
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
                    Logger.Info($"Setting player team to { ChosenTeam }");
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
                            PlayerTeamSet?.Invoke(this, GameClient.PlayerData.Team);
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

        public ObservableCollection<PokemonDataWrapper> AttackTeamMembers { get; private set; } =
            new ObservableCollection<PokemonDataWrapper>();

        /// <summary>
        /// Total amount of Pokemon in players inventory
        /// </summary>
        public int TotalPokemonCount
        {
            get { return PokemonInventory.Count; }
        }

        /// <summary>
        /// Player's profile, we use it just for the maximum amount of pokemon
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

                selectionTarget = SelectionTarget.SelectForDeploy;

                AskForPokemonSelection?.Invoke(this, selectionTarget);
            }));

        #endregion

        #region Train
        private DelegateCommand _trainCommand;

        public DelegateCommand TrainCommand => _trainCommand ?? (
            _trainCommand = new DelegateCommand(() =>
            {
                var defenders = CurrentGymState.Memberships.Select(x => x.PokemonData).ToList();

                if (defenders.Count < 1)
                    return;

                if (CurrentGymState.FortData.IsInBattle)
                {
                    TrainError?.Invoke(this, "GymUnderAttack");
                    return;
                }

                // If no team was selected before take 6 pokemons from the inventory with the highest CP value
                if (AttackTeamMembers.Count == 0)
                {
                    AttackTeamMembers = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                        .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                        .SortByCp()
                        .Take(6));
                }

                RaisePropertyChanged(() => AttackTeamMembers);

                AskForTrainingAttackTeam?.Invoke(this, null);
            }));

        private PokemonDataWrapper _attackTeamMemberToRemove;
        private DelegateCommand<PokemonDataWrapper> _teamSelectorSwitchCommand;

        public DelegateCommand<PokemonDataWrapper> TeamSelectorSwitchCommand => _teamSelectorSwitchCommand ?? (
            _teamSelectorSwitchCommand = new DelegateCommand<PokemonDataWrapper>((pokemonToRemoveFromTeam) =>
            {
                // Select all available Pokemons, without the current team setup
                PokemonInventory = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                    .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                    .SortByCp()
                    .Except(AttackTeamMembers));

                foreach (PokemonDataWrapper attackMember in AttackTeamMembers)
                {
                    foreach (PokemonDataWrapper availablePokemon in PokemonInventory)
                    {
                        if (availablePokemon.Id == attackMember.Id)
                        {
                            PokemonInventory.Remove(availablePokemon);
                            break;
                        }
                    }
                }

                PlayerProfile = GameClient.PlayerData;

                RaisePropertyChanged(() => PokemonInventory);
                RaisePropertyChanged(() => PlayerProfile);

                _attackTeamMemberToRemove = pokemonToRemoveFromTeam;

                selectionTarget = SelectionTarget.SelectForTeamChange;

                AskForPokemonSelection?.Invoke(this, selectionTarget);

            }));


        private Stopwatch _battleStopwatch;
        private string _currentBattleId;
        private long _currentBattleStart;
        private long _currentBattleEnd;

        private BattleParticipant _currentAttacker;
        public BattleParticipant CurrentAttacker
        {
            get { return _currentAttacker; }
            set { Set(ref _currentAttacker, value); }
        }
        private BattlePokemonInfo _currentAttackerBattlePokemon;
        public BattlePokemonInfo CurrentAttackerBattlePokemon
        {
            get { return _currentAttackerBattlePokemon; }
            set { Set(ref _currentAttackerBattlePokemon, value); }
        }
        private PokemonDataWrapper _currentAttackerPokemon;
        public PokemonDataWrapper CurrentAttackerPokemon
        {
            get { return _currentAttackerPokemon; }
            set { Set(ref _currentAttackerPokemon, value); }
        }

        private BattleParticipant _currentDefender;
        public BattleParticipant CurrentDefender
        {
            get { return _currentDefender; }
            set { Set(ref _currentDefender, value); }
        }
        private BattlePokemonInfo _currentDefenderBattlePokemon;
        public BattlePokemonInfo CurrentDefenderBattlePokemon
        {
            get { return _currentDefenderBattlePokemon; }
            set { Set(ref _currentDefenderBattlePokemon, value); }
        }
        private PokemonDataWrapper _currentDefenderPokemon;
        public PokemonDataWrapper CurrentDefenderPokemon
        {
            get { return _currentDefenderPokemon; }
            set { Set(ref _currentDefenderPokemon, value); }
        }

        private BattleLog _battleLogEntries;
        public BattleLog BattleLogEntries
        {
            get { return _battleLogEntries; }
            set { Set(ref _battleLogEntries, value); }
        }

        private string _currentDefendType;
        public string CurrentDefendType
        {
            get { return _currentDefendType; }
            set { Set(ref _currentDefendType, value); }
        }

        private string _currentAttackType;
        public string CurrentAttackType
        {
            get { return _currentAttackType; }
            set { Set(ref _currentAttackType, value); }
        }

        public long RemainingBattleTime
        {
            get
            {
                if (_battleStopwatch != null)
                    return (_currentBattleEnd - _currentBattleStart - _battleStopwatch.ElapsedMilliseconds) / 1000;
                else
                    return 0;
            }
        }

        private BattleLog _currentBattleLog;

        private DelegateCommand _goCommand;

        public DelegateCommand GoCommand => _goCommand ?? (
            _goCommand = new DelegateCommand(async() =>
            {
                try
                {
                    ServerRequestRunning = true;

                    List<ulong> attackingPokemonIds = new List<ulong>();
                    foreach (PokemonDataWrapper attackMember in AttackTeamMembers)
                    {
                        attackingPokemonIds.Add(attackMember.Id);
                    }

                    StartGymBattleResponse startGymBattleResponse = await GameClient.StartGymBattle(CurrentGym.Id, CurrentGymState.Memberships[0].TrainingPokemon.Id, attackingPokemonIds);

                    switch (startGymBattleResponse.Result)
                    {
                        case StartGymBattleResponse.Types.Result.Unset:
                            break;
                        case StartGymBattleResponse.Types.Result.Success:
                            _currentBattleId = startGymBattleResponse.BattleId;
                            CurrentAttacker = startGymBattleResponse.Attacker;
                            CurrentAttackerBattlePokemon = CurrentAttacker.ActivePokemon;
                            CurrentAttackerPokemon = new PokemonDataWrapper(CurrentAttackerBattlePokemon.PokemonData);
                            CurrentDefender = startGymBattleResponse.Defender;
                            CurrentDefenderBattlePokemon = CurrentDefender.ActivePokemon;
                            CurrentDefenderPokemon = new PokemonDataWrapper(CurrentDefenderBattlePokemon.PokemonData);
                            _currentBattleStart = startGymBattleResponse.BattleStartTimestampMs;
                            _currentBattleEnd = startGymBattleResponse.BattleEndTimestampMs;
                            BattleLogEntries = new BattleLog();
                            var actions = startGymBattleResponse.BattleLog.BattleActions.OrderBy(b => b.ActionStartMs);
                            foreach (BattleAction action in actions)
                            {
                                BattleLogEntries.BattleActions.Add(action);
                            }

                            _battleStopwatch = new Stopwatch();
                            _battleStopwatch.Start();
                            BattleStarted?.Invoke(this, null);
                            AudioUtils.PlaySound(AudioUtils.BATTLE);

                            _battleTrackerCancellation = new CancellationTokenSource();
                            await RunBattle();
                            break;
                    }
                }
                finally
                {
                    ServerRequestRunning = false;
                }
            }));

        private DelegateCommand _abandonAttackCommand;

        public DelegateCommand AbandonAttackCommand => _abandonAttackCommand ?? (
            _abandonAttackCommand = new DelegateCommand(() =>
            {
                _battleTrackerCancellation?.Cancel();
            }));

        #endregion

        #region Fight
        private DelegateCommand _fightCommand;

        public DelegateCommand FightCommand => _fightCommand ?? (
            _fightCommand = new DelegateCommand(() =>
            {
                // TODO: Implement Fight
                ConfirmationDialog dialog = new ConfirmationDialog("Fighting is not implemented yet.");
                dialog.Show();
            }));

        // Close the attack team selection panel
        private DelegateCommand _closeAttackTeamSelectionCommand;
        public DelegateCommand CloseAttackTeamSelectionCommand =>
            _closeAttackTeamSelectionCommand ?? (
            _closeAttackTeamSelectionCommand = new DelegateCommand(() =>
            {
                AttackTeamSelectionClosed?.Invoke(this, null);
            }));

        // Close the pokemon selection page, return to gym or to the team selection dialog
        private DelegateCommand _closePokemonSelectCommand;

        public DelegateCommand ClosePokemonSelectCommand =>
            _closePokemonSelectCommand ?? (
            _closePokemonSelectCommand = new DelegateCommand(() =>
            {
                PokemonSelectionCancelled?.Invoke(this, null);
                RaisePropertyChanged(() => SelectedPokemon);
            }));

        public enum SelectionTarget
        {
            SelectForDeploy,
            SelectForTeamChange
        }

        private SelectionTarget selectionTarget;

        /// <summary>
        /// Set the Selected pokemon and ask for a confirmation before deploying the pokémon
        /// </summary>
        private DelegateCommand<PokemonDataWrapper> _selectPokemonCommand;

        public DelegateCommand<PokemonDataWrapper> SelectPokemonCommand =>
            _selectPokemonCommand ?? (
            _selectPokemonCommand = new DelegateCommand<PokemonDataWrapper>((selectedPokemon) =>
            {
                if (selectionTarget == SelectionTarget.SelectForDeploy)
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
                                    // Remove the deployed pokemon from the inventory on screen and update the Gymstate
                                    PokemonInventory.Remove(SelectedPokemon);
                                    CurrentGymState = fortDeployResponse.GymState;

                                    RaisePropertyChanged(() => PokemonInventory);
                                    RaisePropertyChanged(() => CurrentGymState);
                                    RaisePropertyChanged(() => GymLevel);
                                    RaisePropertyChanged(() => GymPrestigeFull);
                                    RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                                    RaisePropertyChanged(() => TrainCommandVisibility);
                                    RaisePropertyChanged(() => FightCommandVisibility);
                                    RaisePropertyChanged(() => DeployCommandButtonEnabled);
                                    RaisePropertyChanged(() => OutOfRangeMessageBorderVisibility);
                                    RaisePropertyChanged(() => GymMemberships);

                                    // TODO: Implement message informing about success of deployment (Shell needed)
                                    //GameClient.UpdateInventory();
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
                }
                else if (selectionTarget == SelectionTarget.SelectForTeamChange)
                {
                    var newSelected = selectedPokemon;

                    AttackTeamMembers.Remove(_attackTeamMemberToRemove);
                    AttackTeamMembers.Add(selectedPokemon);
                    AttackTeamMembers.SortByCp();
                    RaisePropertyChanged(() => AttackTeamMembers);
                    ClosePokemonSelectCommand.Execute();
                }

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
                TrainCommand.RaiseCanExecuteChanged();
                FightCommand.RaiseCanExecuteChanged();
                SelectPokemonCommand.RaiseCanExecuteChanged();
            }
        }

        public event EventHandler ActionAttack;
        public event EventHandler ActionSpecialAttack;

        /// <summary>
        ///     Determines whether we can keep battling.
        /// </summary>
        private CancellationTokenSource _battleTrackerCancellation;

        private async Task RunBattle()
        {
            while (!_battleTrackerCancellation.IsCancellationRequested && RemainingBattleTime >=0)
            {
                RaisePropertyChanged(() => RemainingBattleTime);


                if (BattleLogEntries.BattleActions.Count() > 0)
                {
                    BattleAction action = BattleLogEntries.BattleActions[0];

                    Logger.Debug("Found action: " + action.Type.ToString());
                    BattleLogEntries.BattleActions.RemoveAt(0);

                    switch (action.Type)
                    {
                        case BattleActionType.ActionAttack:
                            CurrentDefendType = "Attack!";
                            ActionAttack?.Invoke(this, null);
                            break;
                        case BattleActionType.ActionSpecialAttack:
                            CurrentDefendType = "Special Attack!";
                            ActionSpecialAttack?.Invoke(this, null);
                            break;
                        case BattleActionType.ActionDodge:
                            break;
                        case BattleActionType.ActionFaint:
                            break;
                        case BattleActionType.ActionDefeat:
                            break;
                        case BattleActionType.ActionVictory:
                            break;
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), _battleTrackerCancellation.Token);
                }
                // cancelled
                catch (OperationCanceledException)
                {
                    break;
                }

            }

            await Task.CompletedTask;

            AttackTeamSelectionClosed?.Invoke(this, null);
            BattleEnded?.Invoke(this, null);
            AudioUtils.StopSounds();
            AudioUtils.PlaySound(AudioUtils.BEFORE_THE_FIGHT);
        }
        #endregion
    }
}