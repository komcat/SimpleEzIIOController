using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using FASTECH;

namespace EzIIOLib
{
    public class IOConfiguration
    {
        public Metadata Metadata { get; set; }
        public EziioDevice[] Eziio { get; set; }
    }

    public class Metadata
    {
        public string Version { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class EziioDevice
    {
        public int DeviceId { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public int InputCount { get; set; }
        public int OutputCount { get; set; }
        public IOConfig IOConfig { get; set; }
    }

    public class IOConfig
    {
        public List<PinConfig> Outputs { get; set; }
        public List<PinConfig> Inputs { get; set; }
    }

    public class PinConfig
    {
        public int Pin { get; set; }
        public string Name { get; set; }
    }

    public class PinStatus : INotifyPropertyChanged
    {
        private int pinNumber;
        private string name;
        private bool state;

        public int PinNumber
        {
            get => pinNumber;
            set
            {
                pinNumber = value;
                OnPropertyChanged(nameof(PinNumber));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool State
        {
            get => state;
            set
            {
                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? $"Pin {PinNumber}" : Name;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EzIIOManager : IDisposable
    {
        private const int TCP = 0;
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "IOConfig.json");

        private static readonly uint[] OUTPUT_PIN_MASKS_16 = new uint[16]
        {
            0x10000,    // Pin 0
            0x20000,    // Pin 1
            0x40000,    // Pin 2
            0x80000,    // Pin 3
            0x100000,   // Pin 4
            0x200000,   // Pin 5
            0x400000,   // Pin 6
            0x800000,   // Pin 7
            0x1000000,  // Pin 8
            0x2000000,  // Pin 9
            0x4000000,  // Pin 10
            0x8000000,  // Pin 11
            0x10000000, // Pin 12
            0x20000000, // Pin 13
            0x40000000, // Pin 14
            0x80000000  // Pin 15
        };

        private static readonly uint[] OUTPUT_PIN_MASKS_8 = new uint[8]
        {
            0x100,     // Pin 0
            0x200,     // Pin 1
            0x400,     // Pin 2
            0x800,     // Pin 3
            0x1000,    // Pin 4
            0x2000,    // Pin 5
            0x4000,    // Pin 6
            0x8000     // Pin 7
        };

        private readonly int boardId;
        private bool isConnected;
        private bool isMonitoring;
        private Thread monitorThread;
        private CancellationTokenSource cancellationTokenSource;
        private readonly int inputPinCount;
        private readonly int outputPinCount;
        private readonly uint[] currentOutputMasks;
        private EziioDevice deviceConfig;

        public ObservableCollection<PinStatus> InputPins { get; private set; }
        public ObservableCollection<PinStatus> OutputPins { get; private set; }

        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<string> Error;
        public event EventHandler<(string PinName, bool State)> InputStateChanged;
        public event EventHandler<(string PinName, bool State)> OutputStateChanged;

        public bool IsConnected => isConnected;
        public string DeviceName => deviceConfig?.Name ?? "Unknown Device";
        public string IPAddress => deviceConfig?.IP ?? "Unknown IP";

        public EzIIOManager(string deviceName)
        {
            LoadConfiguration(deviceName);

            if (deviceConfig == null)
                throw new ArgumentException($"Device '{deviceName}' not found in configuration.");

            this.boardId = deviceConfig.DeviceId;
            this.inputPinCount = deviceConfig.InputCount;
            this.outputPinCount = deviceConfig.OutputCount;

            // Select appropriate mask array based on output count
            currentOutputMasks = outputPinCount <= 8 ? OUTPUT_PIN_MASKS_8 : OUTPUT_PIN_MASKS_16;

            InitializePinCollections();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void LoadConfiguration(string deviceName)
        {
            try
            {
                string jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<IOConfiguration>(jsonContent);
                deviceConfig = config.Eziio.FirstOrDefault(d => d.Name == deviceName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading configuration: {ex.Message}");
            }
        }

        private void InitializePinCollections()
        {
            InputPins = new ObservableCollection<PinStatus>();
            OutputPins = new ObservableCollection<PinStatus>();

            // Initialize input pins
            for (int i = 0; i < inputPinCount; i++)
            {
                var pinConfig = deviceConfig.IOConfig.Inputs?.FirstOrDefault(p => p.Pin == i);
                InputPins.Add(new PinStatus
                {
                    PinNumber = i,
                    Name = pinConfig?.Name ?? string.Empty,
                    State = false
                });
            }

            // Initialize output pins
            for (int i = 0; i < outputPinCount; i++)
            {
                var pinConfig = deviceConfig.IOConfig.Outputs?.FirstOrDefault(p => p.Pin == i);
                OutputPins.Add(new PinStatus
                {
                    PinNumber = i,
                    Name = pinConfig?.Name ?? string.Empty,
                    State = false
                });
            }
        }

        public bool Connect()
        {
            try
            {
                // Parse IP address string (e.g., "192.168.0.3") into bytes
                string[] ipParts = deviceConfig.IP.Split('.');
                if (ipParts.Length != 4)
                {
                    RaiseError("Invalid IP Address format");
                    return false;
                }

                byte[] ipBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    if (!byte.TryParse(ipParts[i], out ipBytes[i]))
                    {
                        RaiseError("Invalid IP Address format");
                        return false;
                    }
                }

                IPAddress ip = new IPAddress(ipBytes);
                if (EziMOTIONPlusELib.FAS_ConnectTCP(ip, boardId))
                {
                    isConnected = true;
                    StartMonitoring();
                    ConnectionStatusChanged?.Invoke(this, true);
                    return true;
                }
                else
                {
                    RaiseError("Connection failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error connecting: {ex.Message}");
                return false;
            }
        }
        public void Disconnect()
        {
            if (isConnected)
            {
                StopMonitoring();
                EziMOTIONPlusELib.FAS_Close(boardId);
                isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        public bool SetOutput(string pinName, bool state)
        {
            var pinConfig = deviceConfig.IOConfig.Outputs.FirstOrDefault(p => p.Name == pinName);
            if (pinConfig == null)
                return false;

            return SetOutputPin(pinConfig.Pin, state);
        }

        public bool SetOutputPin(int pinNumber, bool state)
        {
            if (!isConnected || pinNumber < 0 || pinNumber >= outputPinCount)
                return false;

            uint setMask = state ? currentOutputMasks[pinNumber] : 0;
            uint clearMask = state ? 0 : currentOutputMasks[pinNumber];

            return EziMOTIONPlusELib.FAS_SetOutput(boardId, setMask, clearMask) == EziMOTIONPlusELib.FMM_OK;
        }

        private void StartMonitoring()
        {
            isMonitoring = true;
            monitorThread = new Thread(MonitorPins)
            {
                IsBackground = true
            };
            monitorThread.Start();
        }

        private void StopMonitoring()
        {
            isMonitoring = false;
            cancellationTokenSource.Cancel();
            monitorThread?.Join(1000);
        }

        private void MonitorPins()
        {
            while (isMonitoring && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Monitor Input Pins
                    uint currentInput = 0;
                    uint latch = 0;
                    if (EziMOTIONPlusELib.FAS_GetInput(boardId, ref currentInput, ref latch) == EziMOTIONPlusELib.FMM_OK)
                    {
                        UpdateInputPinStates(currentInput);
                    }

                    // Monitor Output Pins
                    uint currentOutput = 0;
                    uint status = 0;
                    if (EziMOTIONPlusELib.FAS_GetOutput(boardId, ref currentOutput, ref status) == EziMOTIONPlusELib.FMM_OK)
                    {
                        UpdateOutputPinStates(currentOutput);
                    }

                    Thread.Sleep(100); // Polling interval
                }
                catch (Exception ex)
                {
                    RaiseError($"Error monitoring pins: {ex.Message}");
                    Disconnect();
                    break;
                }
            }
        }

        private void UpdateInputPinStates(uint pinStates)
        {
            for (int i = 0; i < InputPins.Count; i++)
            {
                bool newState = (pinStates & (1u << i)) != 0;
                if (InputPins[i].State != newState)
                {
                    InputPins[i].State = newState;
                    InputStateChanged?.Invoke(this, (InputPins[i].Name, newState));
                }
            }
        }

        private void UpdateOutputPinStates(uint pinStates)
        {
            for (int i = 0; i < OutputPins.Count; i++)
            {
                bool newState = (pinStates & currentOutputMasks[i]) != 0;
                if (OutputPins[i].State != newState)
                {
                    OutputPins[i].State = newState;
                    OutputStateChanged?.Invoke(this, (OutputPins[i].Name, newState));
                }
            }
        }

        private void RaiseError(string message)
        {
            Error?.Invoke(this, message);
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            Disconnect();
            cancellationTokenSource.Dispose();
        }
    }
}