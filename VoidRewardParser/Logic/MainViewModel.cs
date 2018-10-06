using Overlay.NET;
using Prism.Commands;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
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
        private OverlayPlugin _overlay;
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

        public bool FetchPlatinum { get; set; }

        public bool MoveToTop { get; set; }

        public event EventHandler MissionComplete;

        public MainViewModel()
        {
            _parseTimer = new DispatcherTimer
            {
                //Check screen every 1 second
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _parseTimer.Tick += _parseTimer_Tick;
            _parseTimer.Start();

            LoadCommand = new DelegateCommand(LoadData);

            spelling = new SpellCheck();

            RenderOverlay = true;

            backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var _wpfoverlay = (WPFOverlay)_overlay;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                _wpfoverlay.Enable();
            });

            while (!worker.CancellationPending)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    _overlay.Update();
                });
            }

#if DEBUG
            Console.WriteLine("Worker Exiting");
#endif
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                _wpfoverlay.Disable();
            });

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
            WarframeNotDetected = false;
            WarframeNotFocus = false;

            bool SkipNotFocus = bool.Parse(ConfigurationManager.AppSettings["SkipIfNotFocus"]);

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
#if DEBUG
                    Console.Error.WriteLine("Error: " + ex.Message);
#endif
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
                        hiddenPrimes.Count < PrimeItems.Count)
                    {
#if DEBUG
                        Console.WriteLine("Mission Success");
#endif
                        _lastMissionComplete = DateTime.MinValue;
                    }

                    if (text.ToLower().Contains(LocalizationManager.SelectAReward.ToLower()) && hiddenPrimes.Count < PrimeItems.Count)
                    {
#if DEBUG
                        Console.WriteLine("Select a Reward");
#endif
                        OnMissionComplete();

                        if (RenderOverlay)
                        {
                            StartRenderOverlayPrimes();

                            if (!backgroundWorker.IsBusy)
                                backgroundWorker.RunWorkerAsync();
                        }
                    }
                    else if (RenderOverlay && backgroundWorker.IsBusy)
                    {
                        backgroundWorker.CancelAsync();
                    }

                    await Task.WhenAll(fetchPlatpriceTasks);
                }
            }

            _parseTimer.Start();
        }

        private bool StartRenderOverlayPrimes()
        {
            if (_overlay == null)
            {
                _overlay = new WPFOverlay();
            }

            if (_processSharp == null)
            {
                _processSharp = new ProcessSharp(Warframe.GetProcess(), MemoryType.Remote);
            }

            var process = Warframe.GetProcess();

            if (process != null)
            {
                var _wpfoverlay = (WPFOverlay)_overlay;

                if (!_wpfoverlay.Initialized)
                    _wpfoverlay.Initialize(_processSharp.WindowFactory.MainWindow);

                _wpfoverlay.UpdatePrimesData(displayPrimes);
                return true;
            }

            return false;
        }

        private bool IsOnFocus()
        {
            System.Diagnostics.Process warframeProcess = Warframe.GetProcess();
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

            //Dirty Dirty lazy fixes
            correction.Replace("silva a aegis", "silva & aegis");

            return correction;
        }

        private async Task FetchPlatPriceTask(DisplayPrime displayPrime)
        {
            if (FetchPlatinum)
            {
                string name = displayPrime.Prime.Name;

                long? minSell = await PlatinumPrices.GetPrimePlatSellOrders(name);

                if (minSell.HasValue)
                {
                    displayPrime.PlatinumPrice = minSell.ToString();
                }
                else
                {
                    displayPrime.PlatinumPrice = "?";
                }
            }
        }

        private async Task FetchDucatPriceTask(DisplayPrime displayPrime)
        {
            if (string.IsNullOrWhiteSpace(displayPrime.DucatValue) || displayPrime.DucatValue == "0" || displayPrime.DucatValue == "?" || displayPrime.DucatValue == "...")
            {
                string name = displayPrime.Prime.Name;
                int? ducat = await DucatPrices.GetPrimePartDucats(name);

                if (ducat.HasValue)
                {
                    displayPrime.DucatValue = ((int)ducat).ToString();
                }
                else
                {
                    displayPrime.DucatValue = "?";
                }
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
            if (RenderOverlay && backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }

            var _wpfoverlay = (WPFOverlay)_overlay;

            if (_wpfoverlay != null)
            {
                _wpfoverlay.Dispose();
            }
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