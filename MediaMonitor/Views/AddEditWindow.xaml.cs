using MediaMonitor.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Windows;


namespace MediaMonitor.Views
{
    public partial class AddEditWindow : Window
    {
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

        public AddEditWindow()
        {
            InitializeComponent();
        }

        private void PosterDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            bool hasImage = TryGetDroppedImagePath(e, out _);
            e.Effects = hasImage ? DragDropEffects.Copy : DragDropEffects.None;
            PosterDropOverlay.Visibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PosterDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            PosterDropOverlay.Visibility = Visibility.Collapsed;
        }

        private void PosterDropBorder_Drop(object sender, DragEventArgs e)
        {
            PosterDropOverlay.Visibility = Visibility.Collapsed;

            if (TryGetDroppedImagePath(e, out var path) && DataContext is AddEditViewModel vm)
            {
                vm.PosterUrl = path!;
            }
        }

        private static bool TryGetDroppedImagePath(DragEventArgs e, out string? path)
        {
            path = null;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return false;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var candidate = files?.FirstOrDefault(f =>
                AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            if (candidate == null)
                return false;

            path = candidate;
            return true;
        }
    }
}
