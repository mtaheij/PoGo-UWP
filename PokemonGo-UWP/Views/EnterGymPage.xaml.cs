using POGOProtos.Data.Battle;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.ViewModels;
using System;
using System.Threading.Tasks;
using Template10.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static PokemonGo_UWP.ViewModels.EnterGymPageViewModel;

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

            // Hide panels
            HideSelectAttackTeamGridStoryboard.Begin();
            HideBattleOutcomeStoryboard.Begin();

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
            ViewModel.GymsAreDisabled += GameManagerViewModelOnGymsAreDisabled;
            ViewModel.PlayerTeamUnset += GameManagerViewModelOnPlayerTeamUnset;

            ViewModel.PlayerTeamSet += GameManagerViewModelOnPlayerTeamSet;
            ViewModel.AskForPokemonSelection += GameManagerViewModelOnAskForPokemonSelection;
            ViewModel.PokemonDeployed += GameManagerViewModelOnPokemonDeployed;
            ViewModel.DeployPokemonError += GameManagerViewModelOnDeployPokemonError;
            ViewModel.PokemonSelectionCancelled += GameManagerViewModelOnPokemonSelectionCancelled;
            ViewModel.AskForAttackTeam += GameManagerViewModelOnAskForAttackTeam;
            ViewModel.AttackTeamSelectionClosed += GameManagerViewModelOnAttackTeamSelectionClosed;
            ViewModel.BattleError += GameManagerViewModelOnBattleError;
            ViewModel.ShowBattleArena += GameManagerViewModelOnShowBattleArena;
            ViewModel.BattleStarted += GameManagerViewModelOnBattleStarted;
            ViewModel.BattleRoundResultVictory += GameManagerViewModelOnBattleResultVictory;
            ViewModel.BattleEnded += GameManagerViewModelOnBattleEnded;
            ViewModel.ShowBattleOutcome += GameManagerViewModelOnShowBattleOutcome;
            ViewModel.CloseBattleOutcome += GameManagerViewModelOnCloseBattleOutcome;

            ViewModel.DefendingActionAttack += GameManagerViewModelOnDefendingActionAttack;
            ViewModel.DefendingActionSpecialAttack += GameManagerViewModelOnDefendingActionSpecialAttack;
            ViewModel.DefendingActionDodge += GameManagerViewModelOnDefendingActionDodge;

            ViewModel.AttackingActionAttack += GameManagerViewModelOnAttackingActionAttack;
            ViewModel.AttackingActionSpecialAttack += GameManagerViewModelOnAttackingActionSpecialAttack;
            ViewModel.AttackingActionDodge += GameManagerViewModelOnAttackingActionDodge;
        }

        private void UnsubscribeToEnterEvents()
        {
            ViewModel.GymLoaded -= GameManagerViewModelOnGymLoaded;

            ViewModel.EnterOutOfRange -= GameManagerViewModelOnEnterOutOfRange;
            ViewModel.EnterSuccess -= GameManagerViewModelOnEnterSuccess;

            ViewModel.PlayerLevelInsufficient -= GameManagerViewModelOnPlayerLevelInsufficient;
            ViewModel.GymsAreDisabled -= GameManagerViewModelOnGymsAreDisabled;
            ViewModel.PlayerTeamUnset -= GameManagerViewModelOnPlayerTeamUnset;

            ViewModel.PlayerTeamSet -= GameManagerViewModelOnPlayerTeamSet;
            ViewModel.AskForPokemonSelection -= GameManagerViewModelOnAskForPokemonSelection;
            ViewModel.PokemonDeployed -= GameManagerViewModelOnPokemonDeployed;
            ViewModel.DeployPokemonError -= GameManagerViewModelOnDeployPokemonError;
            ViewModel.PokemonSelectionCancelled -= GameManagerViewModelOnPokemonSelectionCancelled;
            ViewModel.AskForAttackTeam -= GameManagerViewModelOnAskForAttackTeam;
            ViewModel.AttackTeamSelectionClosed -= GameManagerViewModelOnAttackTeamSelectionClosed;
            ViewModel.BattleError -= GameManagerViewModelOnBattleError;
            ViewModel.ShowBattleArena -= GameManagerViewModelOnShowBattleArena;
            ViewModel.BattleStarted -= GameManagerViewModelOnBattleStarted;
            ViewModel.BattleRoundResultVictory -= GameManagerViewModelOnBattleResultVictory;
            ViewModel.BattleEnded -= GameManagerViewModelOnBattleEnded;
            ViewModel.ShowBattleOutcome -= GameManagerViewModelOnShowBattleOutcome;
            ViewModel.CloseBattleOutcome -= GameManagerViewModelOnCloseBattleOutcome;

            ViewModel.DefendingActionAttack -= GameManagerViewModelOnDefendingActionAttack;
            ViewModel.DefendingActionSpecialAttack -= GameManagerViewModelOnDefendingActionSpecialAttack;
            ViewModel.DefendingActionDodge -= GameManagerViewModelOnDefendingActionDodge;

            ViewModel.AttackingActionAttack -= GameManagerViewModelOnAttackingActionAttack;
            ViewModel.AttackingActionSpecialAttack -= GameManagerViewModelOnAttackingActionSpecialAttack;
            ViewModel.AttackingActionDodge -= GameManagerViewModelOnAttackingActionDodge;
        }

        private void GameManagerViewModelOnGymLoaded(object sender, EventArgs eventArgs)
        {
            GymMembersControl.DataContext = null;
            GymMembersControl.DataContext = ViewModel;

            GymMembersControl.GymDefenders = ViewModel.GymDefenders;
        }

        private void GameManagerViewModelOnEnterOutOfRange(object sender, EventArgs eventArgs)
        {
            ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorNotInRange");
            ErrorMessageText.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;
            ShowErrorMessageStoryboard.Completed += (ss, ee) => { HideErrorMessageStoryboard.Begin(); };
            ShowErrorMessageStoryboard.Begin();
        }

        private void GameManagerViewModelOnEnterSuccess(object sender, EventArgs eventArgs)
        {
        }

        private void GameManagerViewModelOnAskForPokemonSelection(object sender, SelectionTarget selectionTarget)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                switch (selectionTarget)
                {
                    case SelectionTarget.SelectForDeploy:
                        PokemonInventorySelector.SelectionMode = ListViewSelectionMode.Single;
                        break;
                    case SelectionTarget.SelectForTeamChange:
                        PokemonInventorySelector.SelectionMode = ListViewSelectionMode.Single;
                        break;
                }
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

        private void GameManagerViewModelOnDeployPokemonError(object sender, GymDeployResponse.Types.Result result)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                switch (result)
                {
                    case GymDeployResponse.Types.Result.ErrorAlreadyHasPokemonOnFort:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorAlreadyHasPokemonOnFort");
                        break;
                    case GymDeployResponse.Types.Result.ErrorFortDeployLockout:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorFortDeployLockout");
                        break;
                    case GymDeployResponse.Types.Result.ErrorFortIsFull:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorFortIsFull");
                        break;
                    case GymDeployResponse.Types.Result.ErrorNotInRange:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorNotInRange");
                        break;
                    case GymDeployResponse.Types.Result.ErrorOpposingTeamOwnsFort:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorOpposingTeamOwnsFort");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPlayerHasNoNickname:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPlayerHasNoNickname");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPokemonNotFullHp:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPokemonNotFullHp");
                        break;
                    case GymDeployResponse.Types.Result.ErrorInvalidPokemon:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorInvalidPokemon");
                        break;
                    case GymDeployResponse.Types.Result.ErrorLegendaryPokemon:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorLegendaryPokemon");
                        break;
                    case GymDeployResponse.Types.Result.ErrorNotAPokemon:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorNotAPokemon");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPlayerBelowMinimumLevel:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPlayerBelowMinimumLevel");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPlayerHasNoTeam:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPlayerHasNoTeam");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPoiInaccessible:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPoiInaccessible");
                        break;
                    case GymDeployResponse.Types.Result.ErrorPokemonIsBuddy:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorPokemonIsBuddy");
                        break;
                    case GymDeployResponse.Types.Result.ErrorRaidActive:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorRaidActive");
                        break;
                    case GymDeployResponse.Types.Result.ErrorTeamDeployLockout:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorTeamDeployLockout");
                        break;
                    case GymDeployResponse.Types.Result.ErrorTooManyDeployed:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorTooManyDeployed");
                        break;
                    case GymDeployResponse.Types.Result.ErrorTooManyOfSameKind:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("ErrorTooManyOfSameKind");
                        break;
                    case GymDeployResponse.Types.Result.NoResultSet:
                        ErrorMessageText.Text = Utils.Resources.CodeResources.GetString("NoResultSet");
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
                //PokemonInventorySelector.ClearSelectedPokemons();
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

        private void GameManagerViewModelOnGymsAreDisabled(object sender, EventArgs e)
        {
            ProfessorDialog dialog = new ProfessorDialog(BackGroundType.Light, false);
            dialog.Messages.Add(new MessageEntry("I regret to inform you that gyms are currently disabled.", 60));
            dialog.Messages.Add(new MessageEntry("The new battling system has not yet been implemented.", 60));
            dialog.Messages.Add(new MessageEntry("Come back when there is a new version of POGO-UWP!", 60));

            dialog.Closed += Dialog_Closed;
            dialog.Show();
        }

        private void GameManagerViewModelOnAskForAttackTeam(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ButtonBar.Visibility = Visibility.Collapsed;
                SelectAttackTeamGrid.Visibility = Visibility.Visible;
                ShowSelectAttackTeamGridStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnAttackTeamSelectionClosed(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ButtonBar.Visibility = Visibility.Visible;
                HideSelectAttackTeamGridStoryboard.Completed += (ss, ee) => { SelectAttackTeamGrid.Visibility = Visibility.Collapsed; };
                HideSelectAttackTeamGridStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnShowBattleArena(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                BattleUIGrid.Visibility = Visibility.Visible;
                ShowBattleUIGridStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnBattleStarted(object sender, int battleNr)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                BattleRoundTextBox.Text = $"BATTLE {battleNr}";
                ShowBattleRoundStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnBattleResultVictory(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                BattleRoundResultTextBox.Text = $"VICTORY!";
                ShowBattleRoundResultStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnDefendingActionAttack(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateDefendingStoryboardUp.Completed += (ee, ss) => { AnimateDefendingStoryboardDown.Begin(); };
                AnimateDefendingStoryboardUp.Begin();
            });
        }

        private void GameManagerViewModelOnDefendingActionSpecialAttack(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateDefendingStoryboardUp.Completed += (ee, ss) => { AnimateDefendingStoryboardDown.Begin(); };
                AnimateDefendingStoryboardUp.Begin();
            });
        }

        private void GameManagerViewModelOnDefendingActionDodge(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateDefendingStoryboardRight.Completed += (ee, ss) => { AnimateDefendingStoryboardLeft.Begin(); };
                AnimateDefendingStoryboardRight.Begin();
            });
        }

        private void GameManagerViewModelOnAttackingActionAttack(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateAttackingStoryboardUp.Completed += (ee, ss) => { AnimateAttackingStoryboardDown.Begin(); };
                AnimateAttackingStoryboardUp.Begin();
            });
        }

        private void GameManagerViewModelOnAttackingActionSpecialAttack(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateAttackingStoryboardUp.Completed += (ee, ss) => { AnimateAttackingStoryboardDown.Begin(); };
                AnimateAttackingStoryboardUp.Begin();
            });
        }

        private void GameManagerViewModelOnAttackingActionDodge(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                AnimateAttackingStoryboardRight.Completed += (ee, ss) => { AnimateAttackingStoryboardLeft.Begin(); };
                AnimateAttackingStoryboardRight.Begin();
            });
        }

        private void GameManagerViewModelOnBattleEnded(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                HideBattleUIGridStoryboard.Completed += (ss, ee) => { BattleUIGrid.Visibility = Visibility.Collapsed; };
                HideBattleUIGridStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnCloseBattleOutcome(object sender, EventArgs e)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                HideBattleOutcomeStoryboard.Completed += (ss, ee) => { BattleOutcomeGrid.Visibility = Visibility.Collapsed; };
                HideBattleOutcomeStoryboard.Begin();

                HideBattleOutcomeStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnShowBattleOutcome(object sender, BattleOutcomeResultEventArgs battleResult)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                BattleOutcome.Text = battleResult.BattleOutcome;
                if (battleResult.TotalGymPrestigeDelta > 0)
                    TotalGymPrestigeDelta.Text = "+" + battleResult.TotalGymPrestigeDelta.ToString();
                else
                    TotalGymPrestigeDelta.Text = battleResult.TotalGymPrestigeDelta.ToString();
                TotalPlayerXpEarned.Text = battleResult.TotalPlayerXpEarned.ToString();
                PokemonDefeated.Text = battleResult.PokemonDefeated.ToString();
                BattleOutcomeGrid.Visibility = Visibility.Visible;
                ShowBattleOutcomeStoryboard.Begin();
            });
        }

        private void GameManagerViewModelOnBattleError(object sender, string message)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ErrorMessageText.Text = Utils.Resources.CodeResources.GetString(message);
                ErrorMessageText.Visibility = ErrorMessageBorder.Visibility = Visibility.Visible;

                SelectAttackTeamGrid.Visibility = Visibility.Collapsed;

                ShowErrorMessageStoryboard.Completed += (ss, ee) => { HideErrorMessageStoryboard.Begin(); };
                ShowErrorMessageStoryboard.Begin();
            });
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