using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EzIIOLib;

namespace SimpleEzIIOController
{
    public class SlideViewModel : INotifyPropertyChanged
    {
        private readonly PneumaticSlide slide;
        private SlidePosition position;
        private bool extendedSensorActive;
        private bool retractedSensorActive;
        private bool isMoving;

        public string Name => slide.Name;

        public SlidePosition Position
        {
            get => position;
            private set
            {
                position = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        public bool ExtendedSensorActive
        {
            get => extendedSensorActive;
            set
            {
                extendedSensorActive = value;
                OnPropertyChanged(nameof(ExtendedSensorActive));
            }
        }

        public bool RetractedSensorActive
        {
            get => retractedSensorActive;
            set
            {
                retractedSensorActive = value;
                OnPropertyChanged(nameof(RetractedSensorActive));
            }
        }

        public bool IsMoving
        {
            get => isMoving;
            set
            {
                isMoving = value;
                OnPropertyChanged(nameof(IsMoving));
            }
        }

        public PneumaticSlide Slide => slide;

        public SlideViewModel(PneumaticSlide slide)
        {
            this.slide = slide;
            this.Position = slide.Position;

            // Subscribe to slide events
            slide.PositionChanged += (s, pos) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Position = pos;
                    IsMoving = pos == SlidePosition.Moving;
                });
            };

            slide.SensorStateChanged += (s, state) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ExtendedSensorActive = state.ExtendedSensor;
                    RetractedSensorActive = state.RetractedSensor;
                });
            };

            // Initialize sensor states
            var initialState = slide.GetSensorStates();
            ExtendedSensorActive = initialState.ExtendedSensor;
            RetractedSensorActive = initialState.RetractedSensor;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class PneumaticTestWindow : Window
    {
        private ObservableCollection<SlideViewModel> slides;
        private IOConfiguration config;

        public PneumaticTestWindow()
        {
            InitializeComponent();
            slides = new ObservableCollection<SlideViewModel>();
            slidesItemsControl.ItemsSource = slides;
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "IOConfig.json");
                string jsonContent = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<IOConfiguration>(jsonContent);

                RefreshSlides();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogEvent($"Configuration Error: {ex.Message}");
            }
        }

        private void RefreshSlides()
        {
            // Clean up existing slides
            foreach (var slideVM in slides)
            {
                slideVM.Slide.Dispose();
            }
            slides.Clear();

            // Create new slides from configuration
            if (config.PneumaticSlides != null)
            {
                foreach (var slideConfig in config.PneumaticSlides)
                {
                    try
                    {
                        var slide = new PneumaticSlide(slideConfig);
                        var slideVM = new SlideViewModel(slide);

                        // Subscribe to events
                        slide.PositionChanged += (s, pos) => LogEvent($"{slideConfig.Name}: Position changed to {pos}");
                        slide.Error += (s, err) => LogEvent($"{slideConfig.Name} Error: {err}");

                        slides.Add(slideVM);
                        LogEvent($"Initialized {slideConfig.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogEvent($"Error initializing {slideConfig.Name}: {ex.Message}");
                    }
                }
            }
        }

        private async void ExtendButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string slideName)
            {
                var slideVM = slides.FirstOrDefault(s => s.Name == slideName);
                if (slideVM != null && !slideVM.IsMoving)
                {
                    button.IsEnabled = false;
                    try
                    {
                        LogEvent($"Extending {slideName}...");
                        bool success = await slideVM.Slide.ExtendAsync();
                        LogEvent($"{slideName} extend operation {(success ? "completed" : "failed")}");
                    }
                    catch (Exception ex)
                    {
                        LogEvent($"Error extending {slideName}: {ex.Message}");
                    }
                    finally
                    {
                        button.IsEnabled = true;
                    }
                }
            }
        }

        private async void RetractButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string slideName)
            {
                var slideVM = slides.FirstOrDefault(s => s.Name == slideName);
                if (slideVM != null && !slideVM.IsMoving)
                {
                    button.IsEnabled = false;
                    try
                    {
                        LogEvent($"Retracting {slideName}...");
                        bool success = await slideVM.Slide.RetractAsync();
                        LogEvent($"{slideName} retract operation {(success ? "completed" : "failed")}");
                    }
                    catch (Exception ex)
                    {
                        LogEvent($"Error retracting {slideName}: {ex.Message}");
                    }
                    finally
                    {
                        button.IsEnabled = true;
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadConfiguration();
        }

        private void LogEvent(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                while (eventLogListBox.Items.Count > 100) // Keep last 100 events
                {
                    eventLogListBox.Items.RemoveAt(eventLogListBox.Items.Count - 1);
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            foreach (var slideVM in slides)
            {
                slideVM.Slide.Dispose();
            }
            base.OnClosing(e);
        }
    }
}