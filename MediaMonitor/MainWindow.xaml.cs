using MediaMonitor.Models;
using MediaMonitor.Services;
using MediaMonitor.ViewModels;
using MediaMonitor.Views;
using System;
using System.Windows;


namespace MediaMonitor
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly SettingsService _settingsService = new();
        private AppSettings _settings = new();
        private bool _settingsLoaded;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            DataContext = _vm;

            _vm.OpenAddDialog += OnOpenAddDialog;
            _vm.OpenEditDialog += OnOpenEditDialog;
            _vm.ConfirmDelete += OnConfirmDelete;

            StateChanged += MainWindow_StateChanged;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        // --- Кастомный титульный бар ---
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            bool isMaximized = WindowState == WindowState.Maximized;

            // "Развернуть" превращается в "Свернуть в окно" и наоборот
            MaximizeIcon.Text = isMaximized ? "\uE923" : "\uE922";

            // Убираем скругления/отступ/тень в развёрнутом состоянии,
            // чтобы окно занимало весь экран без "рамки"
            RootBorder.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(10);
            RootBorder.Margin = isMaximized ? new Thickness(0) : new Thickness(10);
            RootShadow.Opacity = isMaximized ? 0 : 0.5;
            TitleBarBorder.CornerRadius = isMaximized
                ? new CornerRadius(0)
                : new CornerRadius(10, 10, 0, 0);
            SidebarBorder.CornerRadius = isMaximized
                ? new CornerRadius(0)
                : new CornerRadius(0, 0, 0, 10);
        }

        // --- Настройки: позиция/размер окна + загрузка данных ---
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _settings = await _settingsService.LoadAsync();

            var workArea = SystemParameters.WorkArea;
            bool fitsOnScreen =
                _settings.Left + _settings.Width > workArea.Left &&
                _settings.Left < workArea.Right &&
                _settings.Top + _settings.Height > workArea.Top &&
                _settings.Top < workArea.Bottom;

            if (fitsOnScreen && _settings.Width > 0 && _settings.Height > 0)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = _settings.Left;
                Top = _settings.Top;
                Width = _settings.Width;
                Height = _settings.Height;
            }

            if (_settings.IsMaximized)
                WindowState = WindowState.Maximized;

            _settingsLoaded = true;

            // Загружаем коллекцию, затем — если что-то есть "в процессе",
            // предлагаем продолжить просмотр (каждый запуск — случайный элемент).
            await _vm.InitializeAsync();
            ShowContinueWatchingPromptIfAny();
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_settingsLoaded) return;

            bool maximized = WindowState == WindowState.Maximized;
            var bounds = maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);

            _settings.Left = bounds.Left;
            _settings.Top = bounds.Top;
            _settings.Width = bounds.Width;
            _settings.Height = bounds.Height;
            _settings.IsMaximized = maximized;

            // Гарантируем сохранение коллекции при закрытии (доп. подстраховка
            // к автосохранению, которое уже срабатывает при каждом изменении).
            await _vm.SaveNowAsync();
            await _settingsService.SaveAsync(_settings);
        }

        private void ShowContinueWatchingPromptIfAny()
        {
            var candidate = _vm.GetRandomInProgressItem();
            if (candidate == null) return;

            var toast = new ContinueWatchingWindow(candidate) { Owner = this };
            toast.ContinueRequested += item => OnOpenEditDialog(item);
            toast.Show();
        }

        private void OnOpenAddDialog()
        {
            var viewModel = new AddEditViewModel();
            var dialog = new AddEditWindow { DataContext = viewModel, Owner = this };

            viewModel.OnSave += item => { _vm.AddOrUpdateItem(item); dialog.Close(); };
            viewModel.OnCancel += () => dialog.Close();

            dialog.ShowDialog();
        }

        private void OnOpenEditDialog(MediaItem item)
        {
            var viewModel = new AddEditViewModel(item);
            var dialog = new AddEditWindow { DataContext = viewModel, Owner = this };

            viewModel.OnSave += updated => { _vm.AddOrUpdateItem(updated); dialog.Close(); };
            viewModel.OnCancel += () => dialog.Close();

            dialog.ShowDialog();
        }

        private bool OnConfirmDelete(MediaItem item)
        {
            var result = MessageBox.Show(
                $"Удалить «{item.Title}»?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
    }
}
