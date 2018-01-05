using POGOProtos.Inventory;
using Template10.Mvvm;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PokemonGo_UWP.Views
{
    public sealed partial class EggDetailControl : UserControl
    {
        public EggDetailControl()
        {
            this.InitializeComponent();
        }

        private void ShowIncubatorSelection_Click(object sender, RoutedEventArgs e)
        {
            IncubatorSelectionOverlayControl incubatorControl = new IncubatorSelectionOverlayControl();
            incubatorControl.IncubatorSelected += (incubator) => { IncubateEggCommand?.Execute(incubator); };
            incubatorControl.Show();
        }

        #region Dependency Propertys

        public static readonly DependencyProperty IncubateEggCommandProperty =
            DependencyProperty.Register(nameof(IncubateEggCommand), typeof(DelegateCommand<EggIncubator>), typeof(EggDetailControl),
                new PropertyMetadata(null));


        public DelegateCommand<EggIncubator> IncubateEggCommand
        {
            get { return (DelegateCommand<EggIncubator>)GetValue(IncubateEggCommandProperty); }
            set { SetValue(IncubateEggCommandProperty, value); }
        }

        #endregion
    }
}
