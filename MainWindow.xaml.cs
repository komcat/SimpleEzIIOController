using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using EzIIOLib;

namespace SimpleEzIIOController
{
    public partial class MainWindow : Window
    {
        private EzIIOManager ezIIOManager;

        public MainWindow()
        {
            InitializeComponent();
            deviceCombo.SelectedIndex = 0;
            UpdateDeviceInfo();
        }

        private void UpdateDeviceInfo()
        {
            try
            {
                string selectedDevice = ((ComboBoxItem)deviceCombo.SelectedItem).Content.ToString();
                CreateEzIIOManager(selectedDevice);
                ipTextBlock.Text = ezIIOManager.IPAddress;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing device: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateEzIIOManager(string deviceName)
        {
            ezIIOManager?.Dispose();
            ezIIOManager = new EzIIOManager(deviceName);
            SetupEventHandlers();
            SetupDataContext();
        }

        private void SetupEventHandlers()
        {
            ezIIOManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            ezIIOManager.Error += OnError;
            ezIIOManager.InputStateChanged += OnInputStateChanged;
            ezIIOManager.OutputStateChanged += OnOutputStateChanged;
        }

        private void SetupDataContext()
        {
            inputPinsListView.ItemsSource = ezIIOManager.InputPins;
            outputPinsListView.ItemsSource = ezIIOManager.OutputPins;
        }

        private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ezIIOManager?.IsConnected == true)
            {
                MessageBox.Show("Please disconnect before changing device.",
                    "Device Change", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Revert selection
                deviceCombo.SelectionChanged -= DeviceCombo_SelectionChanged;
                deviceCombo.SelectedItem = e.RemovedItems[0];
                deviceCombo.SelectionChanged += DeviceCombo_SelectionChanged;
                return;
            }

            UpdateDeviceInfo();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ezIIOManager.IsConnected)
            {
                statusBarText.Text = "Connecting...";
                if (ezIIOManager.Connect())
                {
                    connectButton.Content = "Disconnect";
                    deviceCombo.IsEnabled = false;
                }
            }
            else
            {
                ezIIOManager.Disconnect();
                connectButton.Content = "Connect";
                deviceCombo.IsEnabled = true;
                statusBarText.Text = "Disconnected";
            }
        }

        private void OutputPin_Click(object sender, RoutedEventArgs e)
        {
            if (!ezIIOManager.IsConnected) return;

            var button = sender as Button;
            if (button?.Tag is string pinName)
            {
                var pin = ezIIOManager.OutputPins.First(p => p.Name == pinName);
                ezIIOManager.SetOutput(pinName, !pin.State);
            }
        }

        private void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                statusTextBlock.Text = isConnected ? "Connected" : "Disconnected";
                statusTextBlock.Foreground = isConnected ? Brushes.Green : Brushes.Red;
                statusBarText.Text = isConnected ? "Connected successfully" : "Disconnected";
            });
        }

        private void OnError(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                statusBarText.Text = $"Error: {message}";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnInputStateChanged(object sender, (string PinName, bool State) e)
        {
            Dispatcher.Invoke(() =>
            {
                statusBarText.Text = $"Input '{e.PinName}' changed to {(e.State ? "ON" : "OFF")}";
            });
        }

        private void OnOutputStateChanged(object sender, (string PinName, bool State) e)
        {
            Dispatcher.Invoke(() =>
            {
                statusBarText.Text = $"Output '{e.PinName}' changed to {(e.State ? "ON" : "OFF")}";
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            ezIIOManager?.Dispose();
            base.OnClosing(e);
        }
    }

}