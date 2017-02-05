using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using PokemonGo_UWP.Entities;

namespace PokemonGo_UWP.Views
{
    public sealed partial class PokemonInventoryPage : Page
	{
		public PokemonInventoryPage()
		{
			InitializeComponent();

			NavigationCacheMode = NavigationCacheMode.Enabled;
		}

        #region  Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.ScrollPokemonToVisibleRequired += ScrollPokemonToVisible;
            ViewModel.MultiplePokemonSelected += ViewModel_MultiplePokemonSelected;

            // Hide the multiple select panel
            HideMultipleSelectStoryboard.Begin();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ViewModel.ScrollPokemonToVisibleRequired -= ScrollPokemonToVisible;
            ViewModel.MultiplePokemonSelected -= ViewModel_MultiplePokemonSelected;
        }

        #endregion

        private void ScrollPokemonToVisible(PokemonDataWrapper p)
        {
            PokemonInventory.PokemonInventoryGridView.ScrollIntoView(p);
        }

        private void ViewModel_MultiplePokemonSelected(object sender, PokemonDataWrapper e)
        {
            // Show or hide the 'Transfer multiple' button at the bottom of the screen
            if (ViewModel.SelectedPokemons.Count > 0)
            {
                PokemonInventory.SelectPokemon(e);
                ShowMultipleSelectStoryboard.Begin();
                PokemonInventory.ShowHideSortingButton(false);
            }
            else
            {
                PokemonInventory.ClearSelectedPokemons();
                HideMultipleSelectStoryboard.Begin();
                PokemonInventory.ShowHideSortingButton(true);
            }
        }

    }
}