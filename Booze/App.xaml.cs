using Booze.Classes;
using Booze.Data;
using Booze.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using Windows.Devices.Geolocation;

namespace Booze
{
    public partial class App : Application
    {
        #region app info

        public static string GetAppVersion()
        {
            using (var stream = new FileStream("WMAppManifest.xml", FileMode.Open, FileAccess.Read))
            {
                var appVersion = XElement.Load(stream).Descendants("App").FirstOrDefault().Attribute("Version");
                return appVersion != null ? appVersion.Value : null;
            }
        }

        private static string osVersion = "OS: " + Environment.OSVersion.Version.ToString();
        private static string deviceStatus = "Device: " + DeviceStatus.DeviceManufacturer + " " + DeviceStatus.DeviceName;
        private static string firmwareVersion = "Firmware: " + DeviceStatus.DeviceFirmwareVersion;

        public static string userInfo = "<!-- User Info -->\n\n" + "App: " + GetAppVersion() + "\n" + osVersion + "\n" + deviceStatus + "\n" + firmwareVersion;

        #endregion

        #region dados

        public static IList<Bar> bares = RecuperaPreparaDados();

        public static IList<Bar> RecuperaPreparaDados()
        {
            IList<BarModelRaiz> dados;

            using (StreamReader sr = new StreamReader("Data/boozedb.json"))
            {
                dados = JsonConvert.DeserializeObject<IList<BarModelRaiz>>(sr.ReadToEnd());
            }

            var lista = new List<Bar>();

            foreach (var bar in dados)
            {
                if (bar.Img != string.Empty) bar.Img = string.Format("/images/bars/{0}.jpg", bar.Id);

                string[] telPrep = { string.Empty };
                if (bar.Telefone != string.Empty) { telPrep = bar.Telefone.Replace(" ", "").Split(','); }

                var hoursPrep = bar.Horarios.Replace("<br>", "\n").Replace('?', '→');

                var coordSplit = bar.Coordenadas.Replace(" ", "").Split(',');
                var coordPrep = new GeoCoordinate(double.Parse(coordSplit[0], CultureInfo.InvariantCulture), double.Parse(coordSplit[1], CultureInfo.InvariantCulture));

                lista.Add(new Bar(bar.Id, bar.Nome, bar.Bairro, bar.Img, bar.Endereco, telPrep, hoursPrep, coordPrep));
            }

            return lista;
        }

        #endregion dados

        #region favoritos

        public ObservableCollection<string> favoritos;
        public ObservableCollection<Bar> sectionF;

        public bool ExistemFavoritos()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("Favoritos"))
            {
                if (favoritos == null)
                {
                    favoritos = new ObservableCollection<string>(IsolatedStorageSettings.ApplicationSettings["Favoritos"] as ObservableCollection<string>);

                    sectionF = new ObservableCollection<Bar>();
                    Load_SectionF();

                    IsolatedStorageSettings.ApplicationSettings.Save();
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void SaveISS()
        {
            if ((Application.Current as App).ExistemFavoritos())
            {
                IsolatedStorageSettings.ApplicationSettings.Remove("Favoritos");
            }

            if ((Application.Current as App).favoritos.Count > 0)
            {
                IsolatedStorageSettings.ApplicationSettings.Add("Favoritos", (Application.Current as App).favoritos);

                Load_SectionF();
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        private void Load_SectionF()
        {
            if (sectionF != null)
            {
                foreach (Bar bar in bares)
                {
                    if (favoritos.Contains(bar.nome))
                    {
                        if (!sectionF.Contains(bar))
                        {
                            sectionF.Add(bar);
                            sectionF.Sort((x, y) => x.nome.CompareTo(y.nome));
                        }
                    }
                    else
                    {
                        if (sectionF.Contains(bar))
                        {
                            sectionF.Remove(bar);
                        }
                    }
                }
            }
        }

        #endregion favoritos

        #region geolocation

        public static bool LocationConsent()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == true)
                {
                    return true;
                }
                else
                {
                    return AskConsent() ? true : false;
                }
            }
            else
            {
                return AskConsent() ? true : false;
            }
        }

        private static bool AskConsent()
        {
            MessageBoxResult result =
                MessageBox.Show(AppResources.MessageBox_Permissao_Message,
                AppResources.MessageBox_Permissao_Caption,
                MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                IsolatedStorageSettings.ApplicationSettings.Save();

                return true;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                IsolatedStorageSettings.ApplicationSettings.Save();

                return false;
            }
        }

        public static TimeSpan LocationTimeout()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationTimeout"))
            {
                return (TimeSpan)IsolatedStorageSettings.ApplicationSettings["LocationTimeout"];
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] = TimeSpan.FromSeconds(10);

                return TimeSpan.FromSeconds(10);
            }
        }

        public static async Task<Geoposition> GetLocation(Dispatcher dispatcher, bool isAtBarOrUpdating)
        {
            Geoposition position;

            Geolocator geolocator = new Geolocator()
            {
                DesiredAccuracy = PositionAccuracy.High
            };

            dispatcher.BeginInvoke(() =>
                {
                    SystemTray.ProgressIndicator = new ProgressIndicator()
                    {
                        Text = AppResources.SysTray_Localizacao,
                        IsVisible = true
                    };

                    if (isAtBarOrUpdating)
                    {
                        SystemTray.ProgressIndicator.IsIndeterminate = true;
                    }
                });

            try
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

                position = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.Zero,
                    timeout: LocationTimeout()
                );

                return position;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(AppResources.MessageBox_Erro_Localizacao,
                    AppResources.MessageBox_Erro_Caption,
                    MessageBoxButton.OK);

                dispatcher.BeginInvoke(() => { LocationSettings(); });

                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;

                return position = null;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    MessageBox.Show(AppResources.MessageBox_Erro_Localizacao,
                        AppResources.MessageBox_Erro_Caption,
                        MessageBoxButton.OK);

                    dispatcher.BeginInvoke(() => { LocationSettings(); });
                }
                else
                {
                    MessageBox.Show(ex.ToString(), AppResources.MessageBox_Erro_Caption, MessageBoxButton.OK);
                }

                dispatcher.BeginInvoke(() => { SystemTray.ProgressIndicator.IsVisible = false; });

                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;

                return position = null;
            }
        }

        private static async void LocationSettings()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
        }

        #endregion

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            if ((Visibility)Application.Current.Resources["PhoneLightThemeVisibility"] == Visibility.Visible)
            {
                ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.None;
                ThemeManager.ToDarkTheme();
            }

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //

        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}