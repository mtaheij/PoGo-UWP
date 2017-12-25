using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Utils.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class TutorialPage : Page
    {
        public TutorialPage()
        {
            this.InitializeComponent();
        }

        #region Overrides of Page

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeToEvents();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnsubscribeToEvents();
        }

        #endregion

        #region Handlers

        private void SubscribeToEvents()
        {
            ViewModel.HideAllScreens += GameManagerViewModelOnHideAllScreens;
            ViewModel.ShowLegalScreen += GameManagerViewModelOnShowLegalScreen;
            ViewModel.ShowSelectAvatarScreen += GameManagerViewModelOnShowSelectAvatarScreen;
            ViewModel.ShowPokemonCaptureScreen += GameManagerViewModelOnShowPokemonCaptureScreen;
            ViewModel.ShowNameSelectionScreen += GameManagerViewModelOnShowNameSelectionScreen;
            ViewModel.RequestError += GameManagerViewModelOnRequestError;

            ViewModel.AvatarMaleSelected += GameManagerViewModelOnAvatarMaleSelected;
            ViewModel.AvatarFemaleSelected += GameManagerViewModelOnAvatarFemaleSelected;
            ViewModel.AvatarOkSelected += GameManagerViewModelOnAvatarOkSelected;

            ViewModel.PokemonCaptured += GameManagerViewModelOnPokemonCaptured;

            ViewModel.NicknameEntered += GameManagerViewModelOnNicknameEntered;
            ViewModel.NicknameOkSubmitted += GameManagerViewModelOnNicknameOkSubmitted;
            ViewModel.NicknameCancelled += GameManagerViewModelOnNicknameCancelled;

            ViewModel.ShowItsTimeToWalkScreen += GameManagerViewModelOnShowItsTimeToWalkScreen;

            ViewModel.TutorialSkipRequested += GameManagerViewModelOnTutorialSkipRequested;
            ViewModel.TutorialSkipCancelled += GameManagerViewModelOnTutorialSkipCancelled;

        }

        private void UnsubscribeToEvents()
        {
            ViewModel.HideAllScreens -= GameManagerViewModelOnHideAllScreens;
            ViewModel.ShowLegalScreen -= GameManagerViewModelOnShowLegalScreen;
            ViewModel.ShowSelectAvatarScreen -= GameManagerViewModelOnShowSelectAvatarScreen;
            ViewModel.ShowPokemonCaptureScreen -= GameManagerViewModelOnShowPokemonCaptureScreen;
            ViewModel.ShowNameSelectionScreen -= GameManagerViewModelOnShowNameSelectionScreen;
            ViewModel.RequestError -= GameManagerViewModelOnRequestError;

            ViewModel.AvatarMaleSelected -= GameManagerViewModelOnAvatarMaleSelected;
            ViewModel.AvatarFemaleSelected -= GameManagerViewModelOnAvatarFemaleSelected;
            ViewModel.AvatarOkSelected -= GameManagerViewModelOnAvatarOkSelected;

            ViewModel.PokemonCaptured -= GameManagerViewModelOnPokemonCaptured;

            ViewModel.NicknameEntered -= GameManagerViewModelOnNicknameEntered;
            ViewModel.NicknameOkSubmitted -= GameManagerViewModelOnNicknameOkSubmitted;
            ViewModel.NicknameCancelled -= GameManagerViewModelOnNicknameCancelled;

            ViewModel.ShowItsTimeToWalkScreen -= GameManagerViewModelOnShowItsTimeToWalkScreen;

            ViewModel.TutorialSkipRequested -= GameManagerViewModelOnTutorialSkipRequested;
            ViewModel.TutorialSkipCancelled -= GameManagerViewModelOnTutorialSkipCancelled;
        }

        private void GameManagerViewModelOnHideAllScreens(object sender, EventArgs e)
        {
            LegalScreenGrid.Visibility = 
            SelectAvatarGrid.Visibility =
            CatchPokemonGrid.Visibility =
            ChooseNicknameGrid.Visibility = Visibility.Collapsed;
        }

        private void GameManagerViewModelOnRequestError(object sender, string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;

            ShowErrorMessageStoryboard.Completed += (ss, ee) => { HideErrorMessageStoryboard.Begin(); };
            ShowErrorMessageStoryboard.Begin();
        }

        #region Legal/disclaimer screen
        private void GameManagerViewModelOnShowLegalScreen(object sender, EventArgs eventArgs)
        {
            LegalScreenGrid.Visibility = Visibility.Visible;
        }
        #endregion

        #region Select Avatar
        private void GameManagerViewModelOnShowSelectAvatarScreen(object sender, EventArgs e)
        {
            AudioUtils.PlaySound(AudioUtils.PROFESSOR);

            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Dark, false);
            dialog.Show();

            dialog = new ProfessorDialog(BackGroundType.Dark, true);
            dialog.Messages.Add(new MessageEntry("Hello there! I am Professor Willow.", 60));
            dialog.Messages.Add(new MessageEntry("Did you know that this world is inhabited by creatures known as Pokémon?", 90));
            dialog.Messages.Add(new MessageEntry("Pokémon can be found in every corner of the earth.", 60));
            dialog.Messages.Add(new MessageEntry("Some run across the plains, others fly through the skies, some live in the mountains, or in the forests, or near water...", 120));
            dialog.Messages.Add(new MessageEntry("I have spent my whole life studying them and their regional distribution.", 60));
            dialog.Messages.Add(new MessageEntry("Will you help me with my research?", 60));
            dialog.Messages.Add(new MessageEntry("That's great! I was just looking for someone like you to help!", 60));
            dialog.Messages.Add(new MessageEntry("You'll need to find and collect Pokémon from everywhere!", 60));
            dialog.Messages.Add(new MessageEntry("Now, choose your style for your adventure.", 60));
            dialog.Show();

            ShowSelectAvatarStoryboard.Begin();
        }

        private void GameManagerViewModelOnAvatarMaleSelected(object sender, EventArgs e)
        {
            SelectAvatarMaleSelectedStoryboard.Begin();
        }

        private void GameManagerViewModelOnAvatarFemaleSelected(object sender, EventArgs e)
        {
            SelectAvatarFemaleSelectedStoryboard.Begin();
        }

        private void GameManagerViewModelOnAvatarOkSelected(object sender, EventArgs e)
        {

        }
        #endregion

        #region PokemonCapture
        private async void GameManagerViewModelOnShowPokemonCaptureScreen(object sender, EventArgs e)
        {
            if (GameClient.PokemonSettings.Count() == 0)
            {
                await GameClient.LoadGameSettings(true);
            }

            if (LocationServiceHelper.Instance.Geoposition != null)
            {
                await UpdateMap(LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                ViewModel.CreateStarterPokemons(LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
            }

            ShowPokemonCatchStoryboard.Begin();

            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Dark, false);
            dialog.SetToLowerRightCorner();
            dialog.SetTranslucent(true);
            dialog.Messages.Add(new MessageEntry("There's a Pokémon nearby!", 60));
            dialog.Messages.Add(new MessageEntry("Here are some Poké Balls. These will help you catch one!", 60));
            dialog.Closed += ProfessorCaptureDialog_Closed;
            dialog.Show();
        }

        private void GameManagerViewModelOnPokemonCaptured(object sender, EventArgs e)
        {
        }

        private void ProfessorCaptureDialog_Closed(object sender, EventArgs e)
        {
            CatchMessageBorder.Visibility = Visibility.Visible;
        }

        private void BuildBindingList(FrameworkElement element)
        {
            FieldInfo[] infos = element.GetType().GetFields(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Static);

            foreach (FieldInfo field in infos)
            {
                if (field.FieldType == typeof(DependencyProperty))
                {
                    DependencyProperty dp = (DependencyProperty)field.GetValue(null);
                    BindingExpression ex = element.GetBindingExpression(dp);

                    if (ex != null)
                    {
                        Debug.WriteLine("Binding found with path: " + ex.ParentBinding.Path.Path);
                    }
                }
            }

            int children = VisualTreeHelper.GetChildrenCount(element);

            for (int i = 0; i < children; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child != null)
                {
                    BuildBindingList(child);
                }
            }
        }

        private async Task UpdateMap(Geopoint location)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    // Set map's center location
                    GameMapControl.Center = location;

                    // Set player icon's position
                    MapControl.SetLocation(PlayerImage, location);
                }
                catch (Exception ex)
                {
                    await ExceptionHandler.HandleException(ex);
                }
            });
        }

        #endregion

        #region Name Selection
        private async void GameManagerViewModelOnShowNameSelectionScreen(object sender, EventArgs e)
        {
            if (LocationServiceHelper.Instance.Geoposition != null)
            {
                await UpdateMap(LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                ViewModel.ClearStarterPokemons();
            }

            ShowPokemonCatchStoryboard.Begin();

            AudioUtils.PlaySound(AudioUtils.PROFESSOR);

            ButtonSkipTutorial.Visibility = Visibility.Collapsed;

            ProfessorDialog dialog = new Views.ProfessorDialog(BackGroundType.Dark, false);
            dialog.SetToLowerRightCorner();
            dialog.SetTranslucent(true);
            dialog.Messages.Add(new MessageEntry("Congratulations! You've caught your first Pokémon!", 60));
            dialog.Messages.Add(new MessageEntry("You are such a talented Pokémon Trainer! What should I call you?", 60));
            dialog.Closed += ProfessorNameDialog_Closed;
            dialog.Show();
        }

        private void ProfessorNameDialog_Closed(object sender, EventArgs e)
        {
            ButtonSkipTutorial.Visibility = Visibility.Visible;

            ShowChooseNicknameStoryboard.Begin();
        }

        private void GameManagerViewModelOnNicknameEntered(object sender, EventArgs e)
        {
            ViewModel.SelectedNickname = NicknameTextBox.Text;
            NicknameConfirmationTextBlock.Text = String.Format("Are you sure you want to be called {0}?", NicknameTextBox.Text);
            ShowConfirmNicknameStoryboard.Begin();
        }

        private void GameManagerViewModelOnNicknameCancelled(object sender, EventArgs e)
        {
            NicknameTextBox.Text = String.Empty;
            HideConfirmNicknameStoryboard.Begin();
        }

        private void GameManagerViewModelOnNicknameOkSubmitted(object sender, EventArgs e)
        {
            HideChooseNicknameStoryboard.Begin();

            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Dark, false);
            dialog.SetToLowerRightCorner();
            dialog.SetTranslucent(true);

            ButtonSkipTutorial.Visibility = Visibility.Collapsed;

            dialog.Messages.Add(new MessageEntry("Oh, what a cool nickname! Nice to meet you!", 60));
            dialog.Messages.Add(new MessageEntry("You will need more Poké Balls and other useful items during your exploration.", 90));
            dialog.Closed += DialogNeedPokestop;
            dialog.Show();
        }

        private void DialogNeedPokestop(object sender, EventArgs e)
        {
            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Dark, false);
            dialog.SetToLowerRightCorner();
            dialog.SetTranslucent(true);
            dialog.ShowPokeStop();
            dialog.Messages.Add(new MessageEntry("You can find items at PokéStops.", 60));
            dialog.Messages.Add(new MessageEntry("They're found at interesting places like sculptures and monuments.", 60));
            dialog.Messages.Add(new MessageEntry("From now on, you'll be off exploring all over the world. I hope you get out there and catch Pokémon-and register them in your Pokédex!", 120));
            dialog.Messages.Add(new MessageEntry("It's time to GO!", 60));
            dialog.Closed += DialogItsTimeToGO;
            dialog.Show();
        }

        private async void DialogItsTimeToGO(object sender, EventArgs e)
        {
            if (LocationServiceHelper.Instance.Geoposition != null)
            {
                await UpdateMap(LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                ViewModel.ClearStarterPokemons();
            }

            ShowPokemonCatchStoryboard.Begin();
            ButtonSkipTutorial.Visibility = Visibility.Visible;

            AudioUtils.PlaySound(AudioUtils.PROFESSOR);

            TimeToWalkGrid.Visibility = Visibility.Visible;
            ShowTimeToWalkStoryboard.Begin();
        }

        private async void GameManagerViewModelOnShowItsTimeToWalkScreen(object sender, EventArgs e)
        {
            if (LocationServiceHelper.Instance.Geoposition != null)
            {
                await UpdateMap(LocationServiceHelper.Instance.Geoposition.Coordinate.Point);
                ViewModel.ClearStarterPokemons();
            }

            ShowPokemonCatchStoryboard.Begin();
            ButtonSkipTutorial.Visibility = Visibility.Visible;

            AudioUtils.PlaySound(AudioUtils.PROFESSOR);

            TimeToWalkGrid.Visibility = Visibility.Visible;
            ShowTimeToWalkStoryboard.Begin();
        }
        #endregion

        #region Skip Tutorial
        private void GameManagerViewModelOnTutorialSkipRequested(object sender, EventArgs e)
        {
            ShowConfirmSkipTutorialStoryboard.Begin();
        }

        private void GameManagerViewModelOnTutorialSkipCancelled(object sender, EventArgs e)
        {
            HideConfirmSkipTutorialStoryboard.Begin();
        }
        #endregion

        #endregion

    }
}
