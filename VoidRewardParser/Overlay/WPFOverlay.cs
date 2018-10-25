using Overlay.NET.Common;
using Overlay.NET.Wpf;
using Process.NET.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using VoidRewardParser.Entities;
using OverlayWindow = Overlay.NET.Wpf.OverlayWindow;

namespace VoidRewardParser.Overlay
{
    public class WPFOverlay : WpfOverlayPlugin
    {
        public ISettings<OverlaySettings> Settings { get; } = new SerializableSettings<OverlaySettings>();
        public bool Initialized { get; set; } = false;

        private const string TypefaceName = "Courier New";

        // Used to limit update rates via timestamps
        // This way we can avoid thread issues with wanting to delay updates
        private readonly TickEngine _tickEngine = new TickEngine();

        private bool _isDisposed;
        private List<DisplayPrime> displayPrimes = new List<DisplayPrime>();

        private static SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
        private static Brush penBrush = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        private static Pen pen = new Pen(penBrush, 2);

        private static int drawStartX = 70;
        private static int drawStartY = 30;

        public override void Enable()
        {
#if DEBUG
            Console.WriteLine("Overlay: Enable");
#endif
            IsEnabled = true;
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable()
        {
#if DEBUG
            Console.WriteLine("Overlay: Disable");
#endif
            IsEnabled = false;
            _tickEngine.IsTicking = false;

            displayPrimes = new List<DisplayPrime>();
            DrawPrimesText();

            if (OverlayWindow != null)
                OverlayWindow.Hide();

            base.Disable();
        }

        public override void Initialize(IWindow targetWindow)
        {
            // Set target window by calling the base method
            base.Initialize(targetWindow);
            Initialized = true;
#if DEBUG
            Console.WriteLine("Overlay: Initialize");
#endif
            OverlayWindow = new OverlayWindow(targetWindow);

            // Set up update interval and register events for the tick engine.
            _tickEngine.Interval = (1000 / 30).Milliseconds();
            _tickEngine.PreTick += OnPreTick;
            _tickEngine.Tick += OnTick;

            OverlayWindow.Draw += OnDraw;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            // This will only be true if the target window is active
            // (or very recently has been, depends on your update rate)
            if (OverlayWindow.IsVisible)
            {
                DrawPrimesText();
                OverlayWindow.Update();
            }
        }

        private void OnPreTick(object sender, EventArgs eventArgs)
        {
            var activated = TargetWindow.IsActivated;
            var visible = OverlayWindow.IsVisible;

            try
            {
                // Ensure window is shown or hidden correctly prior to updating
                if (!activated && visible)
                {
                    OverlayWindow.Hide();
                }
                else if (activated && !visible)
                {
                    OverlayWindow.Show();
                }
            }
            catch
            {
            }
        }

        public override void Update() => _tickEngine.Pulse();

        public void UpdatePrimesData(List<DisplayPrime> list)
        {
            this.displayPrimes = list;
        }

        // Clear objects
        public override void Dispose()
        {
#if DEBUG
            Console.WriteLine("Overlay: Dispose");
#endif
            if (_isDisposed)
            {
                return;
            }

            if (IsEnabled)
            {
                Disable();
            }

            OverlayWindow?.Hide();
            OverlayWindow?.Close();
            OverlayWindow = null;
            _tickEngine.Stop();

            base.Dispose();
            _isDisposed = true;
        }

        ~WPFOverlay()
        {
            Dispose();
        }

        private void DrawPrimesText()
        {
            this.OverlayWindow?.InvalidateVisual();
        }

        public void OnDraw(object sender, DrawingContext context)
        {
            double height = 0;
            double width = 0;
            List<KeyValuePair<int, FormattedText>> _text = new List<KeyValuePair<int, FormattedText>>();

            for (int i = 0; i < displayPrimes.Count; i++)
            {
                DisplayPrime p = displayPrimes[i];

                StringBuilder text = new StringBuilder(p.Prime.Name);
                text.Append(p.Prime.Name.Length < 20 ? "\t\t" : "\t");
                text.Append(p.Prime.Ducats + " Ducats");

                if (p.PlatinumPrice != "?")
                    text.Append("\t" + p.PlatinumPrice + " Plat");

                // Draw a formatted text string into the DrawingContext.
                FormattedText Ftext = new FormattedText(text.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                        new Typeface(TypefaceName), 13, Brushes.OrangeRed);

                height = Ftext.Height;

                if (width < Ftext.Width)
                    width = Ftext.Width;

                _text.Add(new KeyValuePair<int, FormattedText>(i, Ftext));
            }

            //Draw a rectangle bellow the text for easier reading
            context.DrawRectangle(brush, pen, new Rect(drawStartX - 10, drawStartY - 10, width + 20, ((height + 5) * displayPrimes.Count) + displayPrimes.Count * 7));

            foreach (KeyValuePair<int, FormattedText> keyValuePair in _text)
            {
                context.DrawText(keyValuePair.Value, new Point(drawStartX, (drawStartY + 25 * keyValuePair.Key)));
            }
        }
    }
}