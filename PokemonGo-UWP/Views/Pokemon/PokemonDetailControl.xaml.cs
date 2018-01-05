using POGOProtos.Settings.Master;
using Template10.Mvvm;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PokemonGo_UWP.Views
{
    public sealed partial class PokemonDetailControl : UserControl
    {
        public PokemonDetailControl()
        {
            this.InitializeComponent();

            DataContextChanged += DataContextChangedEvent;

            PokemonTypeCol.MinWidth = PokemonTypeCol.ActualWidth;
            PokemonTypeCol.Width = new GridLength(1, GridUnitType.Star);
        }

        private void DataContextChangedEvent(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ContentScroll.ChangeView(0.0, 0.0, 1.0f);
        }

        #region Dependency Propertys

        public static readonly DependencyProperty FavoritePokemonCommandProperty = 
            DependencyProperty.Register(nameof(FavoritePokemonCommand), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty RenamePokemonCommandProperty =
            DependencyProperty.Register(nameof(RenamePokemonCommand), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PowerUpPokemonCommandProperty =
            DependencyProperty.Register(nameof(PowerUpPokemonCommand), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty EvolvePokemonCommandProperty =
            DependencyProperty.Register(nameof(EvolvePokemonCommand), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty BuddyPokemonCommandProperty =
            DependencyProperty.Register(nameof(BuddyPokemonCommand), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty StardustAmountProperty =
            DependencyProperty.Register(nameof(StardustAmount), typeof(int), typeof(PokemonDetailControl),
                new PropertyMetadata(0));

        public static readonly DependencyProperty PokemonExtraDataProperty =
            DependencyProperty.Register(nameof(PokemonExtraData), typeof(DelegateCommand), typeof(PokemonDetailControl),
                new PropertyMetadata(null));

        public DelegateCommand FavoritePokemonCommand
        {
            get { return (DelegateCommand)GetValue(FavoritePokemonCommandProperty); }
            set { SetValue(FavoritePokemonCommandProperty, value); }
        }

        public DelegateCommand RenamePokemonCommand
        {
            get { return (DelegateCommand)GetValue(RenamePokemonCommandProperty); }
            set { SetValue(RenamePokemonCommandProperty, value); }
        }

        public DelegateCommand PowerUpPokemonCommand
        {
            get { return (DelegateCommand)GetValue(PowerUpPokemonCommandProperty); }
            set { SetValue(PowerUpPokemonCommandProperty, value); }
        }

        public DelegateCommand EvolvePokemonCommand
        {
            get { return (DelegateCommand)GetValue(EvolvePokemonCommandProperty); }
            set { SetValue(EvolvePokemonCommandProperty, value); }
        }

        public DelegateCommand BuddyPokemonCommand
        {
            get { return (DelegateCommand)GetValue(BuddyPokemonCommandProperty); }
            set { SetValue(BuddyPokemonCommandProperty, value); }
        }

        public int StardustAmount
        {
            get { return (int)GetValue(StardustAmountProperty); }
            set { SetValue(StardustAmountProperty, value); }
        }

        public PokemonSettings PokemonExtraData
        {
            get { return (PokemonSettings)GetValue(PokemonExtraDataProperty); }
            set { SetValue(PokemonExtraDataProperty, value); }
        }

        #endregion

    }
}
