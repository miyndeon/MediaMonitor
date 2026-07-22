using MediaMonitor.Models;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MediaMonitor.Views
{
    public partial class ContinueWatchingWindow : Window
    {
        private const double ScreenMargin = 20;
        private static readonly TimeSpan AutoCloseAfter = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(320);

        private readonly DispatcherTimer _autoCloseTimer;
        private bool _closing;

        public MediaItem Item { get; }

        public event Action<MediaItem>? ContinueRequested;

        public ContinueWatchingWindow(MediaItem item)
        {
            InitializeComponent();
            Item = item;
            DataContext = item;

            _autoCloseTimer = new DispatcherTimer { Interval = AutoCloseAfter };
            _autoCloseTimer.Tick += (_, _) => AnimateOutAndClose();

            Loaded += OnLoaded;
            MouseEnter += (_, _) => _autoCloseTimer.Stop();
            MouseLeave += (_, _) => RestartTimer();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            double targetLeft = workArea.Right - Width - ScreenMargin;

            Top = workArea.Bottom - Height - ScreenMargin;
            Left = workArea.Right; // старт за пределами экрана справа

            var slideIn = new DoubleAnimation(workArea.Right, targetLeft, SlideDuration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(LeftProperty, slideIn);

            RestartTimer();
        }

        private void RestartTimer()
        {
            _autoCloseTimer.Stop();
            if (!_closing) _autoCloseTimer.Start();
        }

        private void AnimateOutAndClose()
        {
            if (_closing) return;
            _closing = true;
            _autoCloseTimer.Stop();

            var workArea = SystemParameters.WorkArea;
            var slideOut = new DoubleAnimation(Left, workArea.Right, TimeSpan.FromMilliseconds(250));
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));

            slideOut.Completed += (_, _) => Close();
            BeginAnimation(LeftProperty, slideOut);
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateOutAndClose();
            _closing = true;
            _autoCloseTimer.Stop();
            ContinueRequested?.Invoke(Item);
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateOutAndClose();
        }
    }
}
