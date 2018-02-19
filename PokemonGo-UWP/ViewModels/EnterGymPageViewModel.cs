using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using POGOProtos.Networking.Responses;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Newtonsoft.Json;
using POGOProtos.Data;
using POGOProtos.Data.Gym;
using POGOProtos.Enums;
using PokemonGo_UWP.Utils.Extensions;
using PokemonGo_UWP.Controls;
using Windows.UI.Xaml;
using PokemonGo_UWP.Utils.Helpers;
using POGOProtos.Data.Battle;
using System.Threading;
using System.Diagnostics;
using POGOProtos.Map.Fort;
using Microsoft.HockeyApp;

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
                CurrentGymInfo = JsonConvert.DeserializeObject<GymGetInfoResponse>((string)suspensionState[nameof(CurrentGymInfo)]);
                CurrentGymStatusAndDefenders = CurrentGymInfo.GymStatusAndDefenders;
                CurrentEnterResponse = JsonConvert.DeserializeObject<GymGetInfoResponse>((string)suspensionState[nameof(CurrentEnterResponse)]);
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
                    if (App.GymsAreDisabled)
                    {
                        GymsAreDisabled?.Invoke(this, null);
                    }
                    else
                    {
                        // Navigating from game page, so we need to actually load the Gym
                        Busy.SetBusy(true, "Loading Gym");
                        CurrentGym = (FortDataWrapper)NavigationHelper.NavigationState[nameof(CurrentGym)];
                        NavigationHelper.NavigationState.Remove(nameof(CurrentGym));
                        GameClient.CurrentSession.Logger.Info($"Entering {CurrentGym.Id}");
                        CurrentGymInfo = await GameClient.GymGetInfo(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                        CurrentGymStatusAndDefenders = CurrentGymInfo.GymStatusAndDefenders;
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

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Gym that the user is visiting
        /// </summary>
        private FortDataWrapper _currentGym;

        /// <summary>
        ///     Infos on the current Gym
        /// </summary>
        private GymGetInfoResponse _currentGymInfo;

        /// <summary>
        ///     Results of the current Gym enter
        /// </summary>
        private GymGetInfoResponse _currentEnterResponse;

        /// <summary>
        ///     Info on the state and defenders of the current Gym
        /// </summary>
        private GymStatusAndDefenders _currentGymStatusAndDefenders;

        /// <summary>
        ///     Info on the state of the current Gym
        ///     TODO: Remove after deploy has changed
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
        public GymGetInfoResponse CurrentGymInfo
        {
            get { return _currentGymInfo; }
            set { Set(ref _currentGymInfo, value); }
        }

        /// <summary>
        ///     Info on the state and defenders of the current Gym
        /// </summary>
        public GymStatusAndDefenders CurrentGymStatusAndDefenders
        {
            get { return _currentGymStatusAndDefenders; }
            set { Set(ref _currentGymStatusAndDefenders, value); }
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
        public GymGetInfoResponse CurrentEnterResponse
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
                if (CurrentGymStatusAndDefenders == null) return 0;
                if (CurrentGymStatusAndDefenders.PokemonFortProto == null) return 0;

                long points = CurrentGymStatusAndDefenders.PokemonFortProto.GymDisplay.TotalGymCp;
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

        public long NewGymPoints { get;set; }

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

        private GymMembership _selectedGymMembership;
        public GymMembership SelectedGymMembership
        {
            get { return _selectedGymMembership; }
            set { _selectedGymMembership = value; }
        }

        public Visibility DeployPokemonCommandVisibility
        {
            get
            {
                if (CurrentGym?.OwnedByTeam != GameClient.PlayerData.Team) return Visibility.Collapsed;
                if (CurrentGymStatusAndDefenders.PokemonFortProto.DeployLockoutEndMs > 0) return Visibility.Collapsed;
                
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
                // TODO: Re-enable
                return false;
                /*
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                if (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) return false;

                bool isDeployed = GameClient.GetDeployedPokemons().Count() > 0 ? GameClient.GetDeployedPokemons().Any(a => a.DeployedFortId.Equals(CurrentGym.Id)) : false;
                if (GymLevel > CurrentGymStatusAndDefenders.GymDefender.Count && !isDeployed)
                    return true;

                if (CurrentGym.OwnedByTeam != GameClient.PlayerData.Team) return false;

                return true;
                */
            }
        }

        public bool BattleCommandButtonEnabled
        {
            get
            {
                // TODO: Re-enable
                return false;
                /*
                var distance = GeoHelper.Distance(CurrentGym?.Geoposition, LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                if (distance > GameClient.GameSetting.FortSettings.InteractionRangeMeters) return false;

                if (CurrentGym.OwnedByTeam == GameClient.PlayerData.Team) return false;

                return true;
                */
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

        public ObservableCollection<GymDefender> GymDefenders
        {
            get
            {
                ObservableCollection<GymDefender> defenders = new ObservableCollection<GymDefender>();
                if (CurrentGymStatusAndDefenders == null) return defenders;
                foreach (GymDefender defender in CurrentGymStatusAndDefenders.GymDefender)
                {
                    defenders.Add(defender);
                }
                return defenders;
            }
        }

        public PokemonData UltimatePokemon
        {
            get
            {
                PokemonData ultimatePokemon = new PokemonData();
                if (CurrentGymStatusAndDefenders != null)
                {
                    if (CurrentGymStatusAndDefenders.GymDefender != null && CurrentGymStatusAndDefenders.GymDefender.Count > 0)
                    {
                        GymDefender lastDefender = CurrentGymStatusAndDefenders.GymDefender[CurrentGymStatusAndDefenders.GymDefender.Count - 1];
                        ultimatePokemon = lastDefender.MotivatedPokemon.Pokemon;
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
                new DelegateCommand(() => 
                {
                    HockeyClient.Current.TrackPageView("GameMapPage");
                    NavigationService.Navigate(typeof(GameMapPage), GameMapNavigationModes.GymUpdate);
                },
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
                HockeyClient.Current.TrackEvent("GoBack from EnterGymPage");
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
        ///     Event fired if the user tried to enter a Gym which is disabled
        /// </summary>
        public event EventHandler EnterDisabled;

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
        ///     Event fired if the Gyms are disabled
        /// </summary>
        public event EventHandler GymsAreDisabled;

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
        ///     Event fired when the player wants to train/battle in a Gym
        /// </summary>
        public event EventHandler AskForAttackTeam;

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
        public event EventHandler<GymDeployResponse.Types.Result> DeployPokemonError;

        /// <summary>
        ///     Event fired when the train or battle command failed
        /// </summary>
        public event EventHandler<string> BattleError;

        /// <summary>
        ///     Event fired when the battle arena must be shown
        /// </summary>
        public event EventHandler ShowBattleArena;

        /// <summary>
        ///     Event fired when a battle has started
        /// </summary>
        public event EventHandler<int> BattleStarted;

        /// <summary>
        ///     Event fired when we have won a round in a battle
        /// </summary>
        public event EventHandler BattleRoundResultVictory;

        /// <summary>
        ///     Event fired when a battle has ended
        /// </summary>
        public event EventHandler BattleEnded;

        /// <summary>
        ///     Event fired when the Battle outcome can be shown
        /// </summary>
        public event EventHandler<BattleOutcomeResultEventArgs> ShowBattleOutcome;

        /// <summary>
        ///     Event fired when the close button on the Battle outcome has been clicked
        /// </summary>
        public event EventHandler CloseBattleOutcome;

        private DelegateCommand _enterCurrentGym;

        /// <summary>
        ///     Enters the current Gym, don't know what to do then
        /// </summary>
        public DelegateCommand EnterCurrentGym => _enterCurrentGym ?? (
            _enterCurrentGym = new DelegateCommand(async () =>
            {
                Busy.SetBusy(true, "Entering Gym");
                GameClient.CurrentSession.Logger.Info($"Entering {CurrentGymInfo.Name} [ID = {CurrentGym.Id}]");
                CurrentEnterResponse =
                    await GameClient.GymGetInfo(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                Busy.SetBusy(false);
                switch (CurrentEnterResponse.Result)
                {
                    case GymGetInfoResponse.Types.Result.Unset:
                        break;
                    case GymGetInfoResponse.Types.Result.Success:
                        // Success, we play the animation and update inventory
                        GameClient.CurrentSession.Logger.Info("Entering Gym success");

                        // What to do when we are in the Gym?
                        EnterSuccess?.Invoke(this, null);
                        //GameClient.UpdateInventory();
                        break;
                    case GymGetInfoResponse.Types.Result.ErrorNotInRange:
                        // Gym can't be used because it's out of range, there's nothing that we can do
                        GameClient.CurrentSession.Logger.Info("Entering Gym out of range");
                        EnterOutOfRange?.Invoke(this, null);
                        break;
                    case GymGetInfoResponse.Types.Result.ErrorGymDisabled:
                        // Gym can't be used because it's disabled, there's nothing that we can do
                        GameClient.CurrentSession.Logger.Info("Entering Gym disabled");
                        EnterDisabled?.Invoke(this, null);
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
                    GameClient.CurrentSession.Logger.Info($"Setting player team to { ChosenTeam }");
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
                var defenders = CurrentGymStatusAndDefenders.GymDefender.Select(x => x.MotivatedPokemon.Pokemon).ToList();

                if (defenders.Count < 1)
                    return;

                if (CurrentGymStatusAndDefenders.PokemonFortProto.IsInBattle)
                {
                    BattleError?.Invoke(this, "GymUnderAttack");
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

                AskForAttackTeam?.Invoke(this, null);
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
        private int _totalPlayerXpEarned;
        private int _totalGymPrestigeDelta;
        private int _pokemonDefeated;

        private DelegateCommand _goCommand;

        public DelegateCommand GoCommand => _goCommand ?? (
            _goCommand = new DelegateCommand(async() =>
            {
                try
                {
                    _battleTrackerCancellation = new CancellationTokenSource();
                    _isAbandoned = false;

                    Busy.SetBusy(true, Utils.Resources.CodeResources.GetString("StartingBattle"));

                    ServerRequestRunning = true;

                    var defenders = CurrentGymStatusAndDefenders.GymDefender.Select(x => x.MotivatedPokemon.Pokemon).ToList();
                    if (defenders.Count < 1)
                        return;

                    if (CurrentGymStatusAndDefenders.PokemonFortProto.IsInBattle)
                    {
                        BattleError?.Invoke(this, Utils.Resources.CodeResources.GetString("GymUnderAttack"));
                        return;
                    }

                    bool isTraining = (GameClient.PlayerData.Team == CurrentGymStatusAndDefenders.PokemonFortProto.OwnedByTeam);

                    var badassPokemon = AttackTeamMembers;
                    var pokemonDatas = badassPokemon.Select(x => x.WrappedData).ToArray();

                    GameClient.CurrentSession.Logger.Debug("Start gym battle with : " + string.Join(", ", defenders.Select(x => x.PokemonId.ToString())));

                    var index = 0;
                    bool isVictory = true;
                    bool isFailedToStart = false;

                    _totalPlayerXpEarned = 0;
                    _totalGymPrestigeDelta = 0;

                    List<BattleAction> battleActions = new List<BattleAction>();
                    ulong defenderPokemonId = defenders.First().Id;

                    while (index < defenders.Count() && !_battleTrackerCancellation.IsCancellationRequested)
                    {
                        var thisAttackActions = new List<BattleAction>();

                        GymStartSessionResponse result = null;
                        try
                        {
                            result = await StartBattle(CurrentGym.FortData, pokemonDatas, defenderPokemonId).ConfigureAwait(false);

                            switch (result.Result)
                            {
                                case GymStartSessionResponse.Types.Result.ErrorNotInRange:
                                    EnterOutOfRange?.Invoke(this, null);
                                    break;
                                case GymStartSessionResponse.Types.Result.Success:
                                    _currentBattleId = result.Battle.BattleId;
                                    _currentBattleStart = result.Battle.BattleStartMs;
                                    _currentBattleEnd = result.Battle.BattleEndMs;
                                    ServerBattleStartTimestampMs = result.Battle.BattleLog.BattleStartTimestampMs;

                                    CurrentAttacker = result.Battle.Attacker;
                                    if (CurrentAttacker != null)
                                        CurrentAttackerBattlePokemon = CurrentAttacker.ActivePokemon;
                                    if (CurrentAttackerBattlePokemon != null)
                                        CurrentAttackerPokemon = new PokemonDataWrapper(CurrentAttackerBattlePokemon.PokemonData);

                                    CurrentDefender = result.Battle.Defender;
                                    if (CurrentDefender != null)
                                        CurrentDefenderBattlePokemon = CurrentDefender.ActivePokemon;
                                    if (CurrentDefenderBattlePokemon != null)
                                        CurrentDefenderPokemon = new PokemonDataWrapper(CurrentDefenderBattlePokemon.PokemonData);

                                    _battleStopwatch = new Stopwatch();
                                    _battleStopwatch.Start();
                                    ShowBattleArena?.Invoke(this, null);
                                    AudioUtils.PlaySound(AudioUtils.BATTLE);

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            GameClient.CurrentSession.Logger.Debug("Can't start battle: " + ex.Message);
                            isFailedToStart = true;
                            isVictory = false;

                            GameClient.CurrentSession.Logger.Debug("Start battle result: " + result);
                            GameClient.CurrentSession.Logger.Debug("CurrentGym: " + CurrentGym);
                            GameClient.CurrentSession.Logger.Debug("PokemonDatas: " + string.Join(", ", pokemonDatas.Select(s => string.Format("Id: {0} Name: {1} CP: {2} HP: {3}", s.Id, s.PokemonId, s.Cp, s.Stamina))));
                            GameClient.CurrentSession.Logger.Debug("DefenderId: " + defenderPokemonId);
                            GameClient.CurrentSession.Logger.Debug("ActionsLog -> " + string.Join(Environment.NewLine, battleActions));

                            break;
                        }

                        index++;

                        if (result == null || result.Result != GymStartSessionResponse.Types.Result.Success)
                        {
                            isVictory = false;
                            break;
                        }
                        Busy.SetBusy(false);

                        await Dispatcher.DispatchAsync(async() =>
                        {
                            BattleStarted?.Invoke(this, index);
                            await Task.CompletedTask;
                        });

                        if (result != null && result.Result == GymStartSessionResponse.Types.Result.Success)
                        {
                        }

                        switch (result.Battle.BattleLog.State)
                        {
                            case BattleState.Active:
                                GameClient.CurrentSession.Logger.Debug("Time to start Attack Mode");
                                thisAttackActions = await AttackGym(_battleTrackerCancellation, CurrentGymInfo, result, index).ConfigureAwait(false);
                                battleActions.AddRange(thisAttackActions);
                                break;
                            case BattleState.Defeated:
                                isVictory = false;
                                break;
                            case BattleState.StateUnset:
                                isVictory = false;
                                break;
                            case BattleState.TimedOut:
                                isVictory = false;
                                break;
                            case BattleState.Victory:
                                isVictory = true;
                                break;
                            default:
                                GameClient.CurrentSession.Logger.Debug($"Unhandled result starting gym battle: {result}");
                                break;
                        }

                        var rewarded = battleActions.Select(x => x.BattleResults?.PlayerXpAwarded).Where(x => x != null);
                        var lastAction = battleActions.LastOrDefault();

                        if (lastAction.Type == BattleActionType.ActionTimedOut ||
                            lastAction.Type == BattleActionType.ActionUnset ||
                            lastAction.Type == BattleActionType.ActionDefeat)
                        {
                            isVictory = false;
                            break;
                        }

                        var faintedPKM = battleActions.Where(x => x != null && x.Type == BattleActionType.ActionFaint).Select(x => x.ActivePokemonId).Distinct();
                        var livePokemons = pokemonDatas.Where(x => !faintedPKM.Any(y => y == x.Id));
                        var faintedPokemons = pokemonDatas.Where(x => faintedPKM.Any(y => y == x.Id));
                        pokemonDatas = livePokemons.Concat(faintedPokemons).ToArray();

                        if (lastAction.Type == BattleActionType.ActionVictory)
                        {
                            if (lastAction.BattleResults != null)
                            {
                                var exp = lastAction.BattleResults.PlayerXpAwarded;
                                foreach (int xp in exp)
                                {
                                    _totalPlayerXpEarned += xp;
                                }
                                var point = lastAction.BattleResults.GymPointsDelta;
                                CurrentGym.GymPoints += point;
                                _totalGymPrestigeDelta += point;
                                _pokemonDefeated += 1;

                                defenderPokemonId = unchecked((ulong)lastAction.BattleResults.NextDefenderPokemonId);

                                BattleRoundResultVictory?.Invoke(this, null);
                                await Task.CompletedTask;

                                await Task.Delay(2000).ConfigureAwait(false);

                                GameClient.CurrentSession.Logger.Debug(string.Format("Exp: {0}, Gym points: {1}, Next defender Id: {2}", exp, point, defenderPokemonId));
                            }
                            continue;
                        }
                    }

                    BattleOutcomeResultEventArgs ea = new BattleOutcomeResultEventArgs("", _totalPlayerXpEarned, _totalGymPrestigeDelta, _pokemonDefeated, _lastAttackGymResponse);
                    NewGymPoints = CurrentGymInfo.GymStatusAndDefenders.PokemonFortProto.GymPoints + _totalGymPrestigeDelta;
                    RaisePropertyChanged(() => NewGymPoints);

                    if (!isVictory || _isAbandoned)
                    {
                        ea.BattleOutcome = "YOU LOSE!";
                        ShowBattleOutcome?.Invoke(this, ea);
                    }
                    else
                    {
                        if (isVictory)
                        {
                            ea.BattleOutcome = "YOU WIN!";

                            ShowBattleOutcome?.Invoke(this, ea);
                        }
                    }

                    // We have been victorious on all defenders, or we have been defeated ourselves
                    GameClient.CurrentSession.Logger.Debug(string.Join(Environment.NewLine, battleActions.OrderBy(o => o.ActionStartMs).Select(s => s).Distinct()));

                    AttackTeamSelectionClosed?.Invoke(this, null);
                    BattleEnded?.Invoke(this, null);
                    AudioUtils.StopSounds();
                    AudioUtils.PlaySound(AudioUtils.BEFORE_THE_FIGHT);
                }
                finally
                {
                    ServerRequestRunning = false;
                }
            }));

        private static async Task<GymStartSessionResponse> StartBattle(FortData gym, IEnumerable<PokemonData> attackers, ulong defenderId)
        {
            IEnumerable<PokemonData> currentPokemons = attackers;

            var pokemonDatas = currentPokemons as PokemonData[] ?? currentPokemons.ToArray();
            var attackerPokemons = pokemonDatas.Select(pokemon => pokemon.Id);
            var attackingPokemonIds = attackerPokemons as ulong[] ?? attackerPokemons.ToArray();

            try
            {
                GymStartSessionResponse result = await GameClient.GymStartSession(gym.Id, defenderId, attackingPokemonIds).ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);

                if (result.Result == GymStartSessionResponse.Types.Result.Success)
                {
                    switch (result.Battle.BattleLog.State)
                    {
                        case BattleState.Active:
                            GameClient.CurrentSession.Logger.Info("Start new battle...");
                            return result;
                        case BattleState.Defeated:
                            GameClient.CurrentSession.Logger.Info("We were defated in battle.");
                            return result;
                        case BattleState.Victory:
                            GameClient.CurrentSession.Logger.Info("We were victorious");
                            return result;
                        case BattleState.TimedOut:
                            GameClient.CurrentSession.Logger.Info("We ran out of time");
                            return result;
                        case BattleState.StateUnset:
                            GameClient.CurrentSession.Logger.Info($"Error ocurred: {result.Battle.BattleLog.State}");
                            break;
                        default:
                            GameClient.CurrentSession.Logger.Info($"Error ocurred: {result.Battle.BattleLog.State}");
                            break;
                    }
                }
                else if(result.Result == GymStartSessionResponse.Types.Result.ErrorGymBattleLockout)
                {
                    return result;
                }
                else if (result.Result == GymStartSessionResponse.Types.Result.ErrorAllPokemonFainted)
                {
                    return result;
                }
                else if (result.Result == GymStartSessionResponse.Types.Result.Unset)
                {
                    return result;
                }
                return result;
            }
            catch (Exception ex)
            {
                GameClient.CurrentSession.Logger.Error("Gym details:" + gym);
                throw ex;
            }
        }

        private int _currentAttackerEnergy;

        private async Task<List<BattleAction>> AttackGym(CancellationTokenSource cancellationToken, GymGetInfoResponse currentFortData, GymStartSessionResponse startResponse, int counter)
        {
            long serverMs = startResponse.Battle.BattleLog.BattleStartTimestampMs;
            var lastActions = startResponse.Battle.BattleLog.BattleActions.ToList();

            GameClient.CurrentSession.Logger.Info($"Gym battle started; fighting trainer: {startResponse.Battle.Defender.TrainerPublicProfile.Name}");
            GameClient.CurrentSession.Logger.Info($"We are attacking: {startResponse.Battle.Defender.ActivePokemon.PokemonData.PokemonId} ({startResponse.Battle.Defender.ActivePokemon.PokemonData.Cp} CP)");

            int loops = 0;
            List<BattleAction> emptyActions = new List<BattleAction>();
            BattleAction emptyAction = new BattleAction();
            PokemonData attacker = null;
            PokemonData defender = null;

            FortData gym = currentFortData.GymStatusAndDefenders.PokemonFortProto;
            _currentAttackerEnergy = 0;

            while(!_battleTrackerCancellation.IsCancellationRequested)
            {
                try
                {
                    RaisePropertyChanged(nameof(RemainingBattleTime));

                    GameClient.CurrentSession.Logger.Info("Starting loop");
                    var last = lastActions.Where(w => !AttackTeamMembers.Any(a => a.Id.Equals(w.ActivePokemonId))).LastOrDefault();
                    BattleAction lastSpecialAttack = lastActions.Where(w => !AttackTeamMembers.Any(a => a.Id.Equals(w.ActivePokemonId)) && w.Type == BattleActionType.ActionSpecialAttack).LastOrDefault();

                    //var attackActionz = last == null || last.Type == BattleActionType.ActionVictory || last.Type == BattleActionType.ActionDefeat ? emptyActions : AttackActions;
                    var attackActionz = last == null || last.Type == BattleActionType.ActionVictory || last.Type == BattleActionType.ActionDefeat ? emptyActions : GetActions(serverMs, attacker, defender, _currentAttackerEnergy, last, lastSpecialAttack);
                    GameClient.CurrentSession.Logger.Info(string.Format("Going to make attacks : {0}", string.Join(", ", attackActionz.Select(s => string.Format("{0} -> {1}", s.Type, s.DurationMs)))));

                    BattleAction a2 = (last == null || last.Type == BattleActionType.ActionVictory || last.Type == BattleActionType.ActionDefeat ? emptyAction : last);
                    GymBattleAttackResponse attackResult = null;

                    try
                    {
                        if (attackActionz.Any(a => a.Type == BattleActionType.ActionSwapPokemon))
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                        }

                        long timeBefore = DateTime.UtcNow.ToUnixTime();
                        attackResult = await GameClient.GymBattleAttack(gym.Id, startResponse.Battle.BattleId, attackActionz, a2, serverMs).ConfigureAwait(false);
                        _lastAttackGymResponse = attackResult;
                        long timeAfter = DateTime.UtcNow.ToUnixTime();
                        GameClient.CurrentSession.Logger.Debug(string.Format("Finished making attack call: {0}", timeAfter - timeBefore));
                        AttackActions.Clear();

                        var attackTime = attackActionz.Sum(x => x.DurationMs);
                        int attackTimeCorrected = attackTime;

                        if (attackActionz.Any(a => a.Type != BattleActionType.ActionSpecialAttack))
                            attackTimeCorrected = attackTime - (int)(timeAfter - timeBefore);

                        GameClient.CurrentSession.Logger.Debug(string.Format("Waiting for attack to be prepared: {0} (last call was {1}, after correction {2})", attackTime, timeAfter, attackTimeCorrected > 0 ? attackTimeCorrected : 0));
                        if (attackTimeCorrected > 0)
                            await Task.Delay(attackTimeCorrected).ConfigureAwait(false);

                        if (attackActionz.Any(a => a.Type == BattleActionType.ActionSwapPokemon))
                        {
                            GameClient.CurrentSession.Logger.Info("Extra wait after SWAP call");
                            await Task.Delay(2000).ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        GameClient.CurrentSession.Logger.Warn("Bad attack gym");
                        GameClient.CurrentSession.Logger.Debug(string.Format("Last retrieved action was: {0}", a2));
                        GameClient.CurrentSession.Logger.Debug(string.Format("Actions to perform were: {0}", string.Join(", ", attackActionz)));
                        GameClient.CurrentSession.Logger.Debug(string.Format("Attacker was: {0}, defender was: {1}", attacker, defender));

                        continue;
                    };

                    loops++;

                    if (attackResult.Result == GymBattleAttackResponse.Types.Result.Success)
                    {
                        GameClient.CurrentSession.Logger.Info("Attack success");
                        defender = attackResult.BattleUpdate.ActiveDefender?.PokemonData;
                        if (defender != null)
                        {
                            CurrentDefenderBattlePokemon = attackResult.BattleUpdate.ActiveDefender;
                            CurrentDefenderPokemon = new PokemonDataWrapper(defender);
                        }

                        if (attackResult.BattleUpdate.BattleLog != null && attackResult.BattleUpdate.BattleLog.BattleActions.Count > 0)
                        {
                            var result = attackResult.BattleUpdate.BattleLog.BattleActions.OrderBy(o => o.ActionStartMs).Distinct();
                            ShowActions(result);
                            lastActions.AddRange(result);
                        }

                        serverMs = attackResult.BattleUpdate.BattleLog.ServerMs;

                        switch (attackResult.BattleUpdate.BattleLog.State)
                        {
                            case BattleState.Active:
                                _currentAttackerEnergy = attackResult.BattleUpdate.ActiveAttacker.CurrentEnergy;
                                attacker = attackResult.BattleUpdate.ActiveAttacker.PokemonData;
                                CurrentAttackerBattlePokemon = attackResult.BattleUpdate.ActiveAttacker;
                                CurrentAttackerPokemon = new PokemonDataWrapper(attacker);

                                GameClient.CurrentSession.Logger.Debug($"(GYM ATTACK) : Defender {attackResult.BattleUpdate.ActiveDefender.PokemonData.PokemonId.ToString()  } HP {attackResult.BattleUpdate.ActiveDefender.CurrentHealth} - Attacker  {attackResult.BattleUpdate.ActiveAttacker.PokemonData.PokemonId.ToString()} ({attackResult.BattleUpdate.ActiveAttacker.PokemonData.Cp} CP)  HP/Sta {attackResult.BattleUpdate.ActiveAttacker.CurrentHealth}/{attackResult.BattleUpdate.ActiveAttacker.CurrentEnergy}");
                                if (attackResult != null && attackResult.BattleUpdate.ActiveAttacker != null)
                                {
                                    CurrentAttackerBattlePokemon.CurrentHealth = attackResult.BattleUpdate.ActiveAttacker.CurrentHealth;
                                    RaisePropertyChanged(nameof(CurrentAttackerBattlePokemon));
                                }
                                if (attackResult != null && attackResult.BattleUpdate.ActiveDefender != null)
                                {
                                    CurrentDefenderBattlePokemon.CurrentHealth = attackResult.BattleUpdate.ActiveDefender.CurrentHealth;
                                    RaisePropertyChanged(nameof(CurrentDefenderBattlePokemon));
                                }
                                break;
                            case BattleState.Defeated:
                                GameClient.CurrentSession.Logger.Info($"We were defeated... (AttackGym)");
                                return lastActions;
                            case BattleState.TimedOut:
                                GameClient.CurrentSession.Logger.Info($"Our attack timed out...:");
                                return lastActions;
                            case BattleState.StateUnset:
                                GameClient.CurrentSession.Logger.Info($"State was unset?: {attackResult}");
                                return lastActions;

                            case BattleState.Victory:
                                GameClient.CurrentSession.Logger.Info($"We were victorious!: ");
                                await Task.Delay(2000).ConfigureAwait(false);
                                return lastActions;
                            default:
                                GameClient.CurrentSession.Logger.Info($"Unhandled attack response: {attackResult}");
                                continue;
                        }
                        GameClient.CurrentSession.Logger.Info($"{attackResult}");
                    }
                    else
                    {
                        GameClient.CurrentSession.Logger.Error($"Unexpected attack result: {attackResult}");
                        break;
                    }

                    GameClient.CurrentSession.Logger.Info("Finished attack");
                }
                catch (Exception ex)
                {
                    GameClient.CurrentSession.Logger.Warn("Bad request sent to server - ");
                    GameClient.CurrentSession.Logger.Warn("Did NOT finish attack");
                    GameClient.CurrentSession.Logger.Warn(ex.Message);
                };
            }
            return lastActions;
        }

        long timeToDodge = 0;
        long lastWentDodge = DateTime.Now.ToUnixTime();

        public List<BattleAction> GetActions(long serverMs, PokemonData attacker, PokemonData defender, int energy, BattleAction lastAction, BattleAction lastSpecialAttack)
        {
            List<BattleAction> actions = new List<BattleAction>();
            DateTime now = DateTimeFromUnixTimestampMillis(serverMs);
            const int beforeDodge = 200;

            if (lastSpecialAttack != null && lastSpecialAttack.DamageWindowsStartTimestampMs > serverMs)
            {
                long dodgeTime = lastSpecialAttack.DamageWindowsStartTimestampMs - beforeDodge;
                if (timeToDodge < dodgeTime)
                    timeToDodge = dodgeTime;
            }

            if (attacker != null && defender != null)
            {
                var normalMove = GameClient.MoveSettings.FirstOrDefault(m => m.MovementId == CurrentAttackerPokemon.Move1);
                var specialMove = GameClient.MoveSettings.FirstOrDefault(m => m.MovementId == CurrentAttackerPokemon.Move2);

                bool skipDodge = ((lastSpecialAttack?.DurationMs ?? 0) < normalMove.DurationMs + 550); //if our normal attack is too slow and defender special is too fast so we should to only do dodge all the time then we totally skip dodge

                bool canDoSpecialAttack = Math.Abs(specialMove.EnergyDelta) <= energy && (!(timeToDodge > now.ToUnixTime() && timeToDodge < now.ToUnixTime() + specialMove.DurationMs) || skipDodge);

                bool canDoAttack = !canDoSpecialAttack && (!(timeToDodge > now.ToUnixTime() && timeToDodge < now.ToUnixTime() + normalMove.DurationMs) || skipDodge);

                if (timeToDodge > now.ToUnixTime() && !canDoAttack && !canDoSpecialAttack && !skipDodge)
                {
                    lastWentDodge = now.ToUnixTime();

                    BattleAction dodge = new BattleAction()
                    {
                        Type = BattleActionType.ActionDodge,
                        ActionStartMs = now.ToUnixTime(),
                        DurationMs = 500,
                        TargetIndex = -1,
                        ActivePokemonId = attacker.Id,
                    };

                    GameClient.CurrentSession.Logger.Info(string.Format("Trying to dodge an attack {0}, lastSpecialAttack.DamageWindowsStartTimestampMs: {1}, serverMs: {2}", dodge, lastSpecialAttack.DamageWindowsStartTimestampMs, serverMs));
                    actions.Add(dodge);
                }
                else
                {
                    BattleAction action2 = new BattleAction();
                    if (canDoSpecialAttack)
                    {
                        action2.Type = BattleActionType.ActionSpecialAttack;
                        action2.DurationMs = specialMove.DurationMs;
                        action2.DamageWindowsStartTimestampMs = specialMove.DamageWindowStartMs;
                        action2.DamageWindowsEndTimestampMs = specialMove.DamageWindowEndMs;
                        GameClient.CurrentSession.Logger.Info(string.Format("Trying to make an special attack {0}, on: {1}, duration: {2}", specialMove.MovementId, serverMs, specialMove.DurationMs));
                    }
                    else if (canDoAttack)
                    {
                        action2.Type = BattleActionType.ActionAttack;
                        action2.DurationMs = normalMove.DurationMs;
                        action2.DamageWindowsStartTimestampMs = normalMove.DamageWindowStartMs;
                        action2.DamageWindowsEndTimestampMs = normalMove.DamageWindowEndMs;
                        GameClient.CurrentSession.Logger.Info(string.Format("Trying to make an normal attack {0}, on: {1}, duration: {2}", normalMove.MovementId, serverMs, normalMove.DurationMs));
                    }
                    else
                    {
                        GameClient.CurrentSession.Logger.Info("SHIT, no action available");
                    }
                    action2.ActionStartMs = now.ToUnixTime();
                    action2.TargetIndex = -1;
                    if (attacker.Stamina > 0)
                        action2.ActivePokemonId = attacker.Id;
                    action2.TargetPokemonId = defender.Id;
                    actions.Add(action2);
                }
                return actions;
            }
            BattleAction action1 = new BattleAction()
            {
                Type = BattleActionType.ActionDodge,
                DurationMs = 500,
                ActionStartMs = now.ToUnixTime(),
                TargetIndex = -1
            };
            if (defender != null && attacker != null)
                action1.ActivePokemonId = attacker.Id;

            actions.Add(action1);
            return actions;
        }

        private void ShowActions(IEnumerable<BattleAction> actions)
        {
            Task.Run(() =>
            {
                foreach (BattleAction action in actions)
                {
                    if (action.ActivePokemonId == CurrentAttackerPokemon.Id)
                    {
                        switch (action.Type)
                        {
                            case BattleActionType.ActionAttack:
                                CurrentAttackType = "Attack";
                                AttackingActionAttack?.Invoke(this, null);
                                break;
                            case BattleActionType.ActionSpecialAttack:
                                CurrentAttackType = "Special attack";
                                AttackingActionSpecialAttack?.Invoke(this, null);
                                break;
                            case BattleActionType.ActionDodge:
                                CurrentAttackType = "Dodged";
                                AttackingActionDodge?.Invoke(this, null);
                                break;
                        }
                    }
                    else
                    {
                        switch (action.Type)
                        {
                            case BattleActionType.ActionAttack:
                                CurrentDefendType = "Attack";
                                DefendingActionAttack?.Invoke(this, null);
                                break;
                            case BattleActionType.ActionSpecialAttack:
                                CurrentDefendType = "Special attack";
                                DefendingActionSpecialAttack?.Invoke(this, null);
                                break;
                            case BattleActionType.ActionDodge:
                                CurrentDefendType = "Dodged";
                                DefendingActionDodge?.Invoke(this, null);
                                break;
                        }
                    }

                    Task.Delay(action.DurationMs);
                }
            });
        }

        private bool _isAbandoned;

        private DelegateCommand _abandonAttackCommand;

        public DelegateCommand AbandonAttackCommand => _abandonAttackCommand ?? (
            _abandonAttackCommand = new DelegateCommand(() =>
            {
                _isAbandoned = true;
                _battleTrackerCancellation.Cancel();
            }));

        #endregion

        #region Fight
        private DelegateCommand _fightCommand;

        public DelegateCommand FightCommand => _fightCommand ?? (
            _fightCommand = new DelegateCommand(() =>
            {
                var defenders = CurrentGymStatusAndDefenders.GymDefender.Select(x => x.MotivatedPokemon.Pokemon).ToList();

                if (defenders.Count < 1)
                    return;

                if (CurrentGymStatusAndDefenders.PokemonFortProto.IsInBattle)
                {
                    BattleError?.Invoke(this, "GymUnderAttack");
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

                AskForAttackTeam?.Invoke(this, null);
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
                            var gymDeployResponse = await GameClient.GymDeploy(CurrentGym.Id, SelectedPokemon.Id);

                            switch (gymDeployResponse.Result)
                            {
                                case GymDeployResponse.Types.Result.NoResultSet:
                                    break;
                                case GymDeployResponse.Types.Result.Success:
                                    // Remove the deployed pokemon from the inventory on screen and update the Gymstate
                                    PokemonInventory.Remove(SelectedPokemon);
                                    CurrentGymStatusAndDefenders = gymDeployResponse.GymStatusAndDefenders;

                                    RaisePropertyChanged(() => PokemonInventory);
                                    RaisePropertyChanged(() => CurrentGymStatusAndDefenders);
                                    RaisePropertyChanged(() => GymLevel);
                                    RaisePropertyChanged(() => GymPrestigeFull);
                                    RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                                    RaisePropertyChanged(() => TrainCommandVisibility);
                                    RaisePropertyChanged(() => FightCommandVisibility);
                                    RaisePropertyChanged(() => DeployCommandButtonEnabled);
                                    RaisePropertyChanged(() => OutOfRangeMessageBorderVisibility);
                                    RaisePropertyChanged(() => GymDefenders);

                                    // TODO: Implement message informing about success of deployment (Shell needed)
                                    //GameClient.UpdateInventory();
                                    await GameClient.UpdatePlayerStats();

                                    // Reset to gym screen
                                    PokemonDeployed?.Invoke(this, null);
                                    break;

                                case GymDeployResponse.Types.Result.ErrorAlreadyHasPokemonOnFort:
                                case GymDeployResponse.Types.Result.ErrorFortDeployLockout:
                                case GymDeployResponse.Types.Result.ErrorFortIsFull:
                                case GymDeployResponse.Types.Result.ErrorNotInRange:
                                case GymDeployResponse.Types.Result.ErrorOpposingTeamOwnsFort:
                                case GymDeployResponse.Types.Result.ErrorPlayerBelowMinimumLevel:
                                case GymDeployResponse.Types.Result.ErrorPlayerHasNoNickname:
                                case GymDeployResponse.Types.Result.ErrorPlayerHasNoTeam:
                                case GymDeployResponse.Types.Result.ErrorPokemonIsBuddy:
                                case GymDeployResponse.Types.Result.ErrorPokemonNotFullHp:
                                case GymDeployResponse.Types.Result.ErrorInvalidPokemon:
                                case GymDeployResponse.Types.Result.ErrorLegendaryPokemon:
                                case GymDeployResponse.Types.Result.ErrorNotAPokemon:
                                case GymDeployResponse.Types.Result.ErrorPoiInaccessible:
                                case GymDeployResponse.Types.Result.ErrorRaidActive:
                                case GymDeployResponse.Types.Result.ErrorTeamDeployLockout:
                                case GymDeployResponse.Types.Result.ErrorTooManyDeployed:
                                case GymDeployResponse.Types.Result.ErrorTooManyOfSameKind:
                                    DeployPokemonError?.Invoke(this, gymDeployResponse.Result);
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

        private DelegateCommand _closeBattleOutcomeCommand;
        public DelegateCommand CloseBattleOutcomeCommand =>
            _closeBattleOutcomeCommand ?? (
            _closeBattleOutcomeCommand = new DelegateCommand(async() =>
            {
                // Get new GymInfo, because it might have been changed
                CurrentGymInfo = await GameClient.GymGetInfo(CurrentGym.Id, CurrentGym.Latitude, CurrentGym.Longitude);
                CurrentGymStatusAndDefenders = CurrentGymInfo.GymStatusAndDefenders;
                RaisePropertyChanged(() => GymLevel);
                RaisePropertyChanged(() => GymPrestigeFull);
                RaisePropertyChanged(() => DeployPokemonCommandVisibility);
                RaisePropertyChanged(() => TrainCommandVisibility);
                RaisePropertyChanged(() => FightCommandVisibility);
                RaisePropertyChanged(() => DeployCommandButtonEnabled);
                RaisePropertyChanged(() => TrainCommandButtonEnabled);
                RaisePropertyChanged(() => BattleCommandButtonEnabled);
                RaisePropertyChanged(() => OutOfRangeMessageBorderVisibility);
                GymLoaded?.Invoke(this, null);

                CloseBattleOutcome?.Invoke(this, null);
            }));

        private DelegateCommand _autoSelectCommand;

        public DelegateCommand AutoSelectCommand =>
            _autoSelectCommand ?? (
            _autoSelectCommand = new DelegateCommand(() =>
            {
                bool isTraining = (GameClient.PlayerData.Team == CurrentGymStatusAndDefenders.PokemonFortProto.OwnedByTeam);

                var attackTeam = CompleteAttackTeam(isTraining);
                if (attackTeam != null && attackTeam.Count() == 6)
                {
                    AttackTeamMembers.Clear();
                    foreach (PokemonDataWrapper attackerPokemon in attackTeam)
                    {
                        AttackTeamMembers.Add(attackerPokemon);
                    }
                    RaisePropertyChanged(() => AttackTeamMembers);
                }
                else
                {
                    BattleError?.Invoke(this, "Could not complete team");
                }
            }));

        private IEnumerable<PokemonDataWrapper> CompleteAttackTeam(bool isTraining)
        {
            List<PokemonDataWrapper> attackers = new List<PokemonDataWrapper>();

            while (attackers.Count < 6)
            {
                foreach (var defenderPokemon in GymDefenders)
                {
                    var defender = new PokemonDataWrapper(defenderPokemon.MotivatedPokemon.Pokemon);
                    var attacker = GetBestAgainst(attackers, defender, isTraining);
                    if (attacker != null)
                    {
                        attackers.Add(attacker);
                        if (attackers.Count == 6)
                            break;
                    }
                    else return null;
                }
            }

            return attackers;
        }

        private PokemonDataWrapper GetBestAgainst(List<PokemonDataWrapper> myTeam, PokemonDataWrapper defender, bool isTraining)
        {
            GameClient.CurrentSession.Logger.Info(string.Format("Checking pokemon for {0} ({1} CP). Already collected team has: {2}", defender.PokemonId, defender.Cp, string.Join(", ", myTeam.Select(s => string.Format("{0} ({1} CP)", s.PokemonId, s.Cp)))));

            GymDefender gDefender = GymDefenders.FirstOrDefault(f => f.MotivatedPokemon.Pokemon.Id == defender.Id);
            AnyPokemonStat defenderStat = new AnyPokemonStat(new PokemonDataWrapper(gDefender.MotivatedPokemon.Pokemon));

            var allPokemonStats = new List<MyPokemonStat>();
            foreach (PokemonData pokemonData in GameClient.PokemonsInventory)
            {
                allPokemonStats.Add(new MyPokemonStat(new PokemonDataWrapper(pokemonData)));
            }

            MyPokemonStat myAttacker = allPokemonStats
                .Where(w =>
                        !myTeam.Any(a => a.Id == w.Data.Id) && // not already in team
                        string.IsNullOrEmpty(w.Data.DeployedFortId) && // not deployed
                        GameClient.PlayerData.BuddyPokemon?.Id != w.Data.Id // not a buddy
                        )
                .OrderByDescending(o => o.TypeFactor[defenderStat.MainType] + o.TypeFactor[defenderStat.ExtraType] + o.GetFactorAgainst(defender.Cp, isTraining))
                .ThenByDescending(o => o.Data.Cp)
                .FirstOrDefault();

            if (myAttacker == null)
            {
                var other = GetBestToTeam(myTeam).FirstOrDefault();
                return other;
            }
            else
            {

            }
            return myAttacker.Data;
        }

        private IEnumerable<PokemonDataWrapper> GetBestToTeam(List<PokemonDataWrapper> myTeam)
        {
            var allPokemons = GameClient.PokemonsInventory;

            var data = allPokemons.Where(w =>
                        !myTeam.Any(a => a.Id == w.Id) && // not already in team
                        string.IsNullOrEmpty(w.DeployedFortId) && // not deployed
                        GameClient.PlayerData.BuddyPokemon.Id != w.Id // not a buddy
                    )
                    .OrderByDescending(o => o.Cp)
                    .Take(6 - myTeam.Count());

            GameClient.CurrentSession.Logger.Info("Best others are: " + string.Join(", ", data.Select(s => s.PokemonId)));
            return data.Select(s => new PokemonDataWrapper(s));
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

        public event EventHandler DefendingActionAttack;
        public event EventHandler DefendingActionSpecialAttack;
        public event EventHandler DefendingActionDodge;

        public event EventHandler AttackingActionAttack;
        public event EventHandler AttackingActionSpecialAttack;
        public event EventHandler AttackingActionDodge;

        /// <summary>
        ///     Determines whether we can keep battling.
        /// </summary>
        private CancellationTokenSource _battleTrackerCancellation;

        private long ServerBattleStartTimestampMs;

        //private async Task RunBattle()
        //{
        //    await Task.Run(async() =>
        //    {
        //        while (!_battleTrackerCancellation.IsCancellationRequested && RemainingBattleTime >= 0)
        //        {
        //            RaisePropertyChanged(() => RemainingBattleTime);

        //            BattleAction lastReceivedBattleAction = BattleLogEntries.BattleActions.LastOrDefault();
        //            BattleAction lastSpecialAttack = BattleLogEntries.BattleActions.Where(w => w.Type == BattleActionType.ActionSpecialAttack).LastOrDefault();

        //            if (BattleLogEntries.BattleActions.Count() > 0)
        //            {
        //                BattleAction action = BattleLogEntries.BattleActions[0];

        //                Logger.Debug("Found action: " + action.Type.ToString());

        //                if (action.ActivePokemonId == CurrentAttacker.ActivePokemon.PokemonData.Id)
        //                {
        //                    Logger.Debug("===> Returned action contains attacker");
        //                }

        //                BattleLogEntries.BattleActions.RemoveAt(0);

        //                switch (action.Type)
        //                {
        //                    case BattleActionType.ActionAttack:
        //                        CurrentDefendType = "Attack!";
        //                        DefendingActionAttack?.Invoke(this, null);
        //                        break;
        //                    case BattleActionType.ActionSpecialAttack:
        //                        CurrentDefendType = "Special Attack!";
        //                        DefendingActionSpecialAttack?.Invoke(this, null);
        //                        break;
        //                    case BattleActionType.ActionDodge:
        //                        CurrentDefendType = "Dodge";
        //                        DefendingActionDodge?.Invoke(this, null);
        //                        break;
        //                    case BattleActionType.ActionFaint:
        //                        break;
        //                    case BattleActionType.ActionVictory:
        //                        Victory?.Invoke(this, action);
        //                        _battleTrackerCancellation?.Cancel();
        //                        break;
        //                    case BattleActionType.ActionDefeat:
        //                        Defeat?.Invoke(this, action);
        //                        _battleTrackerCancellation?.Cancel();
        //                        break;
        //                    case BattleActionType.ActionTimedOut:
        //                        TimedOut?.Invoke(this, action);
        //                        _battleTrackerCancellation?.Cancel();
        //                        break;
        //                    case BattleActionType.ActionPlayerJoin:
        //                        CurrentAttackerBattlePokemon = action.PlayerJoined.ReversePokemon[0];
        //                        CurrentDefenderBattlePokemon = action.PlayerJoined.ActivePokemon;
        //                        break;
        //                }

        //                try
        //                {
        //                    Task.Delay(action.DurationMs, _battleTrackerCancellation.Token);
        //                }
        //                catch (OperationCanceledException)
        //                {
        //                    break;
        //                }
        //            }

        //            // When no more defending actions are there, send our own gathered actions in return
        //            if (BattleLogEntries.BattleActions.Count() == 0)
        //            {
        //                List<BattleAction> battleActionsToSend;
        //                if (lastReceivedBattleAction == null || lastReceivedBattleAction.Type == BattleActionType.ActionVictory || lastReceivedBattleAction.Type == BattleActionType.ActionDefeat)
        //                {
        //                    battleActionsToSend = new List<BattleAction>();
        //                }
        //                else
        //                {
        //                    battleActionsToSend = AttackActions;
        //                    AttackActions = null;
        //                }

        //                Logger.Info($"(GYM ATTACK) : Sending {battleActionsToSend.Count} actions to battle");

        //                AttackGymResponse attackGymResponse = await GameClient.AttackGym(CurrentGym.Id, _currentBattleId, battleActionsToSend, lastReceivedBattleAction);
        //                _lastAttackGymResponse = attackGymResponse;

        //                switch (attackGymResponse.Result)
        //                {
        //                    case AttackGymResponse.Types.Result.Success:
        //                        CurrentDefenderPokemon = new PokemonDataWrapper(attackGymResponse.ActiveDefender?.PokemonData);
        //                        if (attackGymResponse.BattleLog != null && attackGymResponse.BattleLog.BattleActions.Count > 0)
        //                        {
        //                            BattleLogEntries.BattleActions.Clear();
        //                            var newActions = attackGymResponse.BattleLog.BattleActions.OrderBy(o => o.ActionStartMs).Distinct();
        //                            BattleLogEntries.BattleActions.AddRange(newActions);
        //                        }
        //                        ServerBattleStartTimestampMs = attackGymResponse.BattleLog.ServerMs;

        //                        switch (attackGymResponse.BattleLog.State)
        //                        {
        //                            case BattleState.Defeated:
        //                                Logger.Notice("We were defeated");
        //                                break;
        //                            case BattleState.Victory:
        //                                Logger.Notice("We have WON!");

        //                                break;
        //                            case BattleState.TimedOut:
        //                                Logger.Notice("Attack timed out");
        //                                break;
        //                            case BattleState.StateUnset:
        //                                Logger.Notice($"State unset {attackGymResponse}");
        //                                break;
        //                            case BattleState.Active:
        //                                if (attackGymResponse.ActiveAttacker.PokemonData.Id != CurrentAttackerPokemon.Id)
        //                                {
        //                                    Logger.Notice($"Pokemon has died and now we switched to {attackGymResponse.ActiveAttacker.PokemonData.Id}");
        //                                    CurrentAttackerPokemon = new PokemonDataWrapper(attackGymResponse.ActiveAttacker.PokemonData);
        //                                }
        //                                CurrentAttackerBattlePokemon = attackGymResponse.ActiveAttacker;
        //                                CurrentDefenderBattlePokemon = attackGymResponse.ActiveDefender;
        //                                RaisePropertyChanged(() => CurrentAttackerBattlePokemon);
        //                                RaisePropertyChanged(() => CurrentDefenderBattlePokemon);
        //                                Logger.Info($"(GYM ATTACK) : Defender {CurrentDefenderBattlePokemon.PokemonData.PokemonId.ToString()  } HP {CurrentDefenderBattlePokemon.CurrentHealth} - Attacker  {CurrentAttackerBattlePokemon.PokemonData.PokemonId.ToString()} ({CurrentAttackerBattlePokemon.PokemonData.Cp} CP)  HP/Sta {CurrentAttackerBattlePokemon.CurrentHealth}/{CurrentAttackerBattlePokemon.CurrentEnergy}        ");
        //                                Logger.Info($"(GYM ATTACK) : Received {attackGymResponse.BattleLog.BattleActions.Count} actions in return");
        //                                break;
        //                        }
        //                        break;
        //                    case AttackGymResponse.Types.Result.ErrorNotInRange:
        //                        TrainError?.Invoke(this, "You are out of range");
        //                        break;
        //                    case AttackGymResponse.Types.Result.ErrorInvalidAttackActions:
        //                        TrainError?.Invoke(this, "There are invalid actions");
        //                        break;
        //                }
        //            }
        //        }

        //    });

        //    await Task.CompletedTask;

        //    AttackTeamSelectionClosed?.Invoke(this, null);
        //    BattleEnded?.Invoke(this, _lastAttackGymResponse);
        //    AudioUtils.StopSounds();
        //    AudioUtils.PlaySound(AudioUtils.BEFORE_THE_FIGHT);
        //}

        private GymBattleAttackResponse _lastAttackGymResponse;

        private DelegateCommand _dodgeCommand;

        public DelegateCommand DodgeCommand => _dodgeCommand ?? (
           _dodgeCommand = new DelegateCommand(() =>
           {
               //CurrentAttackType = "Dodge";
               //AttackingActionDodge?.Invoke(this, null);
               BattleAction dodge = new BattleAction()
               {
                   Type = BattleActionType.ActionDodge,
                   ActionStartMs = DateTimeFromUnixTimestampMillis(ServerBattleStartTimestampMs).ToUnixTime(),
                   DurationMs = 500,
                   TargetIndex = -1,
                   ActivePokemonId = CurrentAttackerPokemon.Id,
                   AttackerIndex = -1
               };
               AttackActions.Add(dodge);
           }));

        private DelegateCommand _attackCommand;

        public DelegateCommand AttackCommand => _attackCommand ?? (
           _attackCommand = new DelegateCommand(() =>
           {
               //CurrentAttackType = "Attack!";
               //AttackingActionAttack?.Invoke(this, null);
               var normalMove = GameClient.MoveSettings.FirstOrDefault(m => m.MovementId == CurrentAttackerPokemon.Move1);
               BattleAction attack = new BattleAction()
               {
                   Type = BattleActionType.ActionAttack,
                   DurationMs = normalMove.DurationMs,
                   DamageWindowsStartTimestampMs = normalMove.DamageWindowStartMs,
                   DamageWindowsEndTimestampMs = normalMove.DamageWindowEndMs,
                   ActionStartMs = DateTimeFromUnixTimestampMillis(ServerBattleStartTimestampMs).ToUnixTime(),
                   TargetIndex = -1,
                   TargetPokemonId = CurrentDefenderPokemon.Id,
                   ActivePokemonId = CurrentAttackerPokemon.Id,
                   AttackerIndex = -1,
               };
               AttackActions.Add(attack);
           }));

        private DelegateCommand _specialAttackCommand;
        public DelegateCommand SpecialAttackCommand => _specialAttackCommand ?? (
           _specialAttackCommand = new DelegateCommand(() =>
           {
               //CurrentAttackType = "Special Attack!";
               //AttackingActionSpecialAttack?.Invoke(this, null);
               var specialMove = GameClient.MoveSettings.FirstOrDefault(m => m.MovementId == CurrentAttackerPokemon.Move2);
               BattleAction specialAttack = new BattleAction()
               {
                   Type = BattleActionType.ActionSpecialAttack,
                   DurationMs = specialMove.DurationMs,
                   DamageWindowsStartTimestampMs = specialMove.DamageWindowStartMs,
                   DamageWindowsEndTimestampMs = specialMove.DamageWindowEndMs,
                   ActionStartMs = DateTimeFromUnixTimestampMillis(ServerBattleStartTimestampMs).ToUnixTime(),
                   TargetIndex = -1,
                   TargetPokemonId = CurrentDefenderPokemon.Id,
                   ActivePokemonId = CurrentAttackerPokemon.Id,
                   AttackerIndex = -1
               };
               AttackActions.Add(specialAttack);
           }));

        private List<BattleAction> _attackActions;
        public List<BattleAction> AttackActions
        {
            get
            {
                if (_attackActions == null)
                {
                    _attackActions = new List<BattleAction>();
                }
                return _attackActions; }
            set { _attackActions = value; }
        }
        #endregion

        public static DateTime DateTimeFromUnixTimestampMillis(long millis)
        {
            return UnixEpoch.AddMilliseconds(millis);
        }
    }

    public class BattleOutcomeResultEventArgs
    {
        public BattleOutcomeResultEventArgs(string battleOutcome, int totalPlayerXpEarned, int totalGymPrestigeDelta, int pokemonDefeated, GymBattleAttackResponse lastAttackGymResponse)
        {
            BattleOutcome = battleOutcome;
            TotalPlayerXpEarned = totalPlayerXpEarned;
            TotalGymPrestigeDelta = totalGymPrestigeDelta;
            PokemonDefeated = pokemonDefeated;
            LastAttackGymResponse = lastAttackGymResponse;
        }

        public string BattleOutcome { get; set; }
        public int TotalPlayerXpEarned { get; set; }
        public int TotalGymPrestigeDelta { get; set; }
        public int PokemonDefeated { get; set; }
        public GymBattleAttackResponse LastAttackGymResponse { get; set; }
    }
}