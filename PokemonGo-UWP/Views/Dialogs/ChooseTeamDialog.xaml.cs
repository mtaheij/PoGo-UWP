using Microsoft.Graphics.Canvas.Effects;
using POGOProtos.Enums;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Controls;
using Template10.Mvvm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Effects;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class ChooseTeamDialog : UserControl
    {
        private bool IgnoreGridTapped;

        public ChooseTeamDialog()
        {
            this.InitializeComponent();

            this.DataContext = this;

            TeamLogo.Visibility = 
                PanelTeamChoice.Visibility = 
                ConfirmationButton.Visibility = Visibility.Collapsed;
        }

        public MessageEntry CurrentMessage { get; set; }

        public ObservableCollection<MessageEntry> Messages { get; set; } = new ObservableCollection<MessageEntry>();

        public void SetTeamLeader(Character character)
        {
            Professor.Visibility = Visibility.Collapsed;

            switch (character)
            {
                case Views.Character.Yellow:
                    RenderImage(TeamLeader, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(480, 640), new Vector3(-60,-60,0), Colors.Yellow);
                    break;
                case Views.Character.Blue:
                    RenderImage(YellowLeader, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-60, -60, 0), Colors.Yellow);
                    RenderImage(TeamLeader, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(480, 640), new Vector3(-60, -60, 0), Colors.Blue);
                    break;
                case Views.Character.Red:
                    RenderImage(YellowLeader, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-60, -60, 0), Colors.Yellow);
                    RenderImage(BlueLeader, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(240, 320), new Vector3(-60, -60, 0), Colors.Blue);
                    RenderImage(TeamLeader, new Uri("ms-appx:///Assets/Teams/team_leader_red.png"), new Vector2(480, 640), new Vector3(-60, -60, 0), Colors.Red);
                    break;
            }
        }

        public void PrepareForTeamChoice()
        {
            Professor.Visibility = Visibility.Collapsed;
            PanelLeaders.Visibility = Visibility.Collapsed;

            PanelTeamChoice.Visibility = Visibility.Visible;
            SelectTeam.Visibility = YellowText.Visibility = BlueText.Visibility = RedText.Visibility = Visibility.Visible;

            RenderImage(YellowBack, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Yellow);
            RenderImage(BlueBack, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Blue);
            RenderImage(RedBack, new Uri("ms-appx:///Assets/Teams/team_leader_red.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Red);

            TeamYellowButton.IsEnabled =
                TeamBlueButton.IsEnabled =
                TeamRedButton.IsEnabled = true;

            DialogRect.Visibility =
                DialogText.Visibility = Visibility.Collapsed;

            IgnoreGridTapped = true;
        }

        public void AskForConfirmation(TeamColor teamColor)
        {
            switch (teamColor)
            {
                case TeamColor.Yellow:
                    TeamLogo.Source = new BitmapImage(new Uri("ms-appx:///Assets/Teams/instinct.png"));
                    TeamLogo.HorizontalAlignment = HorizontalAlignment.Left;
                    RenderImage(YellowBack, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Yellow);
                    RenderImage(BlueBack, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.LightBlue);
                    RenderImage(RedBack, new Uri("ms-appx:///Assets/Teams/team_leader_red.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Salmon);
                    break;
                case TeamColor.Blue:
                    TeamLogo.HorizontalAlignment = HorizontalAlignment.Center;
                    TeamLogo.Source = new BitmapImage(new Uri("ms-appx:///Assets/Teams/mystic.png"));
                    RenderImage(YellowBack, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.LightYellow);
                    RenderImage(BlueBack, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Blue);
                    RenderImage(RedBack, new Uri("ms-appx:///Assets/Teams/team_leader_red.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Salmon);
                    break;
                case TeamColor.Red:
                    TeamLogo.Source = new BitmapImage(new Uri("ms-appx:///Assets/Teams/valor.png"));
                    TeamLogo.HorizontalAlignment = HorizontalAlignment.Right;
                    RenderImage(YellowBack, new Uri("ms-appx:///Assets/Teams/team_leader_yellow.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.LightYellow);
                    RenderImage(BlueBack, new Uri("ms-appx:///Assets/Teams/team_leader_blue.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.LightBlue);
                    RenderImage(RedBack, new Uri("ms-appx:///Assets/Teams/team_leader_red.png"), new Vector2(240, 320), new Vector3(-100, -60, 0), Colors.Red);
                    break;
            }

            TeamLogo.Visibility = Visibility.Visible;
            ConfirmationButton.Visibility = Visibility.Visible;
        }

        public void SetTeamChoiceComplete(TeamColor teamColor)
        {
            PanelLeaders.Visibility =
            ConfirmationButton.Visibility = 
            DialogRect.Visibility = Visibility.Collapsed;

            TeamYellowButton.IsEnabled =
                TeamBlueButton.IsEnabled =
                TeamRedButton.IsEnabled = false;

            WelcomeText.Text = $"Welcome to Team {teamColor}!";
            WelcomeGrid.Visibility =
                PanelTeamChoice.Visibility =
                Visibility.Visible;
        }

        /// <summary>
        /// Displays the dialog modally
        /// </summary>
        public void Show()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                var modal = Window.Current.Content as ModalDialog;
                if (modal == null)
                {
                    return;
                }

                _formerModalBrush = modal.ModalBackground;
                modal.ModalBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                modal.ModalContent = this;
                modal.IsModal = true;

                CurrentMessage = Messages.FirstOrDefault();

                // animate
                Storyboard sb = this.Resources["ShowDialogStoryboard"] as Storyboard;
                sb.Begin();

                AudioUtils.PlaySound(AudioUtils.MESSAGE);
            });
        }

        private Brush _formerModalBrush = null;

        public void Hide()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                var modal = Window.Current.Content as ModalDialog;
                if (modal == null)
                {
                    return;
                }

                // animate
                Storyboard sb = this.Resources["HideDialogStoryboard"] as Storyboard;
                sb.Begin();
                sb.Completed += Cleanup;
            });
        }

        private void Cleanup(object sender, object e)
        {
            var modal = Window.Current.Content as ModalDialog;
            if (modal == null)
            {
                return;
            }

            modal.ModalBackground = _formerModalBrush;
            modal.ModalContent = null;
            modal.IsModal = false;

            Closed?.Invoke(this, null);
        }

        public event EventHandler Closed;

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IgnoreGridTapped) return;

            Messages.RemoveAt(0);
            if (Messages.Count > 0)
            {
                CurrentMessage = Messages.FirstOrDefault();

                DataContext = null;
                DataContext = this;

                AudioUtils.PlaySound(AudioUtils.MESSAGE);
            }
            else
            {
                Hide();
            }
        }

        public event EventHandler<TeamColor> TeamChosen;

        private DelegateCommand _teamYellowCommand;

        /// <summary>
        ///     Team Yellow Is Chosen
        /// </summary>
        public DelegateCommand TeamYellowCommand => _teamYellowCommand ?? (
            _teamYellowCommand =
                new DelegateCommand(
                    () => { TeamChosen?.Invoke(this, TeamColor.Yellow); },
                    () => true)
            );

        private DelegateCommand _teamBlueCommand;

        /// <summary>
        ///     Team Blue Is Chosen
        /// </summary>
        public DelegateCommand TeamBlueCommand => _teamBlueCommand ?? (
            _teamBlueCommand =
                new DelegateCommand(
                    () => { TeamChosen?.Invoke(this, TeamColor.Blue); },
                    () => true)
            );

        private DelegateCommand _teamRedCommand;

        /// <summary>
        ///     Team Red Is Chosen
        /// </summary>
        public DelegateCommand TeamRedCommand => _teamRedCommand ?? (
            _teamRedCommand =
                new DelegateCommand(
                    () => { TeamChosen?.Invoke(this, TeamColor.Red); },
                    () => true)
            );

        public event EventHandler Confirmed;

        private DelegateCommand _confirmationButtonCommand;

        /// <summary>
        ///     The Confirmation button is pressed after choosing a team
        /// </summary>
        public DelegateCommand ConfirmationButtonCommand => _confirmationButtonCommand ?? (
            _confirmationButtonCommand =
                new DelegateCommand(
                    () => { Confirmed?.Invoke(this, null); },
                    () => true)
            );

        public event EventHandler OkAndClose;

        private DelegateCommand _okButtonCommand;

        /// <summary>
        ///     The Confirmation button is pressed after choosing a team
        /// </summary>
        public DelegateCommand OkButtonCommand => _okButtonCommand ?? (
            _okButtonCommand =
                new DelegateCommand(
                    () => { OkAndClose?.Invoke(this, null); },
                    () => true)
            );

        private void RenderImage(UIElement uiElement, Uri imageUri, Vector2 size, Vector3 offset, Color imageColor, float Opacity = 1)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(uiElement).Compositor;
            var visual = compositor.CreateSpriteVisual();

            visual.Size = size;
            visual.Offset = offset;

            var _imageLoader = new ImageLoader();
            _imageLoader.Initialize(compositor);

            var surface = _imageLoader.LoadImageFromUri(imageUri);
            var brush = compositor.CreateSurfaceBrush(surface);

            IGraphicsEffect graphicsEffect = new CompositeEffect
            {
                Mode = Microsoft.Graphics.Canvas.CanvasComposite.DestinationIn,
                Sources =
            {
                new ColorSourceEffect
                {
                    Name = "colorSource",
                    Color = Color.FromArgb(255, 255, 255, 255)
                },
                new CompositionEffectSourceParameter("mask")
            }
            };

            var _effectFactory = compositor.CreateEffectFactory(graphicsEffect, new string[] { "colorSource.Color" });
            var effectBrush = _effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("mask", brush);

            visual.Brush = effectBrush;

            effectBrush.Properties.InsertColor("colorSource.Color", imageColor);

            ElementCompositionPreview.SetElementChildVisual(uiElement, visual);
            uiElement.Opacity = Opacity;
        }
    }

    public enum Character
    {
        Yellow,
        Blue,
        Red
    }
}
