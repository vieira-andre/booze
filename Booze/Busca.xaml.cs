using Booze.Classes;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;

namespace Booze
{
    public partial class Busca : PhoneApplicationPage
    {
        private bool isUpdating;
        private string lastRB;
        private List<Agrupamento<Bar>> listaGrupos;
        private List<Agrupamento<string>> gpBairros;
        private List<Bar> listaBusca;
        private List<string> palavras;
        private Geoposition position;
        private ObservableCollection<DependencyObject> children;
        private MapItemsControl mapItemsControl;
        private ObservableCollection<Push> pushpins;
        private UserLocationMarker marker;
        private ApplicationBarMenuItem ibUpdate, ibView;

        public Busca()
        {
            InitializeComponent();

            LoadData();

            BuildApplicationBar();

            reviewReminder();
        }

        private void myMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = AuthAccess.Keys["AppId"];
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = AuthAccess.Keys["AuthToken"];
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (pivotControl.SelectedItem != pivotBusca)
            {
                pivotControl.SelectedItem = pivotBusca;
                listaBairros.ScrollTo(gpBairros.First());

                e.Cancel = true;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            listaItens.SelectedItem = null;
        }

        #region Pivot "todos"

        private void LoadData()
        {
            listaGrupos = Agrupamento<Bar>.CriaGrupos(App.bares,
                Thread.CurrentThread.CurrentUICulture,
                (Bar s) => { return s.nome; }, true);

            listaItens.ItemsSource = listaGrupos;

            LoadBairros();
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateView();
        }

        private void listaItens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listaItens.SelectedItem != null)
            {
                string encodedValue = Uri.EscapeDataString((listaItens.SelectedItem as Bar).idx.ToString());
                NavigationService.Navigate(new Uri("/Detalhe.xaml?idx=" + encodedValue, UriKind.Relative));
            }
        }

        private void search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (search.Text == string.Empty)
            {
                listaItens.IsGroupingEnabled = true;
                listaItens.ItemsSource = listaGrupos;

                gridLimpar.Visibility = Visibility.Collapsed;
                gridLimpar.Background = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                listaBusca = new List<Bar>(App.bares.Where(w => AnalisaString(w.nome)).OrderBy(o => o.nome).ToList());

                if (!(listaItens.IsGroupingEnabled == false && listaItens.ItemsSource == listaBusca))
                {
                    listaItens.IsGroupingEnabled = false;
                    listaItens.ItemsSource = listaBusca;
                }

                gridLimpar.Visibility = Visibility.Visible;
            }
        }

        private bool AnalisaString(string entrada)
        {
            char[] nome = entrada.ToCharArray();

            bool areEqual = false;

            int x = 0;
            palavras = new List<string>();

            for (int i = 0; i < nome.Length; i++)
            {
                if (char.IsWhiteSpace(nome[i]))
                {
                    palavras.Add(new string(nome, x, i - x));
                    x = i + 1;
                }

                if (i + 1 == nome.Length)
                {
                    if (palavras.Count == 0) palavras.Add(new string(nome));
                    else palavras.Add(new string(nome, x, (i + 1) - x));
                }
            }

            foreach (string palavra in palavras)
            {
                if (areEqual != true)
                {
                    if (palavra.StartsWith(search.Text, CultureInfo.InvariantCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)) /*word.StartsWith(search.Text.ToLower())*/
                    {
                        areEqual = true;
                    }
                    else if (ProximasPalavras(palavra).StartsWith(search.Text, CultureInfo.InvariantCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)) /*NextWords(word).StartsWith(search.Text.ToLower())*/
                    {
                        areEqual = true;
                    }
                }
            }

            return areEqual ? true : false;
        }

        private string ProximasPalavras(string entrada)
        {
            int i = palavras.FindIndex(f => f == entrada);

            string saida = entrada;

            for (; i + 1 < palavras.Count; i++)
            {
                saida += string.Concat(" ", palavras[i + 1]);
            }

            return saida;
        }

        private void Item_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string type = sender.GetType().ToString();

            switch (type)
            {
                case "Microsoft.Phone.Maps.Toolkit.Pushpin":

                    GeoCoordinate geoc = (sender as Pushpin).GeoCoordinate;
                    int idx = App.bares.First(bar => bar.coordenadas.Equals(geoc)).idx;

                    string encodedValue = Uri.EscapeDataString(idx.ToString());
                    NavigationService.Navigate(new Uri("/Detalhe.xaml?idx=" + encodedValue, UriKind.Relative));

                    break;

                case "System.Windows.Controls.Grid":

                    if ((sender as Grid).Name.Equals(btCenter.Name))
                    {
                        myMap.SetView(position.Coordinate.ToGeoCoordinate(), myMap.ZoomLevel);
                    }
                    else if ((sender as Grid).Name.Equals(gridLimpar.Name))
                    {
                        gridLimpar.Background = new SolidColorBrush(Color.FromArgb(255, 255, 238, 33));
                        search.ClearValue(TextBox.TextProperty);
                    }

                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Pivot "bairros"

        private void LoadBairros()
        {
            List<string> bairros = new List<string>();

            foreach (Bar bar in App.bares)
            {
                if (!bairros.Exists(b => b.Equals(bar.bairro)))
                {
                    bairros.Add(bar.bairro);
                }
            }

            gpBairros = Agrupamento<string>.CriaGrupos(bairros,
                Thread.CurrentThread.CurrentUICulture,
                (string s) => { return s; }, true);

            listaBairros.ItemsSource = gpBairros;
        }

        private void listaBairros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listaBairros.SelectedItem != null)
            {
                string encodedValue = Uri.EscapeDataString(listaBairros.SelectedItem as string);
                NavigationService.Navigate(new Uri("/Inter.xaml?param=" + encodedValue + "&fromBairros", UriKind.Relative));

                listaBairros.SelectedItem = null;
            }
        }

        #endregion

        #region Pivot "+próximos"

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.LocationConsent())
            {
                if (lastRB != (sender as RadioButton).Name)
                {
                    Acquire();

                    lastRB = (sender as RadioButton).Name;
                }

                MapZoomLevel();
            }
            else
            {
                (sender as RadioButton).IsChecked = false;
            }
        }

        private void Acquire()
        {
            if (position == null)
            {
                if (myMap.Visibility != Visibility.Collapsed) ClearMap();

                RB_Enabler(false, true);

                Start_GetLocation();
            }
            else
            {
                ClearMap();

                SearchRange();
            }
        }

        private async void Start_GetLocation()
        {
            ApplicationBar.IsMenuEnabled = false;
            pivotControl.Margin = new Thickness(-15, 17, 0, 0);

            if (!isUpdating)
            {
                spinnerSP.Visibility = Visibility.Visible;
                SpinningAnimation.Begin();
            }

            position = await App.GetLocation(Dispatcher, isUpdating);

            if (position != null)
            {
                SystemTray.ProgressIndicator.IsVisible = false;

                SpinningAnimation.Stop();
                spinnerSP.Visibility = Visibility.Collapsed;

                SearchRange();
            }
            else
            {
                Dispatcher.BeginInvoke(() => { lastRB = null; });

                RB_Enabler(true, false);
            }

            ApplicationBar.IsMenuEnabled = true;
            pivotControl.Margin = new Thickness(-15, 0, 0, 0);
        }

        private void SearchRange()
        {
            string nome;
            double distancia;
            GeoCoordinate coordenadas;

            pushpins = new ObservableCollection<Push>();

            foreach (Bar bar in App.bares)
            {
                nome = bar.nome;
                coordenadas = bar.coordenadas;

                distancia = position.Coordinate.ToGeoCoordinate().GetDistanceTo(coordenadas);

                if (rb1.IsChecked == true)
                {
                    if ((distancia /= 1000) <= 1) pushpins.Add(new Push(nome, coordenadas));
                }
                else if (rb3.IsChecked == true)
                {
                    if ((distancia /= 1000) <= 3) pushpins.Add(new Push(nome, coordenadas));
                }
                else if (rb5.IsChecked == true)
                {
                    if ((distancia /= 1000) <= 5) pushpins.Add(new Push(nome, coordenadas));
                }
            }

            if (pushpins.Count == 0) MessageBox.Show(AppResources.Busca_NoPlace);

            ToForgeChildren();

            myMap.Center = position.Coordinate.ToGeoCoordinate();
            ToVisible(true, myMap, btCenter);

            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;

            UpdateView();
            RB_Enabler(true, true);
        }

        private void ToForgeChildren()
        {
            if (children == null)
            {
                children = MapExtensions.GetChildren(myMap);

                marker = children.FirstOrDefault(x => x.GetType() == typeof(UserLocationMarker)) as UserLocationMarker;
                mapItemsControl = children.FirstOrDefault(x => x.GetType() == typeof(MapItemsControl)) as MapItemsControl;
            }

            marker.GeoCoordinate = position.Coordinate.ToGeoCoordinate();

            if (pushpins.Count != 0) mapItemsControl.ItemsSource = pushpins;
        }

        private void MapZoomLevel()
        {
            if (rb1.IsChecked.Value) myMap.ZoomLevel = 15.4;
            else if (rb3.IsChecked.Value) myMap.ZoomLevel = 13.4;
            else if (rb5.IsChecked.Value) myMap.ZoomLevel = 12.7;

            if (position != null) myMap.Center = position.Coordinate.ToGeoCoordinate();
        }

        private void ClearMap()
        {
            if (mapItemsControl.ItemsSource != null)
            {
                pushpins.Clear();
                mapItemsControl.ClearValue(MapItemsControl.ItemsSourceProperty);
            }
        }

        private void RB_Enabler(bool enable, bool check)
        {
            if (enable)
            {
                rb1.IsEnabled = true;
                rb3.IsEnabled = true;
                rb5.IsEnabled = true;

                if (!check)
                {
                    rb1.IsChecked = false;
                    rb3.IsChecked = false;
                    rb5.IsChecked = false;
                }
            }
            else
            {
                if (rb1.IsChecked == true)
                {
                    rb3.IsEnabled = false;
                    rb5.IsEnabled = false;
                }
                else if (rb3.IsChecked == true)
                {
                    rb1.IsEnabled = false;
                    rb5.IsEnabled = false;
                }
                else if (rb5.IsChecked == true)
                {
                    rb1.IsEnabled = false;
                    rb3.IsEnabled = false;
                }
            }
        }

        private void ToVisible(bool isTrue, params UIElement[] obj)
        {
            for (int i = 0; i < obj.Length; i++)
            {
                if (isTrue) obj[i].Visibility = Visibility.Visible;
                else obj[i].Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateView()
        {
            if (!(ApplicationBar.MenuItems.Contains(ibUpdate) && ApplicationBar.MenuItems.Contains(ibView)))
            {
                if (pivotControl.SelectedItem == pivotNearby)
                {
                    if (myMap.Visibility == Visibility.Visible)
                    {
                        ibUpdate = new ApplicationBarMenuItem(AppResources.MenuItem_Atualizar);
                        ibUpdate.Click += updateView_Click;

                        ibView = new ApplicationBarMenuItem(AppResources.MenuItem_AlternarVista);
                        ibView.Click += updateView_Click;

                        ApplicationBar.MenuItems.Insert(0, ibUpdate);
                        ApplicationBar.MenuItems.Insert(1, ibView);
                    }
                }
            }
            else
            {
                if (pivotControl.SelectedItem != pivotNearby)
                {
                    ApplicationBar.MenuItems.Remove(ibUpdate);
                    ApplicationBar.MenuItems.Remove(ibView);
                }
            }
        }

        private void updateView_Click(object sender, EventArgs e)
        {
            string menuItemText = (sender as ApplicationBarMenuItem).Text;

            if (menuItemText.Equals(AppResources.MenuItem_Atualizar))
            {
                position = null;
                isUpdating = true;

                Acquire();
            }
            else if (menuItemText.Equals(AppResources.MenuItem_AlternarVista))
            {
                if (myMap.CartographicMode == MapCartographicMode.Road)
                {
                    myMap.CartographicMode = MapCartographicMode.Hybrid;
                }
                else if (myMap.CartographicMode == MapCartographicMode.Hybrid)
                {
                    myMap.CartographicMode = MapCartographicMode.Road;
                }
            }
        }

        #endregion

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar() { Mode = ApplicationBarMode.Minimized };

            ApplicationBarIconButton favoritos = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/favs.png", UriKind.Relative),
                Text = AppResources.IconButton_Favoritos
            };

            ApplicationBarIconButton submeter = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/upload.png", UriKind.Relative),
                Text = AppResources.IconButton_Submeter
            };

            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem() { Text = AppResources.MenuItem_Ajustes };

            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem() { Text = AppResources.MenuItem_Sobre };

            favoritos.Click += ApplicationBarIconButton_Click;
            submeter.Click += ApplicationBarIconButton_Click;
            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;

            ApplicationBar.Buttons.Add(favoritos);
            ApplicationBar.Buttons.Add(submeter);
            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);
        }

        public void reviewReminder()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;

            if (!settings.Contains("okReview"))
            {
                settings.Add("okReview", 1);
                settings.Add("cancelReview", 0);
            }
            else
            {
                int ok = Convert.ToInt16(settings["cancelReview"]);

                if (ok == 0)
                {
                    int cancel = Convert.ToInt16(settings["okReview"]);

                    if (cancel == 20) cancel = 10;

                    cancel++;

                    if (cancel == 3 || cancel % 10 == 0)
                    {
                        settings["okReview"] = cancel;

                        MessageBoxResult result = MessageBox.Show(AppResources.App_Review_Message, AppResources.App_Review_Title, MessageBoxButton.OKCancel);

                        if (result == MessageBoxResult.OK)
                        {
                            settings["cancelReview"] = 1;

                            MarketplaceReviewTask avaliar = new MarketplaceReviewTask();
                            avaliar.Show();
                        }
                    }
                    else settings["okReview"] = cancel;
                }
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            string iconButtonText = (sender as ApplicationBarIconButton).Text;

            if (iconButtonText.Equals(AppResources.IconButton_Favoritos))
            {
                if ((Application.Current as App).ExistemFavoritos())
                {
                    NavigationService.Navigate(new Uri("/Favoritos.xaml", UriKind.Relative));
                }
                else
                {
                    MessageBox.Show(AppResources.Favoritos_Null_Message, AppResources.Favoritos_Null_Caption, MessageBoxButton.OK);
                }
            }
            else if (iconButtonText.Equals(AppResources.IconButton_Submeter))
            {
                NavigationService.Navigate(new Uri("/Submit.xaml", UriKind.Relative));
            }
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