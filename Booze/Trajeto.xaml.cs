using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using Windows.System;

namespace Booze
{
    public partial class Trajeto : PhoneApplicationPage
    {
        private double latitude, longitude;
        private string nome;
        private Geoposition position;
        private List<GeoCoordinate> geoCoordenadas;
        private ReverseGeocodeQuery reverseGeocodeQuery;
        private RouteQuery routeQuery;
        private Route route;
        private List<string> rotas;

        public Trajeto()
        {
            InitializeComponent();

            Start_GetLocation();

            BuildApplicationBar();
        }

        private void myMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "f505c1ba-2513-494d-9fd0-1a4a9ff3f041";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "C4phvPXeCCM6HWaNcv2SBg";
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if ((int)myMap.GetValue(Grid.RowSpanProperty) == 3)
            {                
                myMap.SetValue(Grid.RowSpanProperty, 1);

                btCenter.SetValue(Grid.RowSpanProperty, 1);
                btCenter.Margin = new Thickness(0, 0, 10, 10);

                ToVisible(true, Header, listaRotas);

                e.Cancel = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string encodedValue = NavigationContext.QueryString["nome"];

            nome = Uri.UnescapeDataString(encodedValue);
            latitude = double.Parse(NavigationContext.QueryString["latitude"], CultureInfo.InvariantCulture);
            longitude = double.Parse(NavigationContext.QueryString["longitude"], CultureInfo.InvariantCulture);
        }

        private async void Start_GetLocation()
        {
            SpinningAnimation.Begin();
            position = await App.GetLocation(this.Dispatcher, false);

            if (position != null)
            {
                if (await Task.Run(() => NetworkInterface.GetIsNetworkAvailable()))
                {
                    geoCoordenadas = new List<GeoCoordinate>();
                    geoCoordenadas.Add(position.Coordinate.ToGeoCoordinate());

                    SystemTray.ProgressIndicator.Text = AppResources.SysTray_Trajeto;

                    Start_ReverseGeocodeQuery();
                }
                else
                {
                    MessageBox.Show(AppResources.Trajeto_SemConexao_Msg,
                        AppResources.Trajeto_SemConexao_Title,
                        MessageBoxButton.OK);

                    Dispatcher.BeginInvoke(() =>
                    {
                        NavigationService.GoBack();
                    });
                }
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        NavigationService.GoBack();
                    });
            }
        }

        private void Start_ReverseGeocodeQuery()
        {
            reverseGeocodeQuery = new ReverseGeocodeQuery()
            {
                GeoCoordinate = new GeoCoordinate(latitude, longitude)
            };

            spinnerSP.SetValue(Grid.RowProperty, 2);
            spinnerSP.SetValue(Grid.RowSpanProperty, 1);
            
            myMap.Center = reverseGeocodeQuery.GeoCoordinate;
            myMap.Visibility = Visibility.Visible;

            reverseGeocodeQuery.QueryCompleted += reverseGeocodeQuery_QueryCompleted;
            reverseGeocodeQuery.QueryAsync();
        }

        private void reverseGeocodeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                geoCoordenadas.Add(e.Result[0].GeoCoordinate);

                routeQuery = new RouteQuery()
                {
                    Waypoints = geoCoordenadas
                };

                routeQuery.QueryCompleted += routeQuery_QueryCompleted;
                routeQuery.QueryAsync();

                reverseGeocodeQuery.Dispose();
                GC.SuppressFinalize(reverseGeocodeQuery);
            }
        }

        private void routeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            if (e.Error == null)
            {
                route = e.Result;
                MapRoute mapRoute = new MapRoute(route);                
                myMap.AddRoute(mapRoute);

                rotas = new List<string>();

                foreach (RouteLeg leg in route.Legs)
                {
                    for (int idx = 1; idx < leg.Maneuvers.Count; idx++)
                    {
                        RouteManeuver maneuver = leg.Maneuvers[idx];
                        string instructionText;

                        if (idx < (leg.Maneuvers.Count - 1))
                        {
                            instructionText = idx + ". " + maneuver.InstructionText;
                        }
                        else
                        {
                            instructionText = "»  " + maneuver.InstructionText;
                        }

                        rotas.Add(instructionText);
                    }
                }

                DrawMarkers();

                listaRotas.ItemsSource = rotas;

                FinalizaDisplay();

                routeQuery.Dispose();
                GC.SuppressFinalize(routeQuery);
            }
        }

        private void FinalizaDisplay()
        {
            if (listaRotas.ItemsSource != null)
            {
                SystemTray.ProgressIndicator.IsVisible = false;
                SpinningAnimation.Stop();
                spinnerSP.Visibility = Visibility.Collapsed;

                spTitle.Margin = new Thickness(0, 17, 0, 15);

                myMap.SetView(position.Coordinate.ToGeoCoordinate(), 17);

                name.Text = nome;
                distance.Text = string.Format("{0:0.00} km", (double)route.LengthInMeters / 1000);

                ToVisible(true, Header, listaRotas, btCenter);

                ApplicationBar.IsVisible = true;
            }

            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        private void ToVisible(bool isTrue, params UIElement[] obj)
        {
            for (int i = 0; i < obj.Length; i++)
            {
                if (isTrue)
                {
                    obj[i].Visibility = Visibility.Visible;
                }
                else
                {
                    obj[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        private void DrawMarkers()
        {
            MapLayer mapLayer = new MapLayer();

            for (int idx = 1; idx < route.Legs[0].Maneuvers.Count - 1; idx++)
                {
                    DrawMapMarker(route.Legs[0].Maneuvers[idx].StartGeoCoordinate, mapLayer, idx, 
                        route.Legs[0].Maneuvers[idx].InstructionText);
                }

            myMap.Layers.Add(mapLayer);
        }

        private void DrawMapMarker(GeoCoordinate coordinate, MapLayer mapLayer, int idx, string instructionText)
        {
            // Cria um marcador
            Ellipse ellipse = new Ellipse()
            {
                StrokeThickness = 2.5,
                Stroke = new SolidColorBrush(Colors.Black),
                Fill = new SolidColorBrush(Colors.White)
            };

            //Define tamanho da elipse dependendo do índice
            if (idx < 10)
            {
                ellipse.Height = 30;
                ellipse.Width = 30;
            }
            else
            {
                ellipse.Height = 35;
                ellipse.Width = 35;
            }

            TextBlock num = new TextBlock()
            {
                Text = idx.ToString(),
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.SemiBold
            };

            Ellipse info = new Ellipse()
            {
                Height = 60,
                Width = 60,
                StrokeThickness = 0,
                Fill = new SolidColorBrush(Colors.Transparent)
            };

            info.Tag = idx.ToString() + ". " + instructionText;
            info.Tap += info_Tap;
            
            // Cria os MapOverlays e adiciona os marcadores
            MapOverlay overlay = new MapOverlay()
            {
                Content = ellipse,
                GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude),
                PositionOrigin = new Point(0.5, 0.5)
            };
            
            mapLayer.Add(overlay);

            MapOverlay overlayText = new MapOverlay()
            {
                Content = num,
                GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude)
            };

            //Define posição da overlay dependendo do índice
            if (idx < 10)
            {
                overlayText.PositionOrigin = new Point(0.5, 0.5);
            }
            else
            {
                overlayText.PositionOrigin = new Point(0.525, 0.5);
            }

            mapLayer.Add(overlayText);

            MapOverlay overlayInfo = new MapOverlay()
            {
                Content = info,
                GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude),
                PositionOrigin = new Point(0.5, 0.5)
            };
            
            mapLayer.Add(overlayInfo);
        }

        private void info_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (Grid.GetRowSpan(myMap) == 3)
            {
                Ellipse ell = (Ellipse)sender;
                MessageBox.Show(ell.Tag.ToString());
            }
        }

        private void myMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (listaRotas.ItemsSource != null)
            {
                ToVisible(false, Header, listaRotas);

                myMap.SetValue(Grid.RowSpanProperty, 3);
                
                btCenter.SetValue(Grid.RowSpanProperty, 3);
                btCenter.Margin = new Thickness(0, 0, 15, 25);
            }
        }

        private void marker_Loaded(object sender, RoutedEventArgs e)
        {
            UserLocationMarker marker = (UserLocationMarker)this.FindName("marker");
            marker.GeoCoordinate = position.Coordinate.ToGeoCoordinate();
            marker.Visibility = Visibility.Visible;
        }

        private void pushpin_Loaded(object sender, RoutedEventArgs e)
        {
            Pushpin pushpin = (Pushpin)this.FindName("pushpin");
            pushpin.GeoCoordinate = reverseGeocodeQuery.GeoCoordinate;
            pushpin.Content = nome;
            pushpin.Visibility = Visibility.Visible;
        }

        private void btCenter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            myMap.SetView(position.Coordinate.ToGeoCoordinate(), 17);
        }

        private void listaRotas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var obj = listaRotas.SelectedItem;
            int idx = listaRotas.ItemsSource.IndexOf(obj);

            if (!(idx < 0))
            {
                listaRotas.ScrollTo(rotas[idx]);
                myMap.SetView(route.Legs[0].Maneuvers[idx + 1].StartGeoCoordinate, myMap.ZoomLevel);
            }
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar()
            {
                IsVisible = false
            };

            // Ícone

            ApplicationBarIconButton navigation = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/map.turningpoint.png", UriKind.Relative),
                Text = AppResources.IconButton_Navegacao
            };

            navigation.Click += ApplicationBarIconButton_Click;

            ApplicationBar.Buttons.Add(navigation);

            // Menus

            ApplicationBarMenuItem view = new ApplicationBarMenuItem(AppResources.MenuItem_AlternarVista);
            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem(AppResources.MenuItem_Ajustes);
            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem(AppResources.MenuItem_Sobre);

            view.Click += ApplicationBarMenuItem_Click;
            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;

            ApplicationBar.MenuItems.Add(view);
            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);
        }

        private async void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            string uri = "ms-drive-to:?destination.latitude=" + latitude.ToString(CultureInfo.InvariantCulture) + 
                "&destination.longitude=" + longitude.ToString(CultureInfo.InvariantCulture) + "&destination.name=" + nome;

            await Launcher.LaunchUriAsync(new Uri(uri));
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            string menuItemText = (sender as ApplicationBarMenuItem).Text;

            if (menuItemText.Equals(AppResources.MenuItem_AlternarVista))
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
            else if (menuItemText.Equals(AppResources.MenuItem_Ajustes))
            {
                NavigationService.Navigate(new Uri("/Ajustes.xaml", UriKind.Relative));
            }
            else if (menuItemText.Equals(AppResources.MenuItem_Sobre))
            {
                NavigationService.Navigate(new Uri("/Sobre.xaml", UriKind.Relative));
            }
        }
    }
}