using fmRadio.Models;
using fmRadio.Resources;
using Microsoft.Devices.Radio;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Windows.Phone.Media.Devices;

namespace fmRadio
{
    public partial class MainPage : PhoneApplicationPage
    {
        private FMRadio fmRadioInstance;
        private DispatcherTimer dispatcherTimer1, dispatcherTimer2, dispatcherTimer3;

        private ObservableCollection<Station> stationList = new ObservableCollection<Station>();
        private Station stationObject = null, nextOrPreviousPreset = null, stationItem = null;

        private IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private CheckBox checkBox;
        private CustomMessageBox customMessageBox;
        private bool isCustomMessageBoxVisible = false;

        public bool isJackPlugged = false;
        private double frequencyDoubleValue;
        public ProgressIndicator progressIndicator = new ProgressIndicator();
        private string digitVal = string.Empty;
        private TextBox nameTextBox;
        private TextBlock titleBlock;
        private StackPanel stackPanel;

        // Constructor
        public MainPage()
        {
            MessageBoxResult result = MessageBoxResult.None;
            try
            {
                InitializeComponent();
                fmRadioInstance = FMRadio.Instance;
                this.Loaded += MainPage_Loaded;
                this.Unloaded += MainPage_Unloaded;
                DataContext = this;
                App.progressIndicatorSetter(true, false, AppResources.ApplicationTitle, this);
            }
            catch (RadioDisabledException b)
            {
                FlurryWP8SDK.Api.LogError("RadioDisabledException", b);
                result = MessageBox.Show(AppResources.NoFmSupport, AppResources.SomethingHappened, MessageBoxButton.OK);
            }
            catch (Exception a)
            {
                FlurryWP8SDK.Api.LogError("Exception after RadioDisabledException", a);
                result = MessageBox.Show(AppResources.NoFmSupport, AppResources.SomethingHappened, MessageBoxButton.OK);
            }
            finally
            {
                if (result == MessageBoxResult.OK) Application.Current.Terminate();
            }
        }

        private async void flurryGeoLocationSetter()
        {
            Geolocator geolocator = new Geolocator();
            string latitude = string.Empty;
            string longtitude = string.Empty;
            string accuracy = string.Empty;
            geolocator.DesiredAccuracyInMeters = 50;
            bool success = true;
            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync
                (
                    TimeSpan.FromHours(12), TimeSpan.FromSeconds(10)
                );
                latitude = geoposition.Coordinate.Latitude.ToString("0.00");
                longtitude = geoposition.Coordinate.Longitude.ToString("0.00");
                accuracy = geoposition.Coordinate.Accuracy.ToString();
            }
            catch (UnauthorizedAccessException)
            {
                // the app does not have the right capability or the location master switch is off
                success = false;
            }
            catch (TargetInvocationException)
            {
                success = false;
            }
            catch (Exception)
            {
                success = false;
            }
            if (success)
            {
                FlurryWP8SDK.Api.SetLocation(Convert.ToDouble(latitude), Convert.ToDouble(longtitude), float.Parse(accuracy, CultureInfo.InvariantCulture.NumberFormat));
            }
            //else
            //{
            //    FlurryWP8SDK.Api.LogError("Location is disabled", null);
            //}
        }

        private void writeToFile(string name, string frequency)
        {
            if (name != null)
            {
                stationObject = new Station();
                stationObject.Name = name;
                stationObject.Frequency = frequency;
                stationList.Add(stationObject);
            }

            IsolatedStorage.SaveToIsolatedStorage(stationList, "list");
            readFromFile();
        }

        private void readFromFile()
        {
            stationList = IsolatedStorage.LoadFromIsolatedStorage<ObservableCollection<Station>>("list");

            saved_presets_list.ItemsSource = stationList.OrderBy(x => Convert.ToDouble(x.Frequency)).ToList();
            if (stationList.Count == 0)
            {
                add_some_presets.Visibility = Visibility.Visible;
            }
            else
            {
                add_some_presets.Visibility = Visibility.Collapsed;
            }
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            AudioRoutingManager.GetDefault().AudioEndpointChanged -= AudioEndpointChanged_Handler;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // AudioRoutingManager.GetDefault().AudioEndpointChanged += AudioEndpointChanged_Handler;
            plyBtnTextUpdater();
            // frequency_slider.ValueChanged -= Slider_Value_Changed;
            // radioRegionSetter();
            // frequency_slider.ValueChanged += Slider_Value_Changed;

            if (fmRadioInstance.PowerMode == RadioPowerMode.On || fmRadioInstance.SignalStrength != 0.0)
            {
                frequency_text.Text = fmRadioInstance.Frequency.ToString();
                frequency_slider.Value = Convert.ToDouble(frequency_text.Text);
                plyBtnTextUpdater();
                updateLiveTile();
                for (int i = 88; i <= 108; i++)
                {
                    if (frequency_text.Text == i.ToString())
                    {
                        frequency_text.Text += ".0";
                        break;
                    }
                }
            }

            organizeAppBarLanguage();
            connectionRenewer();
            signalMeterRenewer();
            readFromFile();
            flurryGeoLocationSetter();
            rateMyAppPrompter();
        }

        private void rateMyAppPrompter()
        {
            bool isRatePending = true;
            // override isRatePending
            if (appSettings.Contains("isRatePending"))
            {
                isRatePending = (bool)appSettings["isRatePending"];
            }
            else
            {
                appSettings.Add("isRatePending", true);
                appSettings.Save();
            }
            if (isRatePending)
            {
                // attach time for popup
                dispatcherTimer3 = new DispatcherTimer();
                dispatcherTimer3.Tick += scheduledRatingPopupTask;
                dispatcherTimer3.Interval = new TimeSpan(0, 3, 0);
                dispatcherTimer3.Start();
            }
            else
            {
                // never show popup again
                appSettings["isRatePending"] = false;
                // dispatcherTimer3.Tick -= scheduledRatingPopupTask;
                // dispatcherTimer3.Stop();
            }
        }

        private void scheduledRatingPopupTask(object sender, EventArgs e)
        {
            if (appSettings["isRatePending"].Equals(true))
            {
                if (isCustomMessageBoxVisible.Equals(false))
                {
                    checkBox = new CheckBox()
                    {
                        Content = AppResources.RatingPromptCheckBox,
                        Margin = new Thickness(0, 14, 0, -2)
                    };

                    customMessageBox = new CustomMessageBox()
                    {
                        Caption = AppResources.RatingPromptTitle,
                        Message = AppResources.RatingPromptBody,
                        Content = checkBox,
                        LeftButtonContent = AppResources.RatingPromptBtn1,
                        RightButtonContent = AppResources.RatingPromptBtn2
                    };

                    customMessageBox.Dismissed += (s1, e1) =>
                    {
                        isCustomMessageBoxVisible = false;
                        switch (e1.Result)
                        {
                            case CustomMessageBoxResult.LeftButton:
                                appSettings["isRatePending"] = false;
                                rateAndReview(this, new EventArgs());
                                break;

                            case CustomMessageBoxResult.RightButton:
                                if ((bool)checkBox.IsChecked)
                                {
                                    appSettings["isRatePending"] = false;
                                }
                                break;

                            default:
                                break;
                        }
                    };
                    customMessageBox.Show();
                    isCustomMessageBoxVisible = true;
                }
            }
            else
            {
                dispatcherTimer3.Tick -= scheduledRatingPopupTask;
                dispatcherTimer3.Stop();
            }
        }

        private void connectionRenewer()
        {
            bool isAttached = false;
            if (fmRadioInstance.PowerMode == RadioPowerMode.On)
            {
                dispatcherTimer1 = new DispatcherTimer();
                dispatcherTimer1.Tick += scheduledConnectionRenewerTask;
                isAttached = true;
                dispatcherTimer1.Interval = new TimeSpan(0, 5, 0);
                dispatcherTimer1.Start();
            }
            else
            {
                if (isAttached)
                {
                    dispatcherTimer1.Tick -= scheduledConnectionRenewerTask;
                    dispatcherTimer1.Stop();
                }
            }
        }

        private void scheduledConnectionRenewerTask(object sender, object e)
        {
            if (fmRadioInstance.PowerMode == RadioPowerMode.Off)
            {
                dispatcherTimer1.Tick -= scheduledConnectionRenewerTask;
                dispatcherTimer1.Stop();
            }
            else
            {
                frequencySetter(frequency_text.Text);
            }
        }

        private void signalMeterRenewer()
        {
            bool isAttached = false;
            if (fmRadioInstance.PowerMode == RadioPowerMode.On)
            {
                dispatcherTimer2 = new DispatcherTimer();
                dispatcherTimer2.Tick += scheduledSignalMeterTask;
                isAttached = true;
                dispatcherTimer2.Interval = new TimeSpan(0, 0, 5);
                dispatcherTimer2.Start();
            }
            else
            {
                if (isAttached)
                {
                    dispatcherTimer2.Tick -= scheduledSignalMeterTask;
                    dispatcherTimer2.Stop();
                }
            }
        }

        private void organizeAppBarLanguage()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("language"))
            {
                ApplicationBarMenuItem appBarSettingsBtn = ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
                ApplicationBarMenuItem appBarRateAndReviewBtn = ApplicationBar.MenuItems[1] as ApplicationBarMenuItem;
                ApplicationBarMenuItem appBarAboutBtn = ApplicationBar.MenuItems[2] as ApplicationBarMenuItem;

                var languageKey = IsolatedStorageSettings.ApplicationSettings["language"].ToString();
                switch (languageKey)
                {
                    case App.Languages.English:
                        appBarSettingsBtn.Text = "settings";
                        appBarRateAndReviewBtn.Text = "rate and review";
                        appBarAboutBtn.Text = "about";
                        break;

                    case App.Languages.Turkce:
                        appBarSettingsBtn.Text = "ayarlar";
                        appBarRateAndReviewBtn.Text = "derecelendir ve değerlendir";
                        appBarAboutBtn.Text = "hakkında";
                        break;
                }
            }
        }

        private void scheduledSignalMeterTask(object sender, object e)
        {
            if (fmRadioInstance.PowerMode == RadioPowerMode.Off)
            {
                dispatcherTimer2.Tick -= scheduledConnectionRenewerTask;
                dispatcherTimer2.Stop();
            }
            else
            {
                visualizeSignalStrenght();
            }
        }

        private void radioRegionSetter()
        {
            int listIndex;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("region"))
            {
                listIndex = Convert.ToInt32(IsolatedStorageSettings.ApplicationSettings["region"]);
            }
            else
            {
                listIndex = 0;
            }
            switch (listIndex)
            {
                case 0:
                    fmRadioInstance.CurrentRegion = RadioRegion.Europe;
                    fmFrequencyLow.Text = "87.5 MHz";
                    fmFrequencyHigh.Text = "108 MHz";
                    frequency_slider.Minimum = 87.5;
                    frequency_slider.Maximum = 108.0;
                    break;

                case 1:
                    fmRadioInstance.CurrentRegion = RadioRegion.UnitedStates;
                    fmFrequencyLow.Text = "87.9 MHz";
                    fmFrequencyHigh.Text = "107.9 MHz";
                    frequency_slider.Minimum = 87.9;
                    frequency_slider.Maximum = 107.9;
                    break;

                case 2:
                    fmRadioInstance.CurrentRegion = RadioRegion.Japan;
                    fmFrequencyLow.Text = "87.5 MHz";
                    fmFrequencyHigh.Text = "90.0 MHz";
                    frequency_slider.Minimum = 87.5;
                    frequency_slider.Maximum = 90.0;
                    break;
            }
            if (frequency_text.Text == "")
            {
                frequency_text.Text = fmFrequencyLow.Text.Substring(0, 4);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            AudioRoutingManager.GetDefault().AudioEndpointChanged += AudioEndpointChanged_Handler;
            base.OnNavigatedTo(e);
            // AudioRoutingManager.GetDefault().AudioEndpointChanged += AudioEndpointChanged_Handler;

            radioRegionSetter();

            plyBtnTextUpdater();
            // Radyo zaten çalıyorken uygulama tekrar çalıştırıldığında duruma adapte ol
            if (fmRadioInstance.PowerMode == RadioPowerMode.On || fmRadioInstance.SignalStrength != 0.0)
            {
                // frequency_text.Text = fmRadioInstance.Frequency.ToString();
                // string addition = frequency_text.Text + ".0";
                // frequency_slider.Value = Convert.ToDouble(addition);

                frequency_slider.Value = Convert.ToDouble(fmRadioInstance.Frequency.ToString());

                plyBtnTextUpdater();
            }
            frequency_slider.ValueChanged += Slider_Value_Changed;
        }

        private void plyBtnTextUpdater()
        {
            if (fmRadioInstance.SignalStrength != 0.0)
            {
                playButtonImage.Source = new BitmapImage(new Uri(@"Assets/AppBar/transport.pause.png", UriKind.RelativeOrAbsolute));
                // play_pause_btn.Content = "Play";
            }
            else
            {
                playButtonImage.Source = new BitmapImage(new Uri(@"Assets/AppBar/transport.play.png", UriKind.RelativeOrAbsolute));
                // play_pause_btn.Content = "Pause";
            }
        }

        private void Slider_Value_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {   // sliderdan gelen long tipindeki value'yu kısalt
                if (e.NewValue <= 99.9)//nokta dahil ilk 4 karakteri al
                {
                    // tam sayı gelirse yanına .0 ekle
                    if (e.NewValue == 88 || e.NewValue == 89 || e.NewValue == 90 || e.NewValue == 91
                        || e.NewValue == 92 || e.NewValue == 93 || e.NewValue == 94 || e.NewValue == 95
                        || e.NewValue == 96 || e.NewValue == 97 || e.NewValue == 98 || e.NewValue == 99)
                    {
                        digitVal = e.NewValue.ToString() + ".0";
                    }
                    else
                    {
                        digitVal = e.NewValue.ToString().Substring(0, 4);
                    }
                }
                else if (e.NewValue >= 100.0) // nokta dahil ilk 5 karakteri al
                {
                    // tam sayı gelirse yanına .0 ekle
                    if (e.NewValue == 100 || e.NewValue == 101 || e.NewValue == 102 || e.NewValue == 103
                        || e.NewValue == 104 || e.NewValue == 105 || e.NewValue == 106 || e.NewValue == 107 || e.NewValue == 108)
                    {
                        digitVal = e.NewValue.ToString() + ".0";
                    }
                    else
                    {
                        digitVal = e.NewValue.ToString().Substring(0, 5);
                    }
                }
                if (fmRadioInstance.PowerMode == RadioPowerMode.On || fmRadioInstance.SignalStrength != 0.0)
                {
                    // region amerika ise sadece tek sayılar gösterilecek ve tune edilebilecek
                    if (fmRadioInstance.CurrentRegion == RadioRegion.UnitedStates)
                    {
                        // stringde noktadan sonraki kısım method ile tek mi çift diye kontrol ediliyor
                        string[] parts = digitVal.Split('.');
                        int secondPart = Convert.ToInt32(parts[1]);
                        if (App.IsOdd(secondPart))
                        {
                            frequencySetter(digitVal);
                            frequency_text.Text = digitVal;
                        }
                    }
                    else
                    {
                        frequencySetter(digitVal);
                        frequency_text.Text = digitVal;
                    }
                }
                else
                {
                    if (fmRadioInstance.CurrentRegion != RadioRegion.UnitedStates)
                    {
                        // region amerika değilse dogrudan gelen string gösterilebilir
                        frequency_text.Text = digitVal;
                    }
                    else
                    {
                        string[] parts = digitVal.Split('.');
                        int secondPart = Convert.ToInt32(parts[1]);
                        if (App.IsOdd(secondPart))
                        {
                            frequency_text.Text = digitVal;
                        }
                    }
                }
            }
            catch (Exception c)
            {
                FlurryWP8SDK.Api.LogError("Slider_Value_Changed exception", c);
                MessageBox.Show(AppResources.UnexpectedError);
            }
            finally
            {
                visualizeSignalStrenght();
                updateLiveTile();
            }
        }

        public async void frequencySetter(string input)
        {
            await Task.Run(() => frequencyDoubleValue = Convert.ToDouble(input));
            try
            {
                if (fmRadioInstance.PowerMode == RadioPowerMode.On)
                {
                    // This runs asynchronously.
                    await Task.Run(() =>
                    {
                        try
                        {
                            fmRadioInstance.Frequency = frequencyDoubleValue;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                        }
                    });
                }
                else
                {
                    fmRadioInstance.PowerMode = RadioPowerMode.On;
                    // This runs asynchronously.
                    await Task.Run(() =>
                    {
                        try
                        {
                            fmRadioInstance.Frequency = frequencyDoubleValue;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                        }
                    });
                }
            }
            catch (ArgumentOutOfRangeException f)
            {
                FlurryWP8SDK.Api.LogError("Trying to set " + input + " to " + Convert.ToString(fmRadioInstance.CurrentRegion) + " frequencySetter ArgumentOutOfRangeException", f);
            }
            plyBtnTextUpdater();
        }

        private void visualizeSignalStrenght()
        {
            // sinyal kuvvetini süsler
            double sgnlStrenght = Convert.ToDouble(fmRadioInstance.SignalStrength);
            string square = HttpUtility.HtmlDecode("&#9632;");
            string empty_square = HttpUtility.HtmlDecode("&#9633;");
            if (sgnlStrenght >= 3)      //very strong
            { frequency_strenght.Text = square + square + square + square; }
            else if (sgnlStrenght >= 2) //strong
            { frequency_strenght.Text = empty_square + square + square + square; }
            else if (sgnlStrenght >= 1) //normal
            { frequency_strenght.Text = empty_square + empty_square + square + square; }
            else                        //bad
            { frequency_strenght.Text = empty_square + empty_square + empty_square + square; }
            // sinyal gücü karelerini dikey sırala
            frequency_strenght.Text = string.Join(Environment.NewLine, frequency_strenght.Text.Select(c => new string(c, 1).ToString()));
        }

        private void updateLiveTile()
        {
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault();
            if (tile != null)
            {
                FlipTileData flipTile = new FlipTileData();
                if (fmRadioInstance.PowerMode == RadioPowerMode.On)
                {
                    flipTile.Title = AppResources.ApplicationTitle;
                    flipTile.BackTitle = AppResources.ApplicationTitle;

                    // Medium size Tile 336x336 px
                    flipTile.BackContent = AppResources.TileBackText2 + frequency_text.Text;

                    flipTile.BackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative);
                    flipTile.BackBackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileMediumBack.png", UriKind.Relative);

                    // Wide size Tile 691x336 px
                    flipTile.WideBackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative);
                    flipTile.WideBackContent = AppResources.TileText2 + frequency_text.Text;
                    flipTile.WideBackBackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileLargeBack.png", UriKind.Relative);

                    // Update Live Tile
                    tile.Update(flipTile);
                }
                else
                {
                    // get random position from list in range of its count
                    int rand = new Random().Next(7);

                    flipTile.Title = AppResources.ApplicationTitle;
                    flipTile.BackTitle = AppResources.ApplicationTitle;
                    flipTile.BackContent = AppResources.TileText1;
                    //flipTile.WideBackContent =  wordList[rand] as string;
                    flipTile.WideBackContent = GetWordList()[rand];
                    flipTile.BackBackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileMediumBack.png", UriKind.Relative);
                    flipTile.WideBackBackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileLargeBack.png", UriKind.Relative);
                    tile.Update(flipTile);
                }
            }
        }

        private List<string> GetWordList() => new List<string>(){
                    "Music produces a kind of pleasure which human nature cannot do without.",
                    "Music is the universal language of mankind.",
                    "Life seems to go on without effort when we are filled with music.",
                    "Skip through your presets with ease on fmRadio.",
                    "Music can change the world because it can change people.",
                    "Music acts like a magic key, to which the most tightly closed heart opens.",
                    "News, Music, Podcasts and more on fmRadio for free."
            };

        private void previous_preset_btn_Click(object sender, RoutedEventArgs e)
        {
            stationItem = null;
            int currentlyPlayingPresetIndex = -1;
            bool match = false;
            for (int i = 0; i < stationList.Count; i++)
            {
                stationItem = stationList.ElementAt(i);
                if (frequency_text.Text == stationItem.Frequency)
                {
                    currentlyPlayingPresetIndex = saved_presets_list.ItemsSource.IndexOf(stationItem);
                    match = true;
                    break;
                }
            }
            if (!match)
            {
                currentlyPlayingPresetIndex = saved_presets_list.ItemsSource.IndexOf(saved_presets_list.SelectedItem as Station);
            }
            if (currentlyPlayingPresetIndex == -1)
            {
                currentlyPlayingPresetIndex = 0;
            }
            if (currentlyPlayingPresetIndex != -1 && currentlyPlayingPresetIndex != 0)
            {
                currentlyPlayingPresetIndex -= 1;
                nextOrPreviousPreset = (Station)saved_presets_list.ItemsSource[currentlyPlayingPresetIndex];
                saved_presets_list.SelectedItem = nextOrPreviousPreset;
            }
        }

        private void next_preset_btn_Click(object sender, RoutedEventArgs e)
        {
            stationItem = null;
            int currentlyPlayingPresetIndex = -1;
            bool match = false;
            for (int i = 0; i < stationList.Count; i++)
            {
                stationItem = stationList.ElementAt(i);
                if (frequency_text.Text == stationItem.Frequency)
                {
                    currentlyPlayingPresetIndex = saved_presets_list.ItemsSource.IndexOf(stationItem);
                    match = true;
                    break;
                }
            }
            if (!match)
            {
                currentlyPlayingPresetIndex = saved_presets_list.ItemsSource.IndexOf(saved_presets_list.SelectedItem as Station);
            }
            if (currentlyPlayingPresetIndex == -1)
            {
                currentlyPlayingPresetIndex = 0;
            }
            int lastItemIndexInList = saved_presets_list.ItemsSource.Count - 1;
            if (currentlyPlayingPresetIndex != -1 & currentlyPlayingPresetIndex != lastItemIndexInList & saved_presets_list.ItemsSource.Count != 0)
            {
                currentlyPlayingPresetIndex += 1;
                nextOrPreviousPreset = (Station)saved_presets_list.ItemsSource[currentlyPlayingPresetIndex];
                saved_presets_list.SelectedItem = nextOrPreviousPreset;
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Station item = null;
                bool match = false;
                for (int i = 0; i < stationList.Count; i++)
                {
                    item = stationList.ElementAt(i);
                    if (frequency_text.Text == item.Frequency)
                    {
                        match = true;
                        MessageBox.Show(AppResources.AlreadySavedFrequencyError, "Oops!", MessageBoxButton.OK);
                        break;
                    }
                }
                if (!match)
                {
                    titleBlock = new TextBlock()
                    {
                        FontFamily = new FontFamily("PhoneFontFamilySemiBold"),
                        FontSize = (double)Resources["PhoneFontSizeNormal"],
                        // FontSize = (Double)Resources["PhoneFontSizeLarge"],
                        Text = AppResources.NameYourPreset + ", " + frequency_text.Text,
                        Margin = new Thickness(12, 50, 12, 0),
                    };

                    nameTextBox = new TextBox()
                    {
                        MaxLength = 20,
                        Margin = new Thickness(0, 12, 12, 0)
                    };

                    stackPanel = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical
                    };
                    stackPanel.Children.Add(titleBlock);
                    stackPanel.Children.Add(nameTextBox);

                    CustomMessageBox presetNameInputMsgBox = new CustomMessageBox()
                    {
                        Margin = new Thickness(0, -35, 0, 0),
                        Content = stackPanel,
                        LeftButtonContent = AppResources.Save
                    };

                    presetNameInputMsgBox.Dismissed += (s1, e1) =>
                    {
                        switch (e1.Result)
                        {
                            case CustomMessageBoxResult.LeftButton:
                                if (nameTextBox.Text != "")
                                {
                                    writeToFile(nameTextBox.Text, frequency_text.Text);
                                }
                                else
                                {
                                    writeToFile("-", frequency_text.Text);
                                }
                                break;

                            default:
                                break;
                        }
                    };
                    nameTextBox.Loaded += nameTextBox_Loaded;
                    presetNameInputMsgBox.Show();
                }
            }
            catch (Exception d)
            {
                FlurryWP8SDK.Api.LogError("save_btn_click exception", d);
                MessageBox.Show(AppResources.FrequencySaveError, "Ahh..!", MessageBoxButton.OK);
            }
        }

        private void nameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            nameTextBox.Focus();
        }

        private void play_pause_btn_Click(object sender, RoutedEventArgs e)
        {
            switch (fmRadioInstance.PowerMode)
            {
                case RadioPowerMode.Off:
                    if (isJackPlugged)
                    {
                        fmRadioInstance.PowerMode = RadioPowerMode.On;
                        frequencySetter(frequency_text.Text);
                        playButtonImage.Source = new BitmapImage(new Uri(@"Assets/AppBar/transport.pause.png", UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        MessageBox.Show(AppResources.NoHeadPhoneIsPluggedError, "", MessageBoxButton.OK);
                    }
                    break;

                case RadioPowerMode.On:
                    fmRadioInstance.PowerMode = RadioPowerMode.Off;
                    playButtonImage.Source = new BitmapImage(new Uri(@"Assets/AppBar/transport.play.png", UriKind.RelativeOrAbsolute));
                    break;
            }
        }

        private void navToinput(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/frequencyInput.xaml?isJackPlugged=" + isJackPlugged.ToString(), UriKind.Relative));
        }

        private void navigateToAboutPage(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/about.xaml", UriKind.Relative));
        }

        private void rateAndReview(object sender, EventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        //code to create new tiles - can be used for preset pinning and directly tuning into radio stations.
        //private void pinPrimaryTile(object sender, EventArgs e)
        //{
        //    StandardTileData standardTileData = new StandardTileData();
        //    ShellTile tileToPin = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("MainPage.xaml"));

        //    standardTileData.Title = "fmRadio";
        //    standardTileData.BackgroundImage = new Uri("/Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative);

        //    if (tileToPin == null)
        //    {
        //        ShellTile.Create(new Uri("/MainPage.xaml", UriKind.Relative), standardTileData);
        //    }
        //    else
        //    {
        //        MessageBox.Show("Application tile is already pinned");
        //    }
        //}

        public void AudioEndpointChanged_Handler(AudioRoutingManager sender, object args)
        {
            string title = AppResources.NoAntenna;
            string msg = AppResources.NoAntennaErrorMessage;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("language"))
            {
                var languageKey = IsolatedStorageSettings.ApplicationSettings["language"].ToString();
                switch (languageKey)
                {
                    case App.Languages.English:
                        title = "No Antenna";
                        msg = "Your headphones are used as an FM radio antenna. To listen to the radio, connect your headphones.";
                        break;

                    case App.Languages.Turkce:
                        title = "Anten Yok";
                        msg = "Kulaklığınız FM radyo anteni olarak kullanılmaktadır. FM radyo dinleyebilmek için kulaklık takınız.";
                        break;
                }
            }

            var AudioEndPoint = sender.GetAudioEndpoint();
            switch (AudioEndPoint)
            {
                case AudioRoutingEndpoint.Default:
                case AudioRoutingEndpoint.Earpiece:
                case AudioRoutingEndpoint.Speakerphone:
                case AudioRoutingEndpoint.Bluetooth:
                case AudioRoutingEndpoint.BluetoothWithNoiseAndEchoCancellation:
                    {
                        isJackPlugged = false;
                        Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(msg, title, MessageBoxButton.OK);
                        });
                        break;
                    }
                case AudioRoutingEndpoint.WiredHeadset:
                    {
                        isJackPlugged = true;
                        break;
                    }
                case AudioRoutingEndpoint.WiredHeadsetSpeakerOnly:
                    {
                        isJackPlugged = true;
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (!isJackPlugged)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    playButtonImage.Source = new BitmapImage(new Uri(@"Assets/AppBar/transport.play.png", UriKind.RelativeOrAbsolute));
                });
            }
        }

        private void longListSelectorLongClickListener(object sender, System.Windows.Input.GestureEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            Station item = (Station)element.DataContext;

            titleBlock = new TextBlock()
            {
                FontFamily = new FontFamily("PhoneFontFamilyNormal"),
                FontSize = (double)Resources["PhoneFontSizeNormal"],
                Text = AppResources.RenameOrDeletePreset + ", " + item.Frequency,
                Margin = new Thickness(12, 50, 12, 0),
            };

            nameTextBox = new TextBox()
            {
                MaxLength = 20,
                Margin = new Thickness(0, 12, 12, 0),
                Text = item.Name
            };

            stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical
            };
            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(nameTextBox);

            CustomMessageBox presetNameInputMsgBox = new CustomMessageBox()
            {
                Margin = new Thickness(0, -35, 0, 0),
                Content = stackPanel,
                RightButtonContent = AppResources.Delete,
                LeftButtonContent = AppResources.SaveEdit
            };

            presetNameInputMsgBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.RightButton:
                        stationList.Remove(item);
                        writeToFile(null, null);
                        break;

                    case CustomMessageBoxResult.LeftButton:
                        // gets the data to overwrite to a new obj
                        Station updateStation = new Station();
                        updateStation.Frequency = item.Frequency;
                        updateStation.Name = nameTextBox.Text;
                        // removes the old items
                        stationList.Remove(item);
                        // saved the new item
                        stationList.Add(updateStation);
                        writeToFile(null, null);
                        break;

                    default:
                        break;
                }
            };
            nameTextBox.Loaded += nameTextBox_Loaded;
            presetNameInputMsgBox.Show();
        }

        private void navigateToSettingsPage(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/settings.xaml", UriKind.Relative));
            fmRadioInstance.PowerMode = RadioPowerMode.Off;
        }

        private void longListSelectorTapListener(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LLSelectorTapAndSelectionChangeCombined();
        }

        private void longListSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LLSelectorTapAndSelectionChangeCombined();
        }

        private void LLSelectorTapAndSelectionChangeCombined()
        {
            try
            {
                if (isJackPlugged)
                {
                    try
                    {
                        Station selectedOne = saved_presets_list.SelectedItem as Station;
                        if (fmRadioInstance.PowerMode == RadioPowerMode.Off)
                        {
                            fmRadioInstance.PowerMode = RadioPowerMode.On;
                        }
                        frequencySetter(selectedOne.Frequency);
                        frequency_slider.Value = Convert.ToDouble(selectedOne.Frequency);
                        saved_presets_list.SelectedItem = selectedOne;
                    }
                    catch (Exception f)
                    {
                        FlurryWP8SDK.Api.LogError("Exception after selectedOne ", f);
                    }
                }
                else
                {
                    MessageBox.Show(AppResources.NoHeadPhoneIsPluggedError, string.Empty, MessageBoxButton.OK);
                }
            }
            catch (Exception g)
            {
                FlurryWP8SDK.Api.LogError("Exception after LLSelectorTapAndSelectionChangeCombined > isJackPlugged ", g);
            }
        }
    }
}