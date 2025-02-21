using EzIIOLib;
using EzIIOLibControl.Controls;
using System.Diagnostics;
using System.Windows;

namespace SimpleEzIIOController
{
    public partial class TestToggleControlWindow : Window
    {
        private MultiDeviceManager deviceManager;

        public TestToggleControlWindow()
        {
            InitializeComponent();
            InitializeDeviceManager();
        }

        private void InitializeDeviceManager()
        {
            // Initialize device manager
            deviceManager = new MultiDeviceManager();
            deviceManager.AddDevice("IOBottom");

            // Create toggle switch
            var toggleSwitch = new IOPinToggleSwitch
            {
                DeviceName = "IOBottom",
                PinName = "L_Gripper",
                PinNumber = 0,
                DeviceManager = deviceManager
            };

            // Subscribe to events
            toggleSwitch.PinStateChanged += ToggleSwitch_PinStateChanged;
            toggleSwitch.Error += ToggleSwitch_Error;

            // Add to layout
            ToggleSwichPanel.Children.Add(toggleSwitch);

            deviceManager.ConnectAll();
        }
        private void ToggleSwitch_PinStateChanged(object sender, bool newState)
        {
            Debug.WriteLine($"Pin state changed to: {newState}");
        }

        private void ToggleSwitch_Error(object sender, string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Disconnect and dispose of device manager
            deviceManager?.DisconnectAll();
            deviceManager?.Dispose();

            base.OnClosing(e);
        }
    }
}