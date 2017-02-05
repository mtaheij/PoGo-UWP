using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using POGOProtos.Data;
using POGOProtos.Inventory;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using PokemonGo_UWP.Utils.Extensions;
using Windows.UI.Xaml.Media.Animation;
using PokemonGo_UWP.Controls;
using POGOProtos.Networking.Responses;

namespace PokemonGo_UWP.ViewModels
{
    public class PokemonInventoryPageViewModel : ViewModelBase
    {
        #region Lifecycle Handlers

        /// <summary>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode,
            IDictionary<string, object> suspensionState)
        {
            if (suspensionState.Any())
            {
                // Recovering the state
                PokemonInventory = JsonConvert.DeserializeObject<ObservableCollection<PokemonDataWrapper>>((string)suspensionState[nameof(PokemonInventory)]);
                EggsInventory = JsonConvert.DeserializeObject<ObservableCollection<PokemonDataWrapper>>((string)suspensionState[nameof(EggsInventory)]);
                CurrentPokemonSortingMode = (PokemonSortingModes)suspensionState[nameof(CurrentPokemonSortingMode)];
                PlayerProfile = GameClient.PlayerProfile;
            }
            else
            {
                // Navigating from game page, so we need to actually load the inventory
                // The sorting mode is directly bound to the settings
                PokemonInventory = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                    .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                    .SortBySortingmode(CurrentPokemonSortingMode));

                RaisePropertyChanged(() => PokemonInventory);

                var unincubatedEggs = GameClient.EggsInventory.Where(o => string.IsNullOrEmpty(o.EggIncubatorId))
                                                              .OrderBy(c => c.EggKmWalkedTarget);
                var incubatedEggs = GameClient.EggsInventory.Where(o => !string.IsNullOrEmpty(o.EggIncubatorId))
                                                              .OrderBy(c => c.EggKmWalkedTarget);
                EggsInventory.Clear();
                // advancedrei: I have verified this is the sort order in the game.
                foreach (var incubatedEgg in incubatedEggs)
                {
                    EggsInventory.Add(new IncubatedEggDataWrapper(GameClient.GetIncubatorFromEgg(incubatedEgg), GameClient.PlayerStats.KmWalked, incubatedEgg));
                }

                foreach (var pokemonData in unincubatedEggs)
                {
                    EggsInventory.Add(new PokemonDataWrapper(pokemonData));
                }

                RaisePropertyChanged(() => TotalPokemonCount);

                PlayerProfile = GameClient.PlayerProfile;
            }

            // try restoring scrolling position 
            if (NavigationHelper.NavigationState.ContainsKey("LastViewedPokemonDetailID"))
            {
                ulong pokemonId = (ulong)NavigationHelper.NavigationState["LastViewedPokemonDetailID"];
                NavigationHelper.NavigationState.Remove("LastViewedPokemonDetailID");
                var pokemon = PokemonInventory.Where(p => p.Id == pokemonId).FirstOrDefault();
                if (pokemon != null)
                {
                    ScrollPokemonToVisibleRequired?.Invoke(pokemon);
                }
            }

            // set the selectioMode to GoToDetail
            currentSelectionMode = SelectionMode.GoToDetail;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Save state before navigating
        /// </summary>
        /// <param name="suspensionState"></param>
        /// <param name="suspending"></param>
        /// <returns></returns>
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {
            if (suspending)
            {
                suspensionState[nameof(PokemonInventory)] = JsonConvert.SerializeObject(PokemonInventory);
                suspensionState[nameof(EggsInventory)] = JsonConvert.SerializeObject(EggsInventory);
                suspensionState[nameof(CurrentPokemonSortingMode)] = CurrentPokemonSortingMode;
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Bindable Game Vars

        public delegate void ScrollPokemonToVisibleHandler(PokemonDataWrapper p);
        public event ScrollPokemonToVisibleHandler ScrollPokemonToVisibleRequired;
        public event EventHandler<PokemonDataWrapper> MultiplePokemonSelected;

        /// <summary>
        /// Player's profile, we use it just for the maximum ammount of pokemon
        /// </summary>
        private PlayerData _playerProfile;
        public PlayerData PlayerProfile
        {
            get { return _playerProfile; }
            set { Set(ref _playerProfile, value); }
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

        /// <summary>
        /// Reference to Pokemon inventory
        /// </summary>
        public ObservableCollection<PokemonDataWrapper> PokemonInventory { get; private set; } =
            new ObservableCollection<PokemonDataWrapper>();

        /// <summary>
        /// Reference to Eggs inventory
        /// </summary>
        public ObservableCollection<PokemonDataWrapper> EggsInventory { get; private set; } =
            new ObservableCollection<PokemonDataWrapper>();

        /// <summary>
        /// Reference to Incubators inventory
        /// </summary>
        public ObservableCollection<EggIncubator> IncubatorsInventory => GameClient.IncubatorsInventory;

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
                GotoPokemonDetailCommand.RaiseCanExecuteChanged();
                GotoEggDetailCommand.RaiseCanExecuteChanged();
                CloseMultipleSelectPokemonCommand.RaiseCanExecuteChanged();
                TransferMultiplePokemonCommand.RaiseCanExecuteChanged();
            }
        }

        public enum SelectionMode
        {
            GoToDetail,
            MultipleSelect
        };

        public SelectionMode currentSelectionMode;

        /// <summary>
        /// Collection of selected pokemons to transfer
        /// </summary>
        public ObservableCollection<PokemonDataWrapper> SelectedPokemons { get; private set; } =
            new ObservableCollection<PokemonDataWrapper>();

        /// <summary>
        /// Total amount of Pokemon in players inventory
        /// </summary>
        public int TotalPokemonCount {
            get { return PokemonInventory.Count + EggsInventory.Count; }
        }

        #endregion

        #region Game Logic

        #region Shared Logic

        private DelegateCommand _returnToGameScreen;

        /// <summary>
        /// Going back to map page
        /// </summary>
        public DelegateCommand ReturnToGameScreen => _returnToGameScreen ?? (
            _returnToGameScreen = new DelegateCommand(() => 
            {
                if (ServerRequestRunning) return;
                NavigationService.GoBack();
            }, () => true));

        #endregion

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

        #region Pokemon Detail

        /// <summary>
        /// Navigate to the detail page for the selected pokemon
        /// </summary>
        private DelegateCommand<PokemonDataWrapper> _gotoPokemonDetailCommand;
        public DelegateCommand<PokemonDataWrapper> GotoPokemonDetailCommand => _gotoPokemonDetailCommand ?? (_gotoPokemonDetailCommand = new DelegateCommand<PokemonDataWrapper>((selectedPokemon) =>
        {
            // If the multiple select mode is on, select additional pokemons in stead of going to the details
            if (currentSelectionMode == SelectionMode.MultipleSelect)
            {
                SelectPokemon(selectedPokemon);
            }
            else
            {
                NavigationService.Navigate(typeof(PokemonDetailPage), new SelectedPokemonNavModel()
                {
                    SelectedPokemonId = selectedPokemon.Id.ToString(),
                    SortingMode = CurrentPokemonSortingMode,
                    ViewMode = PokemonDetailPageViewMode.Normal
                }, new SuppressNavigationTransitionInfo());
            }
        }));

        /// <summary>
        /// Navigate to detail page for the selected egg
        /// </summary>
        private DelegateCommand<PokemonDataWrapper> _gotoEggDetailCommand;
        public DelegateCommand<PokemonDataWrapper> GotoEggDetailCommand => _gotoEggDetailCommand ?? (_gotoEggDetailCommand = new DelegateCommand<PokemonDataWrapper>((selectedEgg) =>
       {
           NavigationService.Navigate(typeof(EggDetailPage), selectedEgg.Id.ToString(), new SuppressNavigationTransitionInfo());
       }));

        #endregion

        #region Multiple Select and transfer
        private DelegateCommand<PokemonDataWrapper> _selectMultiplePokemonCommand;
        public DelegateCommand<PokemonDataWrapper> SelectMultiplePokemonCommand => _selectMultiplePokemonCommand ?? (_selectMultiplePokemonCommand = new DelegateCommand<PokemonDataWrapper>((selectedPokemon) =>
        {
            // switch the selectionMode, so that additional pokemons can be selected by a simple tap
            currentSelectionMode = SelectionMode.MultipleSelect;

            SelectPokemon(selectedPokemon);
        }));

        private void SelectPokemon(PokemonDataWrapper selectedPokemon)
        {
            if (SelectedPokemons.Contains(selectedPokemon))
            {
                SelectedPokemons.Remove(selectedPokemon);

                // If no pokemons are left selected, close the selection mode
                if (SelectedPokemons.Count == 0)
                {
                    currentSelectionMode = SelectionMode.GoToDetail;
                    MultiplePokemonSelected?.Invoke(null, selectedPokemon);
                }
            }
            else
            {
                if (selectedPokemon != null)
                {
                    SelectedPokemons.Add(selectedPokemon);

                    // When the first pokemon is selected, open selection mode
                    if (SelectedPokemons.Count == 1)
                    {
                        MultiplePokemonSelected?.Invoke(null, selectedPokemon);
                    }
                }
            }
            RaisePropertyChanged(() => SelectedPokemons);
            RaisePropertyChanged(() => SelectedPokemonCount);
        }

        public string SelectedPokemonCount
        {
            get { return "(" + SelectedPokemons.Count.ToString() + ")"; }
        }

        private DelegateCommand _closeMultipleSelectPokemonCommand;
        public DelegateCommand CloseMultipleSelectPokemonCommand => _closeMultipleSelectPokemonCommand ?? (_closeMultipleSelectPokemonCommand = new DelegateCommand(() =>
        {
            // Switch the selectionMode back to details
            currentSelectionMode = SelectionMode.GoToDetail;

            // Clear the selection of any pokemons
            SelectedPokemons.Clear();
            RaisePropertyChanged(() => SelectedPokemons);
            RaisePropertyChanged(() => SelectedPokemonCount);

            // Close the multiple selection view
            MultiplePokemonSelected?.Invoke(null, null);
        }));

        private DelegateCommand _transferMultiplePokemonCommand;
        public DelegateCommand TransferMultiplePokemonCommand => _transferMultiplePokemonCommand ?? (_transferMultiplePokemonCommand = new DelegateCommand(() =>
        {
            // build a list of PokemonIds and transfer them
            ulong[] pokemonIds = new ulong[SelectedPokemons.Count];

            int i = 0;
            foreach (PokemonDataWrapper pokemon in SelectedPokemons)
            {
                // Catch if the Pokémon is a Favorite or Buddy, transferring is not permitted in these cases
                // TODO: This isn't a MessageDialog in the original apps, implement error style (Shell needed)
                if (Convert.ToBoolean(pokemon.Favorite))
                {
                    var cannotTransferDialog = new PoGoMessageDialog(Resources.CodeResources.GetString("CannotTransferFavorite"), "")
                    {
                        CoverBackground = true,
                        AnimationType = PoGoMessageDialogAnimation.Bottom
                    };
                    cannotTransferDialog.Show();
                    return;
                }
                if (Convert.ToBoolean(pokemon.IsBuddy))
                {
                    var cannotTransferDialog = new PoGoMessageDialog(Resources.CodeResources.GetString("CannotTransferBuddy"), "")
                    {
                        CoverBackground = true,
                        AnimationType = PoGoMessageDialogAnimation.Bottom
                    };
                    cannotTransferDialog.Show();
                    return;
                }

                pokemonIds[i] = pokemon.Id;
                i++;
            }

            // Ask for confirmation before moving the Pokemons
            var dialog =
                new PoGoMessageDialog(
                    string.Format(Resources.CodeResources.GetString("TransferMultiplePokemonWarningTitle"), SelectedPokemons.Count),
                    Resources.CodeResources.GetString("TransferMultiplePokemonWarningText"))
                {
                    AcceptText = Resources.CodeResources.GetString("YesText"),
                    CancelText = Resources.CodeResources.GetString("NoText"),
                    CoverBackground = true,
                    AnimationType = PoGoMessageDialogAnimation.Bottom
                };

            dialog.AcceptInvoked += async (sender, e) =>
            {
                // User confirmed transfer
                try
                {
                    ServerRequestRunning = true;
                    var pokemonTransferResponse = await GameClient.TransferPokemons(pokemonIds);

                    switch (pokemonTransferResponse.Result)
                    {
                        case ReleasePokemonResponse.Types.Result.Unset:
                            break;
                        case ReleasePokemonResponse.Types.Result.Success:
                            // Remove the transferred pokemons from the inventory on screen
                            foreach (PokemonDataWrapper transferredPokemon in SelectedPokemons)
                            {
                                PokemonInventory.Remove(transferredPokemon);
                            }
                            RaisePropertyChanged(() => PokemonInventory);

                            // TODO: Implement message informing about success of transfer (Shell needed)
                            await GameClient.UpdateInventory();
                            await GameClient.UpdatePlayerStats();

                            // Reset to default screen
                            CloseMultipleSelectPokemonCommand.Execute();
                            break;

                        case ReleasePokemonResponse.Types.Result.PokemonDeployed:
                            break;
                        case ReleasePokemonResponse.Types.Result.Failed:
                            break;
                        case ReleasePokemonResponse.Types.Result.ErrorPokemonIsEgg:
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

        }, () => !ServerRequestRunning));
    }

    #endregion
    
    #endregion
}