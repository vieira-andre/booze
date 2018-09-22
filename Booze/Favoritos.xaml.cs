using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Booze
{
    public partial class Favoritos : PhoneApplicationPage
    {
        public Favoritos()
        {
            InitializeComponent();

            listaFavoritos.ItemsSource = (Application.Current as App).sectionF;

            BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar()
            {
                Mode = ApplicationBarMode.Minimized
            };

            ApplicationBarMenuItem excluirLista = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_ExcluirLista
            };
            
            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Ajustes
            };

            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Sobre
            };

            excluirLista.Click += ApplicationBarMenuItem_Click;
            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;

            ApplicationBar.MenuItems.Add(excluirLista);
            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);
        }

        private void listaFavoritos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listaFavoritos.SelectedItem != null)
            {
                NavigationService.Navigate(new Uri("/Detalhe.xaml?idx=" + (listaFavoritos.SelectedItem as Bar).idx.ToString() +
                    "&fromFavoritos", UriKind.Relative));

                listaFavoritos.SelectedItem = null;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string bar = ((sender as MenuItem).DataContext as Bar).nome;

            (Application.Current as App).favoritos.Remove(bar);
            (Application.Current as App).SaveISS();

            if (!(Application.Current as App).ExistemFavoritos())
            {
                NavigationService.GoBack();
            }
        }

        //private void listaFavoritos_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    string bar = ((e.OriginalSource as TextBlock).DataContext as Bar).nome;

        //    NavigationService.Navigate(new Uri("/Detalhe.xaml?nome=" + bar + "&fromFavoritos", UriKind.Relative));
        //}

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            string menuItemText = (sender as ApplicationBarMenuItem).Text;
            string destination = null;

            if (menuItemText.Equals(AppResources.MenuItem_ExcluirLista))
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Favoritos_ExcluirLista_Message, 
                    AppResources.Favoritos_ExcluirLista_Caption, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    (Application.Current as App).favoritos.Clear();
                    (Application.Current as App).SaveISS();

                    NavigationService.GoBack();
                }
            }
            else if (menuItemText.Equals(AppResources.MenuItem_Ajustes))
            {
                destination = "Ajustes";
            }
            else if (menuItemText.Equals(AppResources.MenuItem_Sobre))
            {
                destination = "Sobre";
            }

            if (destination != null)
            {
                NavigationService.Navigate(new Uri("/" + destination + ".xaml", UriKind.Relative));
            }
        }
    }
}