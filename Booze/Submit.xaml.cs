using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Windows.Devices.Geolocation;

namespace Booze
{
    public partial class Submit : PhoneApplicationPage
    {
        private string standardText = AppResources.Submit_StandardText;
        private string coordenadas, information;
        private Geoposition position;

        public Submit()
        {
            InitializeComponent();

            tb_Adc.Text = standardText;

            BuildApplicationBar();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (AreFieldsNotNull())
            {
                MessageBoxResult result =
                    MessageBox.Show(AppResources.Submit_NotNull_Message, AppResources.Submit_NotNull_Caption, 
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private bool AreFieldsNotNull()
        {
            if (!(string.IsNullOrWhiteSpace(tb_Nome.Text) && string.IsNullOrWhiteSpace(tb_Bairro.Text) &&
                string.Equals(tb_Adc.Text, standardText)) || position != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void tb_Adc_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.Equals(tb_Adc.Text, standardText))
            {
                tb_Adc.Text = string.Empty;
                tb_Adc.Foreground = new SolidColorBrush(Colors.Black);
                tb_Adc.FontStyle = FontStyles.Normal;
            }
        }

        private void tb_Adc_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tb_Adc.Text))
            {
                tb_Adc.Text = standardText;
                tb_Adc.Foreground = new SolidColorBrush(Colors.Gray);
                tb_Adc.FontStyle = FontStyles.Italic;
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            bt_Position.IsEnabled = true;
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            bt_Position.IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (position == null)
            {
                MessageBoxResult result = 
                    MessageBox.Show(AppResources.Submit_GetLocation_Message, 
                    AppResources.Submit_GetLocation_Caption, 
                    MessageBoxButton.OKCancel);
                            
                if (result == MessageBoxResult.OK)
                {
                    if (App.LocationConsent())
                    {
                        Start_GetLocation();
                    }
                }
            }
            else
            {
                MessageBoxResult readquirir =
                    MessageBox.Show(AppResources.Submit_Reacquire_Message, AppResources.Submit_Reacquire_Caption, 
                    MessageBoxButton.OKCancel);

                if (readquirir == MessageBoxResult.OK)
                {
                    position = null;
                    Start_GetLocation();
                }
            }

                //case "bt_Foto":

                //    if (checkBox.IsChecked.Value)
                //    {
                //        MessageBoxResult shot = MessageBox.Show("Com seu OK, a câmera se abrirá para que você registre uma imagem do bar onde está.\n\nPara anexá-la ao envio das demais informações, clique em \"anexar\" (botão central da barra de aplicação) na tela pós-conclusão e selecione-a junto à sua galeria de fotos.\n\nCaso você já possua uma imagem salva no aparelho, apenas cancele e proceda conforme descrito acima.",
                //        "Fotografia do local", MessageBoxButton.OKCancel);

                //        if (shot == MessageBoxResult.OK)
                //        {
                //            CameraCaptureTask cameraCapture = new CameraCaptureTask();

                //            cameraCapture.Show();
                //        }
                //    }
                //    else
                //    {
                //        MessageBoxResult saved = MessageBox.Show("Se você possui uma imagem salva do bar, anexe-a ao envio das demais informações clicando em \"anexar\" (botão central da barra de aplicação) na tela pós-conclusão e selecionando-a junto à sua galeria de fotos.\n\nCaso você esteja no bar e deseje registrar uma imagem do local, assinale a respectiva caixa de seleção e clique novamente neste botão para ser direcionado à câmera.",
                //        "Fotografia do local", MessageBoxButton.OK);
                //    }

                //    break;

                //default:
                //    break;
        }

        private async void Start_GetLocation()
        {
            LayoutAdjustment(true);

            position = await App.GetLocation(this.Dispatcher, true);

            if (position != null)
            {
                coordenadas = "{" + position.Coordinate.Latitude.ToString(CultureInfo.InvariantCulture) + ", " +
                position.Coordinate.Longitude.ToString(CultureInfo.InvariantCulture) + "}";

                bt_Position.Background = new SolidColorBrush(Colors.Green);
            }
            
            SystemTray.ProgressIndicator.IsVisible = false;

            LayoutAdjustment(false);

            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        private void LayoutAdjustment(bool IsGettingLocation)
        {
            if (IsGettingLocation)
            {
                spTitle.Margin = new Thickness(0, 34, 0, 15);

                ToEnabled(false, checkBox, bt_Position/*, bt_Foto*/);
                ApplicationBar.IsMenuEnabled = false;

                for (int i = 0; i < ApplicationBar.Buttons.Count; i++)
                {
                    (ApplicationBar.Buttons[i] as ApplicationBarIconButton).IsEnabled = false;
                }
            }
            else
            {
                spTitle.Margin = new Thickness(0, 17, 0, 15);

                ToEnabled(true, checkBox, bt_Position/*, bt_Foto*/);
                ApplicationBar.IsMenuEnabled = true;

                for (int i = 0; i < ApplicationBar.Buttons.Count; i++)
                {
                    (ApplicationBar.Buttons[i] as ApplicationBarIconButton).IsEnabled = true;
                }
            }
        }

        private void ToEnabled(bool isTrue, params Control[] obj)
        {
            for (int i = 0; i < obj.Length; i++)
            {
                if (isTrue)
                {
                    obj[i].IsEnabled = true;
                }
                else
                {
                    obj[i].IsEnabled = false;
                }
            }
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            // Ícones

            ApplicationBarIconButton concluir = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/check.png", UriKind.Relative),
                Text = AppResources.IconButton_Concluir
            };

            ApplicationBarIconButton ajuda = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/images/icons/questionmark.png", UriKind.Relative),
                Text = AppResources.IconButton_Ajuda
            };

            ApplicationBar.Buttons.Add(concluir);
            ApplicationBar.Buttons.Add(ajuda);

            concluir.Click += ApplicationBarIconButton_Click;
            ajuda.Click += ApplicationBarIconButton_Click;

            // Menus

            ApplicationBarMenuItem ajustes = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Ajustes
            };

            ApplicationBarMenuItem sobre = new ApplicationBarMenuItem()
            {
                Text = AppResources.MenuItem_Sobre
            };

            ApplicationBar.MenuItems.Add(ajustes);
            ApplicationBar.MenuItems.Add(sobre);

            ajustes.Click += ApplicationBarMenuItem_Click;
            sobre.Click += ApplicationBarMenuItem_Click;
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            string iconButtonText = (sender as ApplicationBarIconButton).Text;

            if (iconButtonText.Equals(AppResources.IconButton_Concluir))
            {
                if (AreFieldsOK())
                {
                    EmailComposeTask submeter = new EmailComposeTask()
                    {
                        To = "boozewp@live.com",
                        Subject = AppResources.Submit_Email_Subject,
                        Body = information
                    };

                    submeter.Show();
                }
            }
            else if (iconButtonText.Equals(AppResources.IconButton_Ajuda))
            {
                MessageBox.Show(AppResources.Submit_Ajuda_Message, 
                    AppResources.Submit_Ajuda_Caption, MessageBoxButton.OK);
            }
        }

        private bool AreFieldsOK()
        {
            if (string.IsNullOrWhiteSpace(tb_Nome.Text))
            {
                MessageBox.Show(AppResources.Submit_PreencherCampo, AppResources.Submit_NomeLocal, MessageBoxButton.OK);
                tb_Nome.Focus();

                return false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tb_Bairro.Text))
                {
                    MessageBox.Show(AppResources.Submit_PreencherCampo, AppResources.Submit_Bairro, MessageBoxButton.OK);
                    tb_Bairro.Focus();

                    return false;
                }
                else
                {
                    information = AppResources.Submit_Information_Nome + tb_Nome.Text +
                        "\n" + AppResources.Submit_Information_Bairro + tb_Bairro.Text;

                    if (!string.Equals(tb_Adc.Text, standardText))
                    {
                        information += "\n\n" + tb_Adc.Text;
                    }

                    if (checkBox.IsChecked.Value && position != null)
                    {
                        information += "\n\n" + coordenadas;
                    }

                    return true;
                }
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