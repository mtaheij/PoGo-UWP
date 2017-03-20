using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using System.Collections.ObjectModel;
using Template10.Common;
using Template10.Mvvm;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PokemonGo_UWP.Views
{
    public sealed partial class PokemonInventoryControl : UserControl
    {
        public PokemonInventoryControl()
        {
            this.InitializeComponent();
        }

        #region Properties

        public static readonly DependencyProperty PokemonInventoryProperty =
            DependencyProperty.Register(nameof(PokemonInventory), typeof(ObservableCollection<PokemonDataWrapper>), typeof(PokemonInventoryControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PokemonSelectedCommandProperty =
            DependencyProperty.Register(nameof(PokemonSelectedCommand), typeof(DelegateCommand<PokemonDataWrapper>), typeof(PokemonInventoryControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PokemonMultipleSelectedCommandProperty =
            DependencyProperty.Register(nameof(PokemonMultipleSelectedCommand), typeof(DelegateCommand<PokemonDataWrapper>), typeof(PokemonInventoryControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SortingModeProperty =
            DependencyProperty.Register(nameof(SortingMode), typeof(PokemonSortingModes), typeof(PokemonInventoryControl),
                new PropertyMetadata(PokemonSortingModes.Combat));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(PokemonInventoryControl),
                new PropertyMetadata(ListViewSelectionMode.Single));

        public ObservableCollection<PokemonDataWrapper> PokemonInventory
        {
            get { return (ObservableCollection<PokemonDataWrapper>)GetValue(PokemonInventoryProperty); }
            set { SetValue(PokemonInventoryProperty, value); }
        }

        public DelegateCommand<PokemonDataWrapper> PokemonSelectedCommand
        {
            get { return (DelegateCommand<PokemonDataWrapper>)GetValue(PokemonSelectedCommandProperty); }
            set { SetValue(PokemonSelectedCommandProperty, value); }
        }

        public DelegateCommand<PokemonDataWrapper> PokemonMultipleSelectedCommand
        {
            get { return (DelegateCommand<PokemonDataWrapper>)GetValue(PokemonMultipleSelectedCommandProperty); }
            set { SetValue(PokemonMultipleSelectedCommandProperty, value); }
        }

        public PokemonSortingModes SortingMode
        {
            get { return (PokemonSortingModes)GetValue(SortingModeProperty); }
            set { SetValue(SortingModeProperty, value); }
        }

        public ListViewSelectionMode SelectionMode
        {
            get { return (ListViewSelectionMode)GetValue(SelectionModeProperty); }
            set
            {
                SetValue(SelectionModeProperty, value);
                PokemonInventoryGridView.SelectionMode = value;
            }
        }
        #endregion

        #region Internal Methods

        private void ShowSortingPanel_Click(object sender, RoutedEventArgs e)
        {
            SortingMenuOverlayControl sortingMenu = new SortingMenuOverlayControl();
            sortingMenu.SortingmodeSelected += ((mode) => { SortingMode = mode; });
            sortingMenu.Show();
        }

        #endregion

        public void ClearSelectedPokemons()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                if (PokemonInventoryGridView.SelectedItems.Count > 0)
                {
                    PokemonInventoryGridView.SelectedItems.Clear();
                }
            });
        }

        public void SelectPokemon(PokemonDataWrapper selectedPokemon)
        {
            foreach(PokemonDataWrapper pdw in PokemonInventoryGridView.Items)
            {
                if (pdw == selectedPokemon)
                {
                PokemonInventoryGridView.SelectedItems.Add(pdw);
                }
            }
        }

        public void ShowHideSortingButton(bool Show)
        {
            if (Show)
            {
                ShowSortingButtonStoryboard.Begin();
            }
            else
            {
                HideSortingButtonStoryboard.Begin();
            }
        }
    }
}
