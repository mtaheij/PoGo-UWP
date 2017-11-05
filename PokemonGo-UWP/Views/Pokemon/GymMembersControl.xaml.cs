using POGOProtos.Data;
using POGOProtos.Data.Gym;
using PokemonGo_UWP.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PokemonGo_UWP.Views
{
    public sealed partial class GymMembersControl : UserControl
    {
        public GymMembersControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        #region Properties

        public static readonly DependencyProperty CurrentGymProperty =
            DependencyProperty.Register(nameof(CurrentGym), typeof(FortDataWrapper), typeof(GymMembersControl),
                new PropertyMetadata(null));

        public FortDataWrapper CurrentGym
        {
            get { return (FortDataWrapper)GetValue(CurrentGymProperty); }
            set { SetValue(CurrentGymProperty, value); }
        }

        public static readonly DependencyProperty GymDefendersProperty =
            DependencyProperty.Register(nameof(GymDefenders), typeof(ObservableCollection<GymDefender>), typeof(GymMembersControl),
                new PropertyMetadata(null));

        public ObservableCollection<GymDefender> GymDefenders
        {
            get { return (ObservableCollection<GymDefender>)GetValue(GymDefendersProperty); }
            set { SetValue(GymDefendersProperty, value); }
        }

        public static readonly DependencyProperty SelectedGymMembershipProperty =
            DependencyProperty.Register(nameof(SelectedGymMembership), typeof(GymMembership), typeof(GymMembersControl), new PropertyMetadata(null));

        public GymMembership SelectedGymMembership
        {
            get { return (GymMembership)GetValue(SelectedGymMembershipProperty); }
            set { SetValue(SelectedGymMembershipProperty, value); }
        }

        public static readonly DependencyProperty UltimatePokemonProperty =
            DependencyProperty.Register(nameof(UltimatePokemon), typeof(PokemonData), typeof(GymMembersControl), new PropertyMetadata(null));

        public PokemonData UltimatePokemon
        {
            get { return (PokemonData)GetValue(UltimatePokemonProperty); }
            set { SetValue(UltimatePokemonProperty, value); }
        }

        public string PokemonName
        {
            get
            {
                GymMembership dataContext = this.DataContext as GymMembership;
                if (string.IsNullOrEmpty(dataContext.PokemonData.Nickname))
                {
                    return Utils.Resources.Pokemon.GetString(dataContext.PokemonData.PokemonId.ToString());
                }
                return dataContext.PokemonData.Nickname;
            }
        }
        #endregion

        private void GymMembershipFlip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GymMembership selectedMembership = GymMembershipFlip.SelectedItem as GymMembership;
            if (selectedMembership == null) return;

            PokemonData selectedPokemon = selectedMembership.PokemonData;
            if (selectedPokemon == null) return;

            IsUltimatePokemonVisibility = Visibility.Collapsed;
            if (selectedPokemon.Id == this.UltimatePokemon.Id)
            {
                IsUltimatePokemonVisibility = Visibility.Visible;
            }
        }

        public static readonly DependencyProperty IsUltimatePokemonVisibilityProperty =
            DependencyProperty.Register(nameof(IsUltimatePokemonVisibility), typeof(Visibility), typeof(GymMembersControl), new PropertyMetadata(null));

        public Visibility IsUltimatePokemonVisibility
        {
            get { return (Visibility)GetValue(IsUltimatePokemonVisibilityProperty); }
            set { SetValue(IsUltimatePokemonVisibilityProperty, value); }
        }
    }
}
