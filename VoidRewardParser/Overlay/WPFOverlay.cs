using Overlay.NET.Common;
using Overlay.NET.Wpf;
using Process.NET.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VoidRewardParser.Entities;
using OverlayWindow = Overlay.NET.Wpf.OverlayWindow;

namespace VoidRewardParser.Overlay
{
    [RegisterPlugin("VoidRewardParserOverlayPlugin", "Jacob Kemple, Ricardo Ribeiro", "WPFOverlay", "1.0",
        "A ducat and platinum overlay for reward screens")]
    public class WPFOverlay : WpfOverlayPlugin
    {
        public ISettings<OverlaySettings> Settings { get; } = new SerializableSettings<OverlaySettings>();
        public bool Initialized { get; set; } = false;

        private const string TypefaceName = "Verdana";

        // Used to limit update rates via timestamps
        // This way we can avoid thread issues with wanting to delay updates
        private readonly TickEngine _tickEngine = new TickEngine();

        private bool _isDisposed;
        private List<DisplayPrime> displayPrimes = new List<DisplayPrime>();

        public override void Enable()
        {
            Console.WriteLine("Overlay: Enable");
            IsEnabled = true;
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable()
        {
            Console.WriteLine("Overlay: Disable");
            IsEnabled = false;
            _tickEngine.IsTicking = false;

            displayPrimes = new List<DisplayPrime>();
            DrawPrimesText();

            OverlayWindow.Hide();

            base.Disable();
        }

        public override void Initialize(IWindow targetWindow)
        {
            // Set target window by calling the base method
            base.Initialize(targetWindow);
            Initialized = true;

            Console.WriteLine("Overlay: Initialize");

            OverlayWindow = new OverlayWindow(targetWindow);

            // For demo, show how to use settings
            var current = Settings.Current;
            var type = GetType();

            current.UpdateRate = 1000 / 60;
            current.Author = GetAuthor(type);
            current.Description = GetDescription(type);
            current.Identifier = GetIdentifier(type);
            current.Name = GetName(type);
            current.Version = GetVersion(type);

            // File is made from above info
            Settings.Save();
            Settings.Load();

            // Set up update interval and register events for the tick engine.
            _tickEngine.Interval = Settings.Current.UpdateRate.Milliseconds();
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
            Console.WriteLine("Overlay: Dispose");
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
            Settings.Save();

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
            int drawStartX = 70;
            int drawStartY = 30;

            double height = 0, width = 0;
            List<KeyValuePair<int, FormattedText>> _text = new List<KeyValuePair<int, FormattedText>>();

            for (int i = 0; i < displayPrimes.Count; i++)
            {
                DisplayPrime p = displayPrimes[i];

                string text = p.Prime.Name + "\t\t" + p.Prime.Ducats + " Ducats";
                if (p.PlatinumPrice != "...")
                    text += "\t\t" + p.PlatinumPrice + " Plat";

                // Draw a formatted text string into the DrawingContext.
                FormattedText Ftext = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                        new Typeface(TypefaceName), 13, Brushes.OrangeRed);

                height = Ftext.Height;

                if (Ftext.Width > width)
                    width = Ftext.Width;

                _text.Add(new KeyValuePair<int, FormattedText>(i, Ftext));
            }

            //Draw a rectangle bellow the text for easier reading
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                                    new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 2),
                                    new Rect(drawStartX - 10, drawStartY - 10, width + 20, (height * displayPrimes.Count) + 60)
                                  );

            foreach (KeyValuePair<int, FormattedText> keyValuePair in _text)
            {
                context.DrawText(keyValuePair.Value, new Point(drawStartX, (drawStartY + 25 * keyValuePair.Key)));
            }
        }
    }
}