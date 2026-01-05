using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShadowversEvolveCardTracker
{
    public partial class MainWindow
    {
        private bool _isPanning;
        private Point _panStartPoint;
        private double _startTranslateX;
        private double _startTranslateY;

        // Add this field to your MainWindow class (if not already present)
        private Viewbox? ChecklistPreviewViewbox;

        // Mouse wheel zoom handler (works for Border viewport or Viewbox sender)
        private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Border-based viewport (AllCards or Checklist)
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

                // Use image coordinates (not the Border) so calculations refer to the image layout slot
                var pos = e.GetPosition(img);

                double relativeX = (pos.X - translate.X) / oldScale;
                double relativeY = (pos.Y - translate.Y) / oldScale;

                scale.ScaleX = newScale;
                scale.ScaleY = newScale;

                translate.X = pos.X - relativeX * newScale;
                translate.Y = pos.Y - relativeY * newScale;

                // Keep image within viewport bounds after zoom
                KeepImageInBounds(viewport, img, scale, translate);

                e.Handled = true;
                return;
            }

            // Viewbox-based preview (legacy; still support if present)
            if (sender is Viewbox vb && vb.RenderTransform is ScaleTransform st)
            {
                const double zoomUp = 1.1;
                const double zoomDown = 1.0 / zoomUp;
                var factor = e.Delta > 0 ? zoomUp : zoomDown;

                double newScale = Math.Clamp(st.ScaleX * factor, 0.25, 8.0);
                st.ScaleX = newScale;
                st.ScaleY = newScale;

                e.Handled = true;
            }
        }

        private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border viewport) return;
            if (!(viewport.Tag is Image img)) return;

            _isPanning = true;

            // Use image coordinates so panning delta is in the same coordinate space as translate transforms
            _panStartPoint = e.GetPosition(img);
            var tg = (TransformGroup)img.RenderTransform;
            var translate = (TranslateTransform)tg.Children[1];
            _startTranslateX = translate.X;
            _startTranslateY = translate.Y;

            viewport.CaptureMouse();
            viewport.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            if (sender is not Border viewport) return;
            if (!(viewport.Tag is Image img)) return;

            // Use image coordinates so delta corresponds to translate units
            var current = e.GetPosition(img);
            var delta = current - _panStartPoint;

            var tg = (TransformGroup)img.RenderTransform;
            var scale = (ScaleTransform)tg.Children[0];
            var translate = (TranslateTransform)tg.Children[1];

            translate.X = _startTranslateX + delta.X;
            translate.Y = _startTranslateY + delta.Y;

            // Keep image within viewport bounds while panning
            KeepImageInBounds(viewport, img, scale, translate);

            e.Handled = true;
        }

        private void PreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border viewport) return;
            if (!_isPanning) return;

            _isPanning = false;
            viewport.ReleaseMouseCapture();
            viewport.Cursor = Cursors.Arrow;
            e.Handled = true;
        }

        // Right-click resets zoom/position
        private void PreviewImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Border-based reset
            if (sender is Border viewport && viewport.Tag is Image img && img.RenderTransform is TransformGroup tg)
            {
                var scale = (ScaleTransform)tg.Children[0];
                var translate = (TranslateTransform)tg.Children[1];

                scale.ScaleX = scale.ScaleY = 1.0;
                translate.X = translate.Y = 0.0;

                e.Handled = true;
                return;
            }

            // Viewbox-based reset (if right-clicked directly on viewbox)
            if (sender is Viewbox vb && vb.RenderTransform is ScaleTransform st)
            {
                st.ScaleX = st.ScaleY = 1.0;
                e.Handled = true;
            }
        }

        // Reset button for All Cards preview
        private void AllCardsReset_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (AllCardsPreviewViewport?.Tag is Image img && img.RenderTransform is TransformGroup tg)
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

        // Reset button for Checklist preview
        private void ChecklistReset_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Reset Checklist Border/Image transforms if present
                if (ChecklistPreviewViewport?.Tag is Image img && img.RenderTransform is TransformGroup tg)
                {
                    var scale = (ScaleTransform)tg.Children[0];
                    var translate = (TranslateTransform)tg.Children[1];

                    scale.ScaleX = scale.ScaleY = 1.0;
                    translate.X = translate.Y = 0.0;
                }

                // Also reset viewbox-based fallback
                if (ChecklistPreviewViewbox?.RenderTransform is ScaleTransform st)
                {
                    st.ScaleX = st.ScaleY = 1.0;
                }
            }
            catch
            {
                // ignore UI reset failures
            }
        }

        // Ensure image remains visible inside viewport bounds (centers if smaller than viewport)
        private void KeepImageInBounds(Border viewport, Image img, ScaleTransform scale, TranslateTransform translate)
        {
            if (viewport == null || img == null || scale == null || translate == null)
                return;

            // Use the Image control's layout size as the available area for translation.
            // The RenderTransform operates inside the Image control, so centering/clamping should use img.ActualWidth/Height.
            double vpW = Math.Max(1.0, img.ActualWidth);
            double vpH = Math.Max(1.0, img.ActualHeight);

            double imgWidth = img.ActualWidth;
            double imgHeight = img.ActualHeight;

            if (imgWidth <= 0 || imgHeight <= 0)
                return;

            double contentW = imgWidth * scale.ScaleX;
            double contentH = imgHeight * scale.ScaleY;

            // If content smaller than available area, center it
            if (contentW <= vpW)
            {
                translate.X = (vpW - contentW) / 2.0;
            }
            else
            {
                double minX = vpW - contentW; // most negative allowed
                double maxX = 0;              // most positive allowed
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