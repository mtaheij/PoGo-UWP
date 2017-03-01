using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using PokemonGo_UWP.Utils;
using System;
using System.Threading.Tasks;
using Template10.Common;
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
            ViewModel.GymLoaded += GameManagerViewModelOnGymLoaded;

            ViewModel.EnterOutOfRange += GameManagerViewModelOnEnterOutOfRange;
            ViewModel.EnterSuccess += GameManagerViewModelOnEnterSuccess;

            ViewModel.PlayerLevelInsufficient += GameManagerViewModelOnPlayerLevelInsufficient;
            ViewModel.PlayerTeamUnset += GameManagerViewModelOnPlayerTeamUnset;

            ViewModel.PlayerTeamSet += GameManagerViewModelOnPlayerTeamSet;
            ViewModel.AskForPokemonSelection += GameManagerViewModelOnAskForPokemonSelection;
            ViewModel.PokemonDeployed += GameManagerViewModelOnPokemonDeployed;
            ViewModel.DeployPokemonError += GameManagerViewModelOnDeployPokemonError;
            ViewModel.PokemonSelectionCancelled += GameManagerViewModelOnPokemonSelectionCancelled;
        }

        private void UnsubscribeToEnterEvents()
        {
            ViewModel.GymLoaded -= GameManagerViewModelOnGymLoaded;

            ViewModel.EnterOutOfRange -= GameManagerViewModelOnEnterOutOfRange;
            ViewModel.EnterSuccess -= GameManagerViewModelOnEnterSuccess;

            ViewModel.PlayerLevelInsufficient -= GameManagerViewModelOnPlayerLevelInsufficient;
            ViewModel.PlayerTeamUnset -= GameManagerViewModelOnPlayerTeamUnset;

            ViewModel.PlayerTeamSet -= GameManagerViewModelOnPlayerTeamSet;
            ViewModel.AskForPokemonSelection -= GameManagerViewModelOnAskForPokemonSelection;
            ViewModel.PokemonDeployed -= GameManagerViewModelOnPokemonDeployed;
            ViewModel.DeployPokemonError -= GameManagerViewModelOnDeployPokemonError;
            ViewModel.PokemonSelectionCancelled -= GameManagerViewModelOnPokemonSelectionCancelled;
        }

        private void GameManagerViewModelOnGymLoaded(object sender, EventArgs eventArgs)
        {
            GymMembersControl.DataContext = null;
            GymMembersControl.DataContext = ViewModel;

            GymMembersControl.GymMemberships = ViewModel.GymMemberships;
        }

        private void GameManagerViewModelOnEnterOutOfRange(object sender, EventArgs eventArgs)
        {
            OutOfRangeTextBlock.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;
        }

        private void GameManagerViewModelOnEnterSuccess(object sender, EventArgs eventArgs)
        {
        }

        private void GameManagerViewModelOnAskForPokemonSelection(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                SelectPokemonGrid.Visibility = Visibility.Visible;
            });
        }

        private void GameManagerViewModelOnPokemonDeployed(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                SelectPokemonGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void GameManagerViewModelOnDeployPokemonError(object sender, FortDeployPokemonResponse.Types.Result result)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                switch (result)
                {
                    case FortDeployPokemonResponse.Types.Result.ErrorAlreadyHasPokemonOnFort:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorAlreadyHasPokemonOnFort");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorFortDeployLockout:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorFortDeployLockout");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorFortIsFull:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorFortIsFull");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorNotInRange:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorNotInRange");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorOpposingTeamOwnsFort:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorOpposingTeamOwnsFort");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorPlayerHasNoNickname:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPlayerHasNoNickname");
                        break;
                    case FortDeployPokemonResponse.Types.Result.ErrorPokemonNotFullHp:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPokemonNotFullHp");
                        break;
                }
                ErrorMessageText.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;

                SelectPokemonGrid.Visibility = Visibility.Collapsed;

                ShowErrorMessageStoryboard.Completed += (ss, ee) => { HideErrorMessageStoryboard.Begin(); };
                ShowErrorMessageStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnPokemonSelectionCancelled(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                PokemonInventorySelector.ClearSelectedPokemons();
                SelectPokemonGrid.Visibility = Visibility.Collapsed;
            });
        }

        private void GameManagerViewModelOnPlayerLevelInsufficient(object sender, EventArgs e)
        {
            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Light, false);
            dialog.Messages.Add(new MessageEntry("This is a Gym, a place where you'll test your skills at Pokémon battles.", 60));
            dialog.Messages.Add(new MessageEntry("It looks like you don't have much experience as a Pokémon Trainer yet.",60));
            dialog.Messages.Add(new MessageEntry("Come back when you've reached level 5!", 30));

            dialog.Closed += Dialog_Closed;
            dialog.Show();
        }

        private void Dialog_Closed(object sender, EventArgs e)
        {
            ViewModel.AbandonGym.Execute();
        }

        private ChooseTeamDialog teamChooseDialog;

        private void GameManagerViewModelOnPlayerTeamUnset(object sender, EventArgs e)
        {
            GymUI.Visibility = Visibility.Collapsed;

            teamChooseDialog = new ChooseTeamDialog();
            teamChooseDialog.Messages.Add(new MessageEntry("Wow! Looks like you've caught a bunch of Pokémon and gained a lot of experience as a Pokémon Trainer, huh? Great work!", 90));
            teamChooseDialog.Messages.Add(new MessageEntry("It looks like you're about to start participating in Pokémon battles!", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("I have three excellent assistants.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("They each direct a team, and each has a slightly different approach to researching Pokémon.", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("Part of their research is conducting Pokémon battles at Gyms.", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("They're apparently excited to have you joining as a team member.", 60));

            teamChooseDialog.Closed += Dialog1_Closed;
            teamChooseDialog.Show();
        }

        private void Dialog1_Closed(object sender, EventArgs e)
        {
            // Yellow
            teamChooseDialog = new ChooseTeamDialog();
            teamChooseDialog.SetTeamLeader(Character.Yellow);
            teamChooseDialog.Messages.Add(new MessageEntry("Hey! The name's Spark-the leader of Team Instinct.", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("Pokémon are creatures with excellent intuition.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("I bet the secret to their intuition is related to how they're hatched.", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("Come on and join my team.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("You never lose when you trust your instincts!", 30));

            teamChooseDialog.Closed += Dialog2_Closed;
            teamChooseDialog.Show();
        }

        private void Dialog2_Closed(object sender, EventArgs e)
        {
            // Blue
            teamChooseDialog = new ChooseTeamDialog();
            teamChooseDialog.SetTeamLeader(Character.Blue);
            teamChooseDialog.Messages.Add(new MessageEntry("I am Blanche, leader of Team Mystic.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("The wisdom of Pokémon is immeasurably deep.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("I'm researching why it is that they evolve.", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("My team?", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("With our calm analysis of every situation, we can't lose!", 60));

            teamChooseDialog.Closed += Dialog3_Closed;
            teamChooseDialog.Show();
        }

        private void Dialog3_Closed(object sender, EventArgs e)
        {
            // Red
            teamChooseDialog = new ChooseTeamDialog();
            teamChooseDialog.SetTeamLeader(Character.Red);
            teamChooseDialog.Messages.Add(new MessageEntry("I'm Candela-Team Valor's leader!", 30));
            teamChooseDialog.Messages.Add(new MessageEntry("Pokémon are stronger than humans, and they're warmhearted, too!", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("I'm researching ways to enhance Pokémons natural power in the pursuit of true strength.", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("There's no doubt that the Pokémon our team have trained are the strongest in battle!", 60));
            teamChooseDialog.Messages.Add(new MessageEntry("Are you ready?", 30));

            teamChooseDialog.Closed += Dialog4_Closed;
            teamChooseDialog.Show();
        }


        private void Dialog4_Closed(object sender, EventArgs e)
        {
            //// Select a team to join. Team Instinct, Team Mystic, Team Valor
            teamChooseDialog = new ChooseTeamDialog();
            teamChooseDialog.PrepareForTeamChoice();
            teamChooseDialog.TeamChosen += Dialog_TeamChosen;
            teamChooseDialog.Show();
        }

        private void Dialog_TeamChosen(object sender, TeamColor teamColor)
        {
            //// After tapping a color, the logo appears on the backgound and the others are dimmed. A confirmation button appears.
            ViewModel.ChosenTeam = teamColor;
            teamChooseDialog.AskForConfirmation(teamColor);
            teamChooseDialog.Confirmed += Dialog_Confirmed;
        }

        private void Dialog_Confirmed(object sender, EventArgs e)
        {
            // After clicking the confirmation button, the SetPlayerTeam request is sent
            ViewModel.SetPlayerTeam.Execute();
            teamChooseDialog.OkAndClose += Dialog_OkAndClose;
        }

        private void Dialog_OkAndClose(object sender, EventArgs e)
        {
            GymUI.Visibility = Visibility.Visible;
            teamChooseDialog.Hide();
        }

        private void GameManagerViewModelOnPlayerTeamSet(object sender, TeamColor teamColor)
        {
            // Then, a message is displayed, showing 'Welcom to Team <Color>!
            teamChooseDialog.SetTeamChoiceComplete(teamColor);
            teamChooseDialog.Closed += TeamChooseDialog_Closed;
            teamChooseDialog.Show();
        }

        private void TeamChooseDialog_Closed(object sender, EventArgs e)
        {
        }

        #endregion
    }
}