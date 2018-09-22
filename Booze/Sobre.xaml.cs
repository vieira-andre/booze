using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace Booze
{
    public partial class Sobre : PhoneApplicationPage
    {
        public Sobre()
        {
            InitializeComponent();

            version.Text += ": " + App.GetAppVersion();
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RotateTransition rotate = new RotateTransition()
            {
                Mode = RotateTransitionMode.Out180Clockwise
            };

            ITransition transition = rotate.GetTransition((Grid)sender);

            transition.Begin();

            transition.Completed += delegate
            {
                transition.Stop();

                switch ((sender as Grid).Name)
                {
                    case "contato":

                        EmailComposeTask contato = new EmailComposeTask()
                        {
                            To = "boozewp@live.com",
                            Subject = AppResources.Sobre_Contato_Subject
                        };

                        contato.Show();

                        break;

                    case "share":

                        string[] compartilhar = new string[4] { string.Empty, 
                            AppResources.Sobre_Compartilhar_RedesSociais, "E-mail", "SMS" };

                        lpk.Header = AppResources.Sobre_Compartilhar;
                        lpk.ItemsSource = compartilhar;

                        lpk.Open();

                        break;

                    case "avaliar":

                        IsolatedStorageSettings.ApplicationSettings["cancelReview"] = 1;

                        MarketplaceReviewTask avaliar = new MarketplaceReviewTask();
                        avaliar.Show();

                        break;

                    default:
                        break;
                }
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask contato = new EmailComposeTask()
            {
                To = "boozewp@live.com",
                Subject = AppResources.Sobre_RelatarBug_Subject,
                Body = "\n\n\n" + App.userInfo
            };

            contato.Show();
        }

        private void lpk_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lpk.SelectedIndex != 0)
            {
                string linkUri = Windows.ApplicationModel.Store.CurrentApp.LinkUri.ToString();

                if (lpk.SelectedIndex == 1)
                {
                    ShareLinkTask share = new ShareLinkTask()
                    {
                        Title = AppResources.Sobre_Compartilhar_Message,
                        LinkUri = new Uri(linkUri, UriKind.Absolute)
                    };

                    share.Show();
                }
                else if (lpk.SelectedIndex == 2)
                {
                    EmailComposeTask email = new EmailComposeTask()
                    {
                        Subject = AppResources.Sobre_Compartilhar_Message,
                        Body = linkUri
                    };

                    email.Show();
                }
                else if (lpk.SelectedIndex == 3)
                {
                    SmsComposeTask sms = new SmsComposeTask()
                    {
                        Body = AppResources.Sobre_Compartilhar_Message + ": " + linkUri
                    };

                    sms.Show();
                }

                lpk.SelectedIndex = 0;
            }
        }

        private async void privacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(AppResources.Sobre_PrivacyPolicy_Link));
        }
    }
}