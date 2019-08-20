using Overlay.NET;
using Prism.Commands;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VoidRewardParser.Entities;
using VoidRewardParser.Overlay;

namespace VoidRewardParser.Logic
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer parseTimer;
        private ObservableCollection<DisplayPrime> _primeItems = new ObservableCollection<DisplayPrime>();
        private List<DisplayPrime> detectedPrimePartsList = new List<DisplayPrime>();
        private bool _warframeNotDetected;
        private bool _warframeNotFocus;
        private bool showAllPrimes;
        private DateTime lastMissionCompleteTime = DateTime.MinValue;
        private SpellCheck spelling;
        private OverlayPlugin overlay;
        private ProcessSharp WarframeProcess;
        private BackgroundWorker backgroundWorker;

        public DelegateCommand LoadCommand { get; set; }

        public ObservableCollection<DisplayPrime> AllPrimePartsList
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
                    foreach (var primeItem in AllPrimePartsList)
                    {
                        primeItem.Visible = true;
                    }
                }
                OnNotifyPropertyChanged();
            }
        }

        public bool RenderOverlay { get; set; }

        public bool FetchPlatinum { get; set; }

        public bool SkipNotFocus { get; set; }

        public bool MoveToTop { get; set; }

        public event EventHandler MissionComplete;

        public MainViewModel()
        {
            parseTimer = new DispatcherTimer
            {
                //Check screen every 1 second
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            parseTimer.Tick += ParseTimerTick;
            parseTimer.Start();

            LoadCommand = new DelegateCommand(LoadSavedPrimeData);

            spelling = new SpellCheck();

            backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += new DoWorkEventHandler(DoWorkBackground);
        }

        private void DoWorkBackground(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            WPFOverlay PrimePartOverlay = (WPFOverlay)overlay;

            Application.Current.Dispatcher.Invoke(delegate
            {
                PrimePartOverlay.Enable();
            });

            while (!worker.CancellationPending)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    overlay.Update();
                });
            }

#if DEBUG
            Console.WriteLine("Worker Exiting");
#endif
            Application.Current.Dispatcher.Invoke(delegate
            {
                PrimePartOverlay.Disable();
            });

            return;
        }

        private async void LoadSavedPrimeData()
        {
            PrimeData primeData = await PrimeData.GetInstance().ConfigureAwait(false);
            foreach (var primeItem in primeData.Primes)
            {
                AllPrimePartsList.Add(new DisplayPrime() { Data = primeData.GetDataForItem(primeItem), Prime = primeItem });
            }
        }

        private bool VisibleFilter(object item)
        {
            var prime = item as DisplayPrime;
            return prime.Visible;
        }

        private async void ParseTimerTick(object sender, object e)
        {
            List<DisplayPrime> notDetectedPrimePartsList = new List<DisplayPrime>();
            List<Task> fetchTasks = new List<Task>();
            string text = string.Empty;

            parseTimer.Stop();

            WarframeNotDetected = false;
            WarframeNotFocus = false;

            if (!Warframe.WarframeIsRunning())
            {
                WarframeNotDetected = true;
                WarframeProcess = null;
                parseTimer.Start();
                return;
            }
            if (SkipNotFocus && !IsOnFocus())
            {
                WarframeNotFocus = true;
                parseTimer.Start();
                return;
            }

            try
            {
                text = await ScreenCapture.ParseTextAsync();
                text = text.ToLower(new System.Globalization.CultureInfo("en-US"));

                //Only use spellcheck if using English
                if (LocalizationManager.Language.ToLower(new System.Globalization.CultureInfo("en-US")) == "english")
                {
                    text = await Task.Run(() => SpellCheckOCR(text));
                }

                detectedPrimePartsList.Clear();

                foreach (var part in AllPrimePartsList)
                {
                    if (text.IndexOf(LocalizationManager.Localize(part.Prime.Name), StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        part.Visible = true;
                        detectedPrimePartsList.Add(part);
                        fetchTasks.Add(FetchPlatTask(part));
                        fetchTasks.Add(FetchDucatTask(part));
                    }
                    else
                    {
                        notDetectedPrimePartsList.Add(part);
                    }
                }

                if (!ShowAllPrimes)
                {
                    if (notDetectedPrimePartsList.Count < AllPrimePartsList.Count)
                    {
                        //Only hide if we see at least one prime (let the old list persist until we need to refresh)
                        foreach (var part in notDetectedPrimePartsList) { part.Visible = false; }
                    }
                }

                if (text.Contains(LocalizationManager.MissionSuccess.ToLower(new System.Globalization.CultureInfo("en-US"))) &&
                    lastMissionCompleteTime + TimeSpan.FromMinutes(1.0) > DateTime.Now &&
                    notDetectedPrimePartsList.Count < AllPrimePartsList.Count)
                {
#if DEBUG
                    Console.WriteLine("Mission Success");
#endif
                    OnMissionComplete();
                }

                if (text.Contains(LocalizationManager.SelectAReward.ToLower(new System.Globalization.CultureInfo("en-US"))) &&
                    notDetectedPrimePartsList.Count < AllPrimePartsList.Count)
                {
#if DEBUG
                    Console.WriteLine("Select a Reward");
#endif

                    if (RenderOverlay && !backgroundWorker.IsBusy)
                    {
                        StartRenderOverlayPrimes();
                        backgroundWorker.RunWorkerAsync();
                    }
                }
                else if (backgroundWorker.IsBusy)
                {
                    backgroundWorker.CancelAsync();
                }

                await Task.WhenAll(fetchTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine("Error: " + ex.Message);
#endif
            }
            finally
            {
                parseTimer.Start();
            }
        }

        private void StartRenderOverlayPrimes()
        {
            if (overlay == null)
            {
                overlay = new WPFOverlay();
            }

            if (WarframeProcess == null)
            {
                WarframeProcess = new ProcessSharp(Warframe.GetProcess(), MemoryType.Remote);
            }

            if (WarframeProcess != null)
            {
                WPFOverlay wpfoverlay = (WPFOverlay)overlay;

                if (!wpfoverlay.Initialized)
                    wpfoverlay.Initialize(WarframeProcess.WindowFactory.MainWindow);

                wpfoverlay.UpdatePrimesData(detectedPrimePartsList);
            }
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
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder correction = new StringBuilder();
            foreach (string item in text.Split(' '))
            {
                correction.Append(" " + spelling.Correct(item));
            }

            //Lazy fixes
            correction.Replace("silva a aegis", "silva & aegis");
            correction.Replace("fissure a rewards", "fissure rewards");

            return correction.ToString();
        }

        private async Task FetchPlatTask(DisplayPrime displayPrime)
        {
            if (FetchPlatinum)
            {
                string name = displayPrime.Prime.Name;

                long? minSell = await PlatinumPrices.GetPrimePlatSellOrders(name).ConfigureAwait(false);

                if (minSell.HasValue)
                {
                    displayPrime.PlatinumPrice = minSell.Value.ToString();
                }
                else
                {
                    displayPrime.PlatinumPrice = "?";
                }
            }
        }

        private async Task FetchDucatTask(DisplayPrime displayPrime)
        {
            if (string.IsNullOrWhiteSpace(displayPrime.DucatValue) || displayPrime.DucatValue == "0" || displayPrime.DucatValue == "?" || displayPrime.DucatValue == "...")
            {
                string name = displayPrime.Prime.Name;
                int? ducat = await DucatPrices.GetPrimePartDucats(name).ConfigureAwait(false);

                if (ducat.HasValue)
                {
                    displayPrime.DucatValue = ducat.Value.ToString();
                }
                else
                {
                    displayPrime.DucatValue = "?";
                }
            }
        }

        private void OnMissionComplete()
        {
            if (lastMissionCompleteTime + TimeSpan.FromSeconds(30) < DateTime.Now)
            {
                //Only raise this event at most once every 30 seconds
                MissionComplete?.Invoke(this, EventArgs.Empty);
                lastMissionCompleteTime = DateTime.Now;
            }
        }

        public async void Close()
        {
            if (RenderOverlay && backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }

            WPFOverlay wpfoverlay = (WPFOverlay)overlay;

            if (wpfoverlay != null)
            {
                wpfoverlay.Dispose();
            }
            (await PrimeData.GetInstance().ConfigureAwait(false)).SaveToFile();
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