using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Booze
{
    public partial class Detalhe : PhoneApplicationPage
    {
        private Bar bar;
        private bool isFavorite;
        private string[] telefone;
        private GeoCoordinate coordenadas;
        private List<Bar> listaFiltro;
        private ApplicationBarIconButton ibFavorito;

        public Detalhe()
        {
            InitializeComponent();

            BuildApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            int idx = Convert.ToInt16(Uri.UnescapeDataString(NavigationContext.QueryString["idx"]));

            bar = App.bares.FirstOrDefault(bar => bar.idx.Equals(idx));

            LoadData();

            AnalisaFavorito();
        }

        private void LoadData()
        {
            if (listaFiltro == null)
            {
                listaFiltro = new List<Bar>();

                if (bar != null)
                {
                    listaFiltro.Add(bar);

                    title.Text = bar.nome;

                    if (bar.imagem != string.Empty)
                    {
                        Uri uri = new Uri(bar.imagem, UriKind.Relative);
                        ImageSource imgSource = new BitmapImage(uri);
                        img.Source = imgSource;
                    }
                    else
                    {
                        img.Visibility = Visibility.Collapsed;
                    }

                    if (bar.horarios.Equals(string.Empty))
                    {
                        bar.horarios = AppResources.Horarios_NA;
                    }

                    telefone = bar.telefone;
                    coordenadas = bar.coordenadas;
                }

                listaItens.ItemsSource = listaFiltro;
            }
        }

        private void AnalisaFavorito()
        {
            if (!ApplicationBar.Buttons.Contains(ibFavorito))
            {
                if ((Application.Current as App).ExistemFavoritos())
                {
                    if ((Application.Current as App).favoritos.Contains(title.Text))
                    {
                        isFavorite = true;
                    }
                    else
                    {
                        isFavorite = false;
                    }
                }

                IconButton_Favoritos();

                ApplicationBar.Buttons.Insert(2, ibFavorito);
            }
        }

        private void IconButton_Favoritos()
        {
            if (ibFavorito == null)
            {
                ibFavorito = new ApplicationBarIconButton();
                ibFavorito.Click += favs_Click;
            }

            if (isFavorite)
            {
                ibFavorito.Text = AppResources.IconButton_Desfavoritar;
                ibFavorito.IconUri = new Uri("/images/icons/favs.removefrom.png", UriKind.Relative);
            }
            else
            {
                ibFavorito.Text = AppResources.IconButton_Favoritar;
                ibFavorito.IconUri = new Uri("/images/icons/favs.addto.png", UriKind.Relative);
            }
        }

        private void favs_Click(object sender, EventArgs e)
        {
            if ((Application.Current as App).ExistemFavoritos())
            {
                if (isFavorite)
                {
                    (Application.Current as App).favoritos.Remove(title.Text);

                    isFavorite = false;

                    MessageBox.Show(AppResources.Detalhes_Favoritos_Rem, title.Text, MessageBoxButton.OK);
                }
                else
                {
                    (Application.Current as App).favoritos.Add(title.Text);

                    isFavorite = true;

                    MessageBox.Show(AppResources.Detalhes_Favoritos_Add, title.Text, MessageBoxButton.OK);
                }
            }
            else
            {
                (Application.Current as App).favoritos = new ObservableCollection<string>();
                (Application.Current as App).favoritos.Add(title.Text);

                (Application.Current as App).sectionF = new ObservableCollection<Bar>();

                isFavorite = true;

                MessageBox.Show(AppResources.Detalhes_Favoritos_Add, title.Text, MessageBoxButton.OK);
            }

            FinalizaFavorito();

            IconButton_Favoritos();
        }

        private void FinalizaFavorito()
        {
            (Application.Current as App).SaveISS();

            if (!((Application.Current as App).favoritos.Count > 0))
            {
                if (NavigationContext.QueryString.ContainsKey("fromFavoritos"))
                {
                    NavigationService.RemoveBackEntry();
                }
            }
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            // Ícones

            ApplicationBarIconButton ligar = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/feature.phone.png", UriKind.Relative),
                Text = AppResources.IconButton_Ligar
            };

            ApplicationBarIconButton trajeto = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/map.direction.png", UriKind.Relative),
                Text = AppResources.IconButton_Trajeto
            };

            ligar.Click += ApplicationBarIconButton_Click;
            trajeto.Click += ApplicationBarIconButton_Click;

            ApplicationBar.Buttons.Add(ligar);
            ApplicationBar.Buttons.Add(trajeto);

            // Menus

            ApplicationBarMenuItem relatar = new ApplicationBarMenuItem(AppResources.MenuItem_Relatar);
            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem(AppResources.MenuItem_Ajustes);
            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem(AppResources.MenuItem_Sobre);

            relatar.Click += ApplicationBarMenuItem_Click;
            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;

            ApplicationBar.MenuItems.Add(relatar);
            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            string iconButtonText = (sender as ApplicationBarIconButton).Text;

            if (iconButtonText.Equals(AppResources.IconButton_Ligar))
            {
                if (telefone.Length == 1)
                {
                    if (telefone[0] == string.Empty)
                    {
                        MessageBox.Show(AppResources.Detalhes_Ligar_NA, "N/A", MessageBoxButton.OK);
                    }
                    else
                    {
                        Ligar(telefone[0]);
                    }
                }
                else
                {
                    string[] telefone_lpk = new string[telefone.Length + 1];
                    telefone_lpk[0] = string.Empty;

                    for (int i = 1; i < telefone_lpk.Length; i++)
                    {
                        telefone_lpk[i] = telefone[i - 1];
                    }

                    lpkTelefone.ItemsSource = telefone_lpk;
                    lpkTelefone.Open();
                }
            }
            else if (iconButtonText.Equals(AppResources.IconButton_Trajeto))
            {
                if (App.LocationConsent())
                {
                    Trajeto();
                }
            }
        }

        private void Ligar(string numtel)
        {
            PhoneCallTask ligar = new PhoneCallTask()
            {
                PhoneNumber = numtel
            };

            ligar.Show();
        }

        private void lpkTelefone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lpkTelefone.SelectedIndex != 0)
            {
                string numtel = lpkTelefone.SelectedItem as string;

                Ligar(numtel);

                lpkTelefone.SelectedIndex = 0;
            }
        }

        private async void Trajeto()
        {
            if (await Task.Run(() => NetworkInterface.GetIsNetworkAvailable()))
            {
                string encodedValue = Uri.EscapeDataString(title.Text);

                NavigationService.Navigate(new Uri("/Trajeto.xaml?nome=" + encodedValue +
                    "&latitude=" + coordenadas.Latitude.ToString(CultureInfo.InvariantCulture) +
                    "&longitude=" + coordenadas.Longitude.ToString(CultureInfo.InvariantCulture), UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.Trajeto_SemConexao_Msg, 
                    AppResources.Trajeto_SemConexao_Title, 
                    MessageBoxButton.OK);
            }
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            string menuItemText = (sender as ApplicationBarMenuItem).Text;
            string destination = null;

            if (menuItemText.Equals(AppResources.MenuItem_Relatar))
            {
                EmailComposeTask report = new EmailComposeTask()
                {
                    To = "boozewp@live.com",
                    Subject = AppResources.RelatarErro + " (#" + bar.idx.ToString() + ")",
                    Body = "\n\n\n" + AppResources.Local + bar.nome + "\n" + "v. app: " + App.GetAppVersion()
                };

                report.Show();
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