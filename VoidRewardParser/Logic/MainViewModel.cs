using Overlay.NET;
using Prism.Commands;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using VoidRewardParser.Entities;
using VoidRewardParser.Overlay;

namespace VoidRewardParser.Logic
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _parseTimer;
        private ObservableCollection<DisplayPrime> _primeItems = new ObservableCollection<DisplayPrime>();
        private List<DisplayPrime> displayPrimes = new List<DisplayPrime>();
        private bool _warframeNotDetected;
        private bool _warframeNotFocus;
        private bool showAllPrimes;
        private DateTime _lastMissionComplete;
        private SpellCheck spelling;
        private OverlayPlugin _wpfoverlay;
        private ProcessSharp _processSharp;
        private BackgroundWorker backgroundWorker;

        public DelegateCommand LoadCommand { get; set; }

        public ObservableCollection<DisplayPrime> PrimeItems
        {
            get
            {
                return _primeItems;
            }

            set
            {
                if (_primeItems == value) return;
                _primeItems = value;
                OnNotifyPropertyChanged();
            }
        }

        public bool WarframeNotDetected
        {
            get
            {
                return _warframeNotDetected;
            }
            set
            {
                if (_warframeNotDetected == value) return;
                _warframeNotDetected = value;
                OnNotifyPropertyChanged();
            }
        }

        public bool WarframeNotFocus
        {
            get
            {
                return _warframeNotFocus;
            }
            set
            {
                if (_warframeNotFocus == value) return;
                _warframeNotFocus = value;
                OnNotifyPropertyChanged();
            }
        }

        public bool ShowAllPrimes
        {
            get
            {
                return showAllPrimes;
            }
            set
            {
                if (showAllPrimes == value) return;
                showAllPrimes = value;
                if (showAllPrimes)
                {
                    foreach (var primeItem in PrimeItems)
                    {
                        primeItem.Visible = true;
                    }
                }
                OnNotifyPropertyChanged();
            }
        }

        public bool RenderOverlay { get; set; }

        public event EventHandler MissionComplete;

        public MainViewModel()
        {
            _parseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _parseTimer.Tick += _parseTimer_Tick;
            _parseTimer.Start();

            LoadCommand = new DelegateCommand(LoadData);

            spelling = new SpellCheck();

            RenderOverlay = true;

            if (RenderOverlay)
            {
                backgroundWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };
                backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            _wpfoverlay.Enable();

            while (!worker.CancellationPending)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    _wpfoverlay.Update();
                });
            }

            Console.WriteLine("Worker exit");

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                _wpfoverlay.Disable();
            });

            e.Cancel = true;
            return;
        }

        private async void LoadData()
        {
            var primeData = await PrimeData.GetInstance();
            foreach (var primeItem in primeData.Primes)
            {
                PrimeItems.Add(new DisplayPrime() { Data = primeData.GetDataForItem(primeItem), Prime = primeItem });
            }
        }

        private bool VisibleFilter(object item)
        {
            var prime = item as DisplayPrime;
            return prime.Visible;
        }

        private async void _parseTimer_Tick(object sender, object e)
        {
            _parseTimer.Stop();

            bool SkipNotFocus = Boolean.Parse(ConfigurationManager.AppSettings["SkipIfNotFocus"]);

            if (!Warframe.WarframeIsRunning())
            {
                WarframeNotDetected = true;
                _processSharp = null;
            }
            else if (SkipNotFocus && !IsOnFocus())
            {
                WarframeNotFocus = true;
            }
            else
            {
                var hiddenPrimes = new List<DisplayPrime>();
                List<Task> fetchPlatpriceTasks = new List<Task>();
                string text = string.Empty;

                try
                {
                    text = await ScreenCapture.ParseTextAsync();
                    text = await Task.Run(() => SpellCheckOCR(text));

                    displayPrimes.Clear();

                    foreach (var p in PrimeItems)
                    {
                        if (text.IndexOf(LocalizationManager.Localize(p.Prime.Name), StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            p.Visible = true;
                            displayPrimes.Add(p);
                            fetchPlatpriceTasks.Add(FetchPlatPriceTask(p));
                            fetchPlatpriceTasks.Add(FetchDucatPriceTask(p));
                        }
                        else
                        {
                            hiddenPrimes.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error: " + ex.Message);
                    _parseTimer.Start();
                    return;
                }
                finally
                {
                    if (!ShowAllPrimes)
                    {
                        if (hiddenPrimes.Count < PrimeItems.Count)
                        {
                            //Only hide if we see at least one prime (let the old list persist until we need to refresh)
                            foreach (var p in hiddenPrimes) { p.Visible = false; }
                        }
                    }

                    if (text.ToLower().Contains(LocalizationManager.MissionSuccess.ToLower()) && _lastMissionComplete.AddMinutes(1) > DateTime.Now &&
                        PrimeItems.Count - hiddenPrimes.Count == 1)
                    {
                        Console.WriteLine("Mission Success");
                        //Auto-record the selected reward if we detect a prime on the mission complete screen
                        _lastMissionComplete = DateTime.MinValue;
                        await Task.Run(() => PrimeItems.FirstOrDefault(p => p.Visible)?.AddCommand?.Execute());
                    }

                    if (text.ToLower().Contains(LocalizationManager.SelectAReward.ToLower()) && hiddenPrimes.Count < PrimeItems.Count)
                    {
                        Console.WriteLine("Select a Reward");
                        OnMissionComplete();

                        if (RenderOverlay)
                        {
                            if (_processSharp == null || _wpfoverlay == null)
                                StartRenderOverlayPrimes();

                            if (!backgroundWorker.IsBusy)
                                backgroundWorker.RunWorkerAsync();
                        }
                    }
                    else if (RenderOverlay && backgroundWorker.IsBusy)
                    {
                        backgroundWorker.CancelAsync();
                    }

                    WarframeNotDetected = false;
                    WarframeNotFocus = false;

                    await Task.WhenAll(fetchPlatpriceTasks);
                }
            }

            _parseTimer.Start();
        }

        private bool StartRenderOverlayPrimes()
        {
            if (_wpfoverlay == null)
            {
                _wpfoverlay = new WPFOverlay();
            }

            if (_processSharp == null)
            {
                _processSharp = new ProcessSharp(ScreenCapture.GetProcess(), MemoryType.Remote);
            }

            var process = ScreenCapture.GetProcess();

            if (process != null)
            {
                var d3DOverlay = (WPFOverlay)_wpfoverlay;
                _wpfoverlay.Initialize(_processSharp.WindowFactory.MainWindow);
                d3DOverlay.Update(displayPrimes);
                return true;
            }

            return false;
        }

        private bool IsOnFocus()
        {
            System.Diagnostics.Process warframeProcess = ScreenCapture.GetProcess();
            if (warframeProcess == null)
            {
                return false;       // Warframe not running
            }

            IntPtr activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            GetWindowThreadProcessId(activatedHandle, out int activeProcId);

            return activeProcId == warframeProcess.Id;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private string SpellCheckOCR(string text)
        {
            if (spelling == null) return text;

            string correction = "";
            foreach (string item in text.Split(' '))
            {
                correction += " " + spelling.Correct(item);
            }

            return correction;
        }

        private async Task FetchPlatPriceTask(DisplayPrime displayPrime)
        {
            string name = displayPrime.Prime.Name;

            var minSell = await PlatinumPrices.GetPrimePlatSellOrders(name);

            if (minSell.HasValue)
            {
                displayPrime.PlatinumPrice = minSell.ToString();
            }
            else
            {
                displayPrime.PlatinumPrice = "?";
            }
        }

        private async Task FetchDucatPriceTask(DisplayPrime displayPrime)
        {
            string name = displayPrime.Prime.Name;

            var ducat = await DucatPrices.GetPrimePlatDucats(name);

            if (ducat.HasValue)
            {
                displayPrime.Prime.Ducats = (int)ducat;
                displayPrime.DucatValue = ((int)ducat).ToString();
            }
            else
            {
                displayPrime.DucatValue = "?";
            }
        }

        private void OnMissionComplete()
        {
            if (_lastMissionComplete + TimeSpan.FromSeconds(30) < DateTime.Now)
            {
                //Only raise this event at most once every 30 seconds
                MissionComplete?.Invoke(this, EventArgs.Empty);
                _lastMissionComplete = DateTime.Now;
            }
        }

        public async void Close()
        {
            (await PrimeData.GetInstance()).SaveToFile();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnNotifyPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}