using Template10.Common;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class ItemsInventoryPage : Page
    {
        public ItemsInventoryPage()
        {
            InitializeComponent();
        }

        #region Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeToEvents();
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            // Hide the panel to use items
            HideUseItemStoryboard.Begin();
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs backRequestedEventArgs)
        {
            backRequestedEventArgs.Handled = true;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnsubscribeToEvents();
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
        }

        #endregion

        #region Handlers
        private void SubscribeToEvents()
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.AskForPokemonSelection += ViewModel_AskForPokemonSelection;
            ViewModel.PokemonSelectionCancelled += ViewModel_PokemonSelectionCancelled;
            ViewModel.ErrorDeployedToFort += ViewModel_ErrorDeployedToFort;
        }

        private void UnsubscribeToEvents()
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.AskForPokemonSelection -= ViewModel_AskForPokemonSelection;
            ViewModel.PokemonSelectionCancelled -= ViewModel_PokemonSelectionCancelled;
            ViewModel.ErrorDeployedToFort += ViewModel_ErrorDeployedToFort;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName=="CurrentUseItem")
            {
                if (ViewModel.CurrentUseItem == null)
                {
                    HideUseItemStoryboard.Begin();
                }
                else
                {
                    ShowUseItemStoryboard.Begin();
                }
            }
        }

        private void ViewModel_AskForPokemonSelection(object sender, System.EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                SelectPokemonGrid.Visibility = Visibility.Visible;
            });
        }

        private void ViewModel_PokemonSelectionCancelled(object sender, System.EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                SelectPokemonGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void ViewModel_ErrorDeployedToFort(object sender, System.EventArgs e)
        {
            ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPokemonDeployed");
            ErrorMessageText.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;
            ShowErrorMessageStoryboard.Completed += (ss, ee) => { HideErrorMessageStoryboard.Begin(); };
            ShowErrorMessageStoryboard.Begin();
        }

        #endregion
    }
}