using PokemonGo_UWP.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Template10.Common;
using Template10.Controls;
using Template10.Mvvm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class ConfirmationDialog : UserControl
    {
        public ConfirmationDialog(string MessageText)
        {
            this.InitializeComponent();
            this.MessageText = MessageText;
            this.DataContext = this;
        }

        public string MessageText { get; set; }

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

        private DelegateCommand _okButtonCommand;

        /// <summary>
        ///     Team Yellow Is Chosen
        /// </summary>
        public DelegateCommand OkButtonCommand => _okButtonCommand ?? (
            _okButtonCommand =
                new DelegateCommand(() => 
                {
                    Hide();
                    Closed?.Invoke(this, null);
                }, () => true)
            );
    }
}
