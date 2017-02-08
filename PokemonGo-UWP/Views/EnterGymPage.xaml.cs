using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PokemonGo_UWP.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EnterGymPage : Page
    {
        public EnterGymPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                // Of course binding doesn't work so we need to manually setup height for animations
            };
        }

        #region Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeToEnterEvents();
        }


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnsubscribeToEnterEvents();
        }

        #endregion

        #region Handlers

        private void SubscribeToEnterEvents()
        {
            ViewModel.EnterOutOfRange += GameManagerViewModelOnEnterOutOfRange;
            ViewModel.EnterSuccess += GameManagerViewModelOnEnterSuccess;

            ViewModel.PlayerLevelInsufficient += GameManagerViewModelOnPlayerLevelInsufficient;
        }

        private void UnsubscribeToEnterEvents()
        {
            ViewModel.EnterOutOfRange -= GameManagerViewModelOnEnterOutOfRange;
            ViewModel.EnterSuccess -= GameManagerViewModelOnEnterSuccess;

            ViewModel.PlayerLevelInsufficient -= GameManagerViewModelOnPlayerLevelInsufficient;
        }

        private void GameManagerViewModelOnEnterOutOfRange(object sender, EventArgs eventArgs)
        {            
            OutOfRangeTextBlock.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;
        }

        private void GameManagerViewModelOnEnterSuccess(object sender, EventArgs eventArgs)
        {
        }

        private void GameManagerViewModelOnPlayerLevelInsufficient(object sender, EventArgs e)
        {
            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Light, false);
            dialog.Messages.Add("This is a Gym, a place where you'll test your skills at Pokémon battles.");
            dialog.Messages.Add("It looks like you don't have much experience as a Pokémon Trainer yet.");
            dialog.Messages.Add("Come back when you've reached level 5!");

            dialog.Closed += Dialog_Closed;
            dialog.Show();
        }

        private void Dialog_Closed(object sender, EventArgs e)
        {
            ViewModel.AbandonGym.Execute();
        }

        #endregion
    }
}