using System;
using System.Windows;
using EzIIOLib;

namespace SimpleEzIIOController
{
    public partial class TestMultipleIODevicePage : Window
    {
        private MultiDeviceManager deviceManager;
        

        public TestMultipleIODevicePage()
        {
            InitializeComponent();
            InitializeDeviceManager();
        }

        private MultiDeviceManager CreateDeviceManager()
        {
            // Centralize device manager creation
            var deviceManager = new MultiDeviceManager();
            ConfigureDevices(deviceManager);
            return deviceManager;
        }

        private void ConfigureDevices(MultiDeviceManager deviceManager)
        {
            // Add all devices
            deviceManager.AddDevice("IOBottom");
            deviceManager.AddDevice("IOTop");

            // Connect to devices
            deviceManager.ConnectAll();
        }

        private void InitializeDeviceManager()
        {
            try
            {
                // Create device manager
                deviceManager = CreateDeviceManager();

                // Setup pin monitors
                SetupPinMonitors();

                // Setup pneumatic slide control
                SetupPneumaticSlideControl();

                statusBarText.Text = "Connected to IOBottom and IOTop devices";
            }
            catch (Exception ex)
            {
                HandleInitializationError(ex);
            }
        }

        private void SetupPinMonitors()
        {
            // Setup for IOBottom
            outputPinMonitorIOBottom.DeviceManager = deviceManager;
            outputPinMonitorIOBottom.DeviceName = "IOBottom";
            outputPinMonitorIOBottom.PinsSource = deviceManager.GetOutputPins("IOBottom");
            inputPinMonitorIOBottom.PinsSource = deviceManager.GetInputPins("IOBottom");

            // Setup for IOTop
            outputPinMonitorIOTop.DeviceManager = deviceManager;
            outputPinMonitorIOTop.DeviceName = "IOTop";
            outputPinMonitorIOTop.PinsSource = deviceManager.GetOutputPins("IOTop");
            inputPinMonitorIOTop.PinsSource = deviceManager.GetInputPins("IOTop");

            // Optional: If you want to handle the pin clicked event for logging or additional processing
            outputPinMonitorIOBottom.PinClicked += OnOutputPinClicked;
            outputPinMonitorIOTop.PinClicked += OnOutputPinClicked;
        }

        private void SetupPneumaticSlideControl()
        {
            pneumaticSlideControl.DeviceManager = deviceManager;
            pneumaticSlideControl.LogEvent += OnPneumaticSlideLog;
            pneumaticSlideControl.RefreshRequested += OnPneumaticSlideRefresh;
        }

        private void OnOutputPinClicked(object sender, (string DeviceName, string PinName) e)
        {
            // Optional: Additional logging or processing
            statusBarText.Text = $"Toggled {e.DeviceName} pin: {e.PinName}";
        }
        private void HandleInitializationError(Exception ex)
        {
            statusBarText.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Existing event handlers remain the same
        private void OnPneumaticSlideLog(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                statusBarText.Text = message;
            });
        }

        private void OnPneumaticSlideRefresh(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                statusBarText.Text = "Pneumatic slides refreshed";
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe from events
            if (pneumaticSlideControl != null)
            {
                pneumaticSlideControl.LogEvent -= OnPneumaticSlideLog;
                pneumaticSlideControl.RefreshRequested -= OnPneumaticSlideRefresh;
            }

            // Disconnect and dispose of device manager
            deviceManager?.DisconnectAll();
            deviceManager?.Dispose();

            base.OnClosing(e);
        }
    }
}