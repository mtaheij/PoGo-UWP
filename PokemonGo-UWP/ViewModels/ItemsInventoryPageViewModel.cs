using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Views;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using PokemonGo_UWP.Controls;
using POGOProtos.Networking.Responses;
using System;
using POGOProtos.Inventory;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Data;
using PokemonGo_UWP.Utils.Extensions;
using POGOProtos.Inventory.Item;

namespace PokemonGo_UWP.ViewModels
{
    public class ItemsInventoryPageViewModel : ViewModelBase
    {
        #region Lifecycle Handlers

        /// <summary>
        /// Defines the modes, the ItemsInventoryPage can be viewed
        /// </summary>
        public enum ItemsInventoryViewMode
        {
            Normal,
            Catch
        }

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
                ItemsInventory = JsonConvert.DeserializeObject<ObservableCollection<ItemDataWrapper>>((string)suspensionState[nameof(ItemsInventory)]);
            }
            else if (parameter is bool)
            {
                // Navigating from game page, so we need to actually load the inventory
                // The sorting is directly bound to the ViewMode
                ItemsInventory = new ObservableCollection<ItemDataWrapper>(this.SortItems(
                    GameClient.ItemsInventory.Where(
                        itemData => ((POGOProtos.Inventory.Item.ItemData)itemData).Count > 0).Select(
                        itemData => new ItemDataWrapper(itemData))));

                RaisePropertyChanged(() => ItemsInventory);
            }

            ItemsTotalCount = ItemsInventory.Sum(i => i.WrappedData.Count);

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
            if (suspending)
            {
                suspensionState[nameof(ItemsInventory)] = JsonConvert.SerializeObject(ItemsInventory);
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Datahandling

        /// <summary>
        /// Orders the list of items accorfing to the viewmode set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        private IOrderedEnumerable<ItemDataWrapper> SortItems(IEnumerable<ItemDataWrapper> items)
        {
            var useableList = ViewMode == ItemsInventoryViewMode.Normal ? GameClient.NormalUseItemIds : GameClient.CatchItemIds;
            return items.OrderBy(item => !useableList.Contains(item.ItemId)).ThenBy(item => item.ItemId);
        }

        #endregion

        #region Bindable Game Vars

        /// <summary>
        ///     Reference to Items inventory
        /// </summary>
        public ObservableCollection<ItemDataWrapper> ItemsInventory { get; private set; } =
            new ObservableCollection<ItemDataWrapper>();

        private int _itemsTotalCount;
        public int ItemsTotalCount
        {
            get { return _itemsTotalCount; }
            private set { Set(ref _itemsTotalCount, value); }
        }

        public ItemsInventoryViewMode ViewMode { get; set; }

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
        /// Player's profile, we use it just for the maximum amount of pokemon
        /// </summary>
        private PlayerData _playerProfile;
        public PlayerData PlayerProfile
        {
            get { return _playerProfile; }
            set { Set(ref _playerProfile, value); }
        }

        /// <summary>
        /// Player's profile, we use it just for the maximum amount of pokemon
        /// </summary>
        private ItemDataWrapper _currentUseItem;
        public ItemDataWrapper CurrentUseItem
        {
            get { return _currentUseItem; }
            set { Set(ref _currentUseItem, value); }
        }

        /// <summary>
        /// Current selected Pokemon
        /// </summary>
        private PokemonDataWrapper _selectedPokemon;
        public PokemonDataWrapper SelectedPokemon
        {
            get { return _selectedPokemon; }
            set
            {
                Set(ref _selectedPokemon, value);
            }
        }
        #endregion

        #region Game Logic

        #region Shared Logic

        private DelegateCommand _returnToGameScreen;

        /// <summary>
        ///     Going back to map page
        /// </summary>
        public DelegateCommand ReturnToGameScreen
            =>
                _returnToGameScreen ??
                (_returnToGameScreen =
                    new DelegateCommand(() => { NavigationService.Navigate(typeof(GameMapPage)); }, () => true));

        public int MaxItemStorageFieldNumber => GameClient.PlayerData.MaxItemStorage;

        #endregion

        #region Use

        private DelegateCommand<ItemDataWrapper> _useItemCommand;

        public DelegateCommand<ItemDataWrapper> UseItemCommand => _useItemCommand ?? (
            _useItemCommand = new DelegateCommand<ItemDataWrapper>((ItemDataWrapper item) =>
            {
                if (item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemIncenseOrdinary ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemIncenseSpicy ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemIncenseFloral ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemIncenseCool)
                {
                    AskAndUseIncense(item);
                }

                if (item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemLuckyEgg)
                {
                    AskAndUseLuckyEgg(item);
                }

                if (item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemPotion ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemHyperPotion ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemMaxPotion ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemSuperPotion)
                {
                    AskAndUsePotion(item);
                }

                if (item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemRevive ||
                    item.ItemId == POGOProtos.Inventory.Item.ItemId.ItemMaxRevive)
                {
                    AskAndUseRevive(item);
                }

            }, (ItemDataWrapper item) => true));

        private void AskAndUseIncense(ItemDataWrapper item)
        {
            if (!GameClient.IsIncenseActive)
            {
                var dialog = new PoGoMessageDialog("", string.Format(Resources.CodeResources.GetString("ItemUseQuestionText"), Resources.Items.GetString(item.ItemId.ToString())));
                dialog.AcceptText = Resources.CodeResources.GetString("YesText");
                dialog.CancelText = Resources.CodeResources.GetString("CancelText");
                dialog.CoverBackground = true;
                dialog.AnimationType = PoGoMessageDialogAnimation.Bottom;
                dialog.AcceptInvoked += async (sender, e) =>
                {
                    //// Send use request
                    var res = await GameClient.UseIncense(item.ItemId);
                    switch (res.Result)
                    {
                        case UseIncenseResponse.Types.Result.Success:
                            GameClient.AppliedItems.Add(new AppliedItemWrapper(res.AppliedIncense));
                            ReturnToGameScreen.Execute();
                            break;
                        case UseIncenseResponse.Types.Result.IncenseAlreadyActive:
                            ReturnToGameScreen.Execute();
                            break;
                        case UseIncenseResponse.Types.Result.LocationUnset:
                        case UseIncenseResponse.Types.Result.NoneInInventory:
                        case UseIncenseResponse.Types.Result.Unknown:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                };

                dialog.Show();
            }
        }

        private void AskAndUseLuckyEgg(ItemDataWrapper item)
        {
            if (!GameClient.IsXpBoostActive)
            {
                var dialog = new PoGoMessageDialog("", string.Format(Resources.CodeResources.GetString("ItemUseQuestionText"), Resources.Items.GetString(item.ItemId.ToString())));
                dialog.AcceptText = Resources.CodeResources.GetString("YesText");
                dialog.CancelText = Resources.CodeResources.GetString("CancelText");
                dialog.CoverBackground = true;
                dialog.AnimationType = PoGoMessageDialogAnimation.Bottom;
                dialog.AcceptInvoked += async (sender, e) =>
                {
                    // Send use request
                    var res = await GameClient.UseXpBoost(item.ItemId);
                    switch (res.Result)
                    {
                        case UseItemXpBoostResponse.Types.Result.Success:
                            AppliedItem appliedItem = res.AppliedItems.Item.FirstOrDefault<AppliedItem>();
                            GameClient.AppliedItems.Add(new AppliedItemWrapper(appliedItem));
                            ReturnToGameScreen.Execute();
                            break;
                        case UseItemXpBoostResponse.Types.Result.ErrorXpBoostAlreadyActive:
                            ReturnToGameScreen.Execute();
                            break;
                        case UseItemXpBoostResponse.Types.Result.ErrorInvalidItemType:
                        case UseItemXpBoostResponse.Types.Result.ErrorLocationUnset:
                        case UseItemXpBoostResponse.Types.Result.ErrorNoItemsRemaining:
                        case UseItemXpBoostResponse.Types.Result.Unset:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                };

                dialog.Show();
            }
        }

        /// <summary>
        ///     Event fired when the ItemsInventoryPage has to show the Pokemon selection control
        /// </summary>
        public event EventHandler AskForPokemonSelection;

        /// <summary>
        ///     Event fired when the Selection of a pokemon (to revive or use a potion on) has been cancelled
        /// </summary>
        public event EventHandler PokemonSelectionCancelled;

        /// <summary>
        ///     Event fired when a potion is used, but the affected Pokemon is deployed to a Fort
        /// </summary>
        public event EventHandler ErrorDeployedToFort;

        private void AskAndUsePotion(ItemDataWrapper item)
        {
            PokemonInventory = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                .Where(pokemonData => pokemonData.Stamina != pokemonData.StaminaMax));

            CurrentUseItem = item;

            RaisePropertyChanged(() => PokemonInventory);
            RaisePropertyChanged(() => CurrentUseItem);

            AskForPokemonSelection?.Invoke(this, null);
        }

        private void AskAndUseRevive(ItemDataWrapper item)
        {
            PokemonInventory = new ObservableCollection<PokemonDataWrapper>(GameClient.PokemonsInventory
                .Select(pokemonData => new PokemonDataWrapper(pokemonData))
                .Where(pokemonData => pokemonData.Stamina ==0));

            CurrentUseItem = item;

            RaisePropertyChanged(() => PokemonInventory);
            RaisePropertyChanged(() => CurrentUseItem);

            AskForPokemonSelection?.Invoke(this, null);
        }

        // Return to the inventory
        private DelegateCommand _returnToInventoryCommand;

        public DelegateCommand ReturnToInventoryCommand =>
            _returnToInventoryCommand ?? (
            _returnToInventoryCommand = new DelegateCommand(() =>
            {
                CurrentUseItem = null;
                RaisePropertyChanged(() => CurrentUseItem);

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
                SelectedPokemon = selectedPokemon;

                if (CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemPotion ||
                    CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemSuperPotion ||
                    CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemMaxPotion ||
                    CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemHyperPotion)
                {
                    UsePotion(selectedPokemon, CurrentUseItem.ItemId);
                }
                if (CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemRevive ||
                    CurrentUseItem.ItemId == POGOProtos.Inventory.Item.ItemId.ItemMaxRevive)
                {
                    UseRevive(selectedPokemon, CurrentUseItem.ItemId);
                }
            }));

        private async void UsePotion(PokemonDataWrapper pokemon, ItemId item)
        {
            try
            {
                ServerRequestRunning = true;
                // Use local var to prevent bug when changing selected pokemon during running request
                var affectingPokemon = SelectedPokemon;
                // Send potion request
                var res = await GameClient.UseItemPotion(item, affectingPokemon.Id);
                switch (res.Result)
                {
                    case UseItemPotionResponse.Types.Result.Unset:
                        break;
                    case UseItemPotionResponse.Types.Result.Success:
                        // Reload updated data
                        bool selectedPokemonSameAsAffecting = affectingPokemon == SelectedPokemon;
                        PokemonInventory.Remove(affectingPokemon);
                        var affectedPokemon = new PokemonDataWrapper(affectingPokemon.WrappedData);
                        affectedPokemon.SetStamina(res.Stamina);
                        if (affectedPokemon.Stamina < affectedPokemon.StaminaMax)
                        {
                            PokemonInventory.Add(affectedPokemon);
                        }
                        PokemonInventory.SortBySortingmode(CurrentPokemonSortingMode);

                        CurrentUseItem.Count--;

                        // If the affecting pokemon is still showing (not fliped to other), change selected to affected
                        if (selectedPokemonSameAsAffecting)
                        {
                            SelectedPokemon = affectedPokemon;
                            RaisePropertyChanged(nameof(SelectedPokemon));
                            RaisePropertyChanged(nameof(CurrentUseItem));
                        }
                        await GameClient.UpdateInventory();
                        await GameClient.UpdateProfile();
                        break;
                    case UseItemPotionResponse.Types.Result.ErrorDeployedToFort:
                        ErrorDeployedToFort?.Invoke(this, null);
                        break;
                    case UseItemPotionResponse.Types.Result.ErrorCannotUse:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                ServerRequestRunning = false;
            }
        }

        private async void UseRevive(PokemonDataWrapper pokemon, ItemId item)
        {
            try
            {
                ServerRequestRunning = true;
                // Use local var to prevent bug when changing selected pokemon during running request
                var affectingPokemon = SelectedPokemon;
                // Send revive request
                var res = await GameClient.UseItemRevive(item, pokemon.Id);
                switch (res.Result)
                {
                    case UseItemReviveResponse.Types.Result.Unset:
                        break;
                    case UseItemReviveResponse.Types.Result.Success:
                        // Reload updated data
                        bool selectedPokemonSameAsAffecting = affectingPokemon == SelectedPokemon;
                        PokemonInventory.Remove(affectingPokemon);
                        var affectedPokemon = new PokemonDataWrapper(affectingPokemon.WrappedData);
                        affectedPokemon.SetStamina(res.Stamina);
                        if (affectedPokemon.Stamina < affectedPokemon.StaminaMax)
                        {
                            PokemonInventory.Add(affectedPokemon);
                        }
                        PokemonInventory.SortBySortingmode(CurrentPokemonSortingMode);

                        CurrentUseItem.Count--;

                        // If the affecting pokemon is still showing (not fliped to other), change selected to affected
                        if (selectedPokemonSameAsAffecting)
                        {
                            SelectedPokemon = affectedPokemon;
                            RaisePropertyChanged(nameof(SelectedPokemon));
                            RaisePropertyChanged(nameof(CurrentUseItem));
                        }
                        await GameClient.UpdateInventory();
                        await GameClient.UpdateProfile();
                        break;
                    case UseItemReviveResponse.Types.Result.ErrorDeployedToFort:
                        ErrorDeployedToFort?.Invoke(this, null);
                        break;
                    case UseItemReviveResponse.Types.Result.ErrorCannotUse:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                ServerRequestRunning = false;
            }
        }

        #endregion

        #region Pokemon Inventory Handling
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

        #region Recycle

        private DelegateCommand<ItemDataWrapper> _recycleItemCommand;

        public DelegateCommand<ItemDataWrapper> RecycleItemCommand => _recycleItemCommand ?? (
          _recycleItemCommand = new DelegateCommand<ItemDataWrapper>((ItemDataWrapper item) =>
          {

              var dialog = new PoGoMessageDialog("", string.Format(Resources.CodeResources.GetString("ItemDiscardWarningText"), Resources.Items.GetString(item.ItemId.ToString())));
              var stepper = new StepperMessageDialog(1, item.Count, 1);
              dialog.DialogContent = stepper;
              dialog.AcceptText = Resources.CodeResources.GetString("YesText");
              dialog.CancelText = Resources.CodeResources.GetString("CancelText");
              dialog.CoverBackground = true;
              dialog.AnimationType = PoGoMessageDialogAnimation.Bottom;
              dialog.AcceptInvoked += async (sender, e) =>
              {
                  // Send recycle request
                  var res = await GameClient.RecycleItem(item.ItemId, stepper.Value);
                  switch (res.Result)
                  {
                      case RecycleInventoryItemResponse.Types.Result.Unset:
                          break;
                      case RecycleInventoryItemResponse.Types.Result.Success:
                          // Refresh the Item amount
                          item.WrappedData.Count = res.NewCount;
                          // Hacky? you guessed it...
                          item.Update(item.WrappedData);

                          // Handle if there are no more items of this type
                          if(res.NewCount == 0)
                          {
                              GameClient.ItemsInventory.Remove(item.WrappedData);
                              ItemsInventory.Remove(item);
                          }
                          // Update the total count
                          ItemsTotalCount = ItemsInventory.Sum(i => i.WrappedData.Count);
                          break;
                      case RecycleInventoryItemResponse.Types.Result.ErrorNotEnoughCopies:
                          break;
                      case RecycleInventoryItemResponse.Types.Result.ErrorCannotRecycleIncubators:
                          break;
                      default:
                          throw new ArgumentOutOfRangeException();
                  }
              };

              dialog.Show();
          }, (ItemDataWrapper item) => true));

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
                SelectPokemonCommand.RaiseCanExecuteChanged();
                ReturnToInventoryCommand.RaiseCanExecuteChanged();
            }
        }
    }
}