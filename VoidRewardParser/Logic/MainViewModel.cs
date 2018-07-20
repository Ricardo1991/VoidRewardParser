using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using VoidRewardParser.Entities;

namespace VoidRewardParser.Logic
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _parseTimer;
        private ObservableCollection<DisplayPrime> _primeItems = new ObservableCollection<DisplayPrime>();
        private bool _warframeNotDetected;
        private bool _warframeNotFocus;
        private bool showAllPrimes;
        private DateTime _lastMissionComplete;
        private SpellCheck spelling;

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

            if (!Warframe.WarframeIsRunning())
            {
                WarframeNotDetected = true;
            }
            else if (!IsOnFocus())
            {
                WarframeNotFocus = true;
            }
            else
            {
                var text = await ScreenCapture.ParseTextAsync();

                text = await Task.Run(() => SpellCheckOCR(text));

                var hiddenPrimes = new List<DisplayPrime>();
                List<Task> fetchPlatpriceTasks = new List<Task>();
                foreach (var p in PrimeItems)
                {
                    if (text.IndexOf(LocalizationManager.Localize(p.Prime.Name), StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        p.Visible = true;
                        fetchPlatpriceTasks.Add(FetchPlatPriceTask(p));
                        fetchPlatpriceTasks.Add(FetchDucatPriceTask(p));
                    }
                    else
                    {
                        hiddenPrimes.Add(p);
                    }
                }

                if (!ShowAllPrimes)
                {
                    if (hiddenPrimes.Count < PrimeItems.Count)
                    {
                        //Only hide if we see at least one prime (let the old list persist until we need to refresh)
                        foreach (var p in hiddenPrimes) { p.Visible = false; }
                    }
                }

                if (text.Contains(LocalizationManager.MissionSuccess) && _lastMissionComplete.AddMinutes(1) > DateTime.Now &&
                    PrimeItems.Count - hiddenPrimes.Count == 1)
                {
                    //Auto-record the selected reward if we detect a prime on the mission complete screen
                    _lastMissionComplete = DateTime.MinValue;
                    await Task.Run(() => PrimeItems.FirstOrDefault(p => p.Visible)?.AddCommand?.Execute());
                }

                if (text.Contains(LocalizationManager.SelectAReward) && PrimeItems.Count - hiddenPrimes.Count > 0)
                {
                    OnMissionComplete();
                }
                WarframeNotDetected = false;
                WarframeNotFocus = false;

                await Task.WhenAll(fetchPlatpriceTasks);
            }
            _parseTimer.Start();
        }

        private bool IsOnFocus()
        {
            Process warframeProcess = ScreenCapture.GetProcess();
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