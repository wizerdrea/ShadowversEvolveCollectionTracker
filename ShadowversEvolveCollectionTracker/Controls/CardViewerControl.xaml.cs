using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShadowverseEvolveCardTracker.ViewModels;

namespace ShadowverseEvolveCardTracker.Controls
{
    public partial class CardViewerControl : UserControl
    {
        private bool _isPanning;
        private Point _panStartPoint;
        private double _startTranslateX;
        private double _startTranslateY;

        public CardViewerControl()
        {
            InitializeComponent();
            // Do NOT set DataContext here. The parent view binds this control's DataContext
            // to its CardViewer view model (see AllCardsTabView / ChecklistTabView).
            // Setting DataContext here prevented that binding and caused the favorite star
            // to reflect the wrong (internal) view model.
        }

        // Expose the view model if callers need it; it may be null until the parent binds DataContext.
        public CardViewerViewModel? ViewModel => DataContext as CardViewerViewModel;

        private void ViewerImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Border viewport && viewport.Tag is Image img && img.RenderTransform is TransformGroup tg)
            {
                var scale = (ScaleTransform)tg.Children[0];
                var translate = (TranslateTransform)tg.Children[1];

                const double zoomFactorPerNotch = 1.1;
                var factor = e.Delta > 0 ? zoomFactorPerNotch : 1.0 / zoomFactorPerNotch;

                double oldScale = scale.ScaleX;
                double newScale = Math.Clamp(oldScale * factor, 0.25, 8.0);
                if (Math.Abs(newScale - oldScale) < 0.0001)
                    return;

                var pos = e.GetPosition(img);
                double relativeX = (pos.X - translate.X) / oldScale;
                double relativeY = (pos.Y - translate.Y) / oldScale;

                scale.ScaleX = newScale;
                scale.ScaleY = newScale;

                translate.X = pos.X - relativeX * newScale;
                translate.Y = pos.Y - relativeY * newScale;

                KeepImageInBounds(viewport, img, scale, translate);
                e.Handled = true;
            }
        }

        private void ViewerImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border viewport) return;
            if (!(viewport.Tag is Image img)) return;

            _isPanning = true;
            _panStartPoint = e.GetPosition(img);
            var tg = (TransformGroup)img.RenderTransform;
            var translate = (TranslateTransform)tg.Children[1];
            _startTranslateX = translate.X;
            _startTranslateY = translate.Y;

            viewport.CaptureMouse();
            viewport.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void ViewerImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            if (sender is not Border viewport) return;
            if (!(viewport.Tag is Image img)) return;

            var current = e.GetPosition(img);
            var delta = current - _panStartPoint;

            var tg = (TransformGroup)img.RenderTransform;
            var scale = (ScaleTransform)tg.Children[0];
            var translate = (TranslateTransform)tg.Children[1];

            translate.X = _startTranslateX + delta.X;
            translate.Y = _startTranslateY + delta.Y;

            KeepImageInBounds(viewport, img, scale, translate);
            e.Handled = true;
        }

        private void ViewerImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border viewport) return;
            if (!_isPanning) return;

            _isPanning = false;
            viewport.ReleaseMouseCapture();
            viewport.Cursor = Cursors.Arrow;
            e.Handled = true;
        }

        private void ViewerImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetZoomAndPan();
            e.Handled = true;
        }

        private void Reset_Click(object? sender, RoutedEventArgs e)
        {
            ResetZoomAndPan();
        }

        private void ResetZoomAndPan()
        {
            try
            {
                if (ViewerViewport?.Tag is Image img && img.RenderTransform is TransformGroup tg)
                {
                    var scale = (ScaleTransform)tg.Children[0];
                    var translate = (TranslateTransform)tg.Children[1];

                    scale.ScaleX = scale.ScaleY = 1.0;
                    translate.X = translate.Y = 0.0;
                }
            }
            catch
            {
                // ignore UI reset failures
            }
        }

        private void KeepImageInBounds(Border viewport, Image img, ScaleTransform scale, TranslateTransform translate)
        {
            if (viewport == null || img == null || scale == null || translate == null)
                return;

            double vpW = Math.Max(1.0, img.ActualWidth);
            double vpH = Math.Max(1.0, img.ActualHeight);

            double imgWidth = img.ActualWidth;
            double imgHeight = img.ActualHeight;

            if (imgWidth <= 0 || imgHeight <= 0)
                return;

            double contentW = imgWidth * scale.ScaleX;
            double contentH = imgHeight * scale.ScaleY;

            if (contentW <= vpW)
            {
                translate.X = (vpW - contentW) / 2.0;
            }
            else
            {
                double minX = vpW - contentW;
                double maxX = 0;
                translate.X = Math.Clamp(translate.X, minX, maxX);
            }

            if (contentH <= vpH)
            {
                translate.Y = (vpH - contentH) / 2.0;
            }
            else
            {
                double minY = vpH - contentH;
                double maxY = 0;
                translate.Y = Math.Clamp(translate.Y, minY, maxY);
            }
        }
    }
}