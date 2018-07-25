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
    [RegisterPlugin("VoidRewardParserOverlayPlugin", "Jacob Kemple, Ricardo Ribeiro", "DirectxOverlay", "0.1",
        "A ducat and platinum overlay for reward screens")]
    public class WPFOverlay : WpfOverlayPlugin
    {
        public ISettings<OverlaySettings> Settings { get; } = new SerializableSettings<OverlaySettings>();

        // Used to limit update rates via timestamps
        // This way we can avoid thread issues with wanting to delay updates
        private readonly TickEngine _tickEngine = new TickEngine();

        private bool _isDisposed;

        private List<DisplayPrime> displayPrimes = new List<DisplayPrime>();

        public override void Enable()
        {
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable()
        {
            _tickEngine.IsTicking = false;
            displayPrimes = new List<DisplayPrime>();
            DrawPrimesText();

            base.Disable();
        }

        public override void Initialize(IWindow targetWindow)
        {
            // Set target window by calling the base method
            base.Initialize(targetWindow);

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

        public override void Update() => _tickEngine.Pulse();

        public void Update(List<DisplayPrime> list)
        {
            this.displayPrimes = list;
            Update();
        }

        // Clear objects
        public override void Dispose()
        {
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
            this.OverlayWindow.InvalidateVisual();
        }

        public void OnDraw(object sender, DrawingContext context)
        {
            //displayPrimes
            for (int i = 0; i < displayPrimes.Count; i++)
            {
                DisplayPrime p = displayPrimes[i];
                String text = p.Prime.Name + "     " + p.Prime.Ducats + " Ducats    " + p.PlatinumPrice + " Plat";

                // Draw a formatted text string into the DrawingContext.
                context.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                        new Typeface("Verdana"), 12, Brushes.OrangeRed), new Point(70, (30 + 30 * i)));
            }
        }
    }
}