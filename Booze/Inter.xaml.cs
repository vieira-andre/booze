using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Navigation;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Booze
{
    public partial class Inter : PhoneApplicationPage
    {
        private List<Bar> listaFiltro;

        public Inter()
        {
            InitializeComponent();

            BuildApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string encodedValue = NavigationContext.QueryString["param"];
            title.Text = Uri.UnescapeDataString(encodedValue);

            LoadData();
        }

        private void LoadData()
        {
            if (listaFiltro == null)
            {
                listaFiltro = new List<Bar>();

                foreach (Bar bar in App.bares)
                {
                    if (bar.bairro.Equals(title.Text))
                    {
                        listaFiltro.Add(bar);
                    }
                }

                listaItens.ItemTemplate = llsT_Bairro;

                listaItens.ItemsSource = listaFiltro.OrderBy(o => o.nome).ToList();
            }
        }

        private void listaItens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listaItens.SelectedItem != null)
            {
                string encodedValue = Uri.EscapeDataString((listaItens.SelectedItem as Bar).idx.ToString());
                NavigationService.Navigate(new Uri("/Detalhe.xaml?idx=" + encodedValue, UriKind.Relative));

                listaItens.SelectedItem = null;
            }
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar()
            {
                Mode = ApplicationBarMode.Minimized
            };

            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Ajustes
            };

            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Sobre
            };

            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;

            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            string menuItemText = (sender as ApplicationBarMenuItem).Text;
            string destination = null;

            if (menuItemText.Equals(AppResources.MenuItem_Ajustes))
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