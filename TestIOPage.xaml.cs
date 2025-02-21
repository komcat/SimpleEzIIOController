using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EzIIOLib;

namespace SimpleEzIIOController
{
    public partial class TestIOPage : Window
    {
        private EzIIOManager ezIIOManager;

        public TestIOPage()
        {
            InitializeComponent();
            InitAndConnect();
        }

        private void InitAndConnect()
        {
            try
            {
                // Create manager for IOBottom device
                ezIIOManager = EzIIOManager.CreateFromConfig("IOBottom");

                // Setup pin monitors
                inputPinMonitor.PinsSource = ezIIOManager.InputPins;
                outputPinMonitor.PinsSource = ezIIOManager.OutputPins;

                // Add event handler for output pin clicks
                outputPinMonitor.PinClicked += OutputPinMonitor_PinClicked;

                // Connect to device
                if (ezIIOManager.Connect())
                {
                    statusBarText.Text = "Connected to " + ezIIOManager.DeviceName;
                }
                else
                {
                    statusBarText.Text = "Failed to connect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void OutputPinMonitor_PinClicked(object sender, (string DeviceName, string PinName) e)
        {
            try
            {
                // Toggle the output pin
                bool? currentState = ezIIOManager.GetOutputState(e.PinName);

                if (currentState.HasValue)
                {
                    bool newState = !currentState.Value;

                    if (newState)
                    {
                        // Set the output pin
                        ezIIOManager.SetOutput(e.PinName);
                        statusBarText.Text = $"Set output pin {e.PinName} to ON";
                    }
                    else
                    {
                        // Clear the output pin
                        ezIIOManager.ClearOutput(e.PinName);
                        statusBarText.Text = $"Set output pin {e.PinName} to OFF";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling output pin {e.PinName}: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            if (outputPinMonitor != null)
            {
                outputPinMonitor.PinClicked -= OutputPinMonitor_PinClicked;
            }

            ezIIOManager?.Dispose();
            base.OnClosing(e);
        }
    }
}