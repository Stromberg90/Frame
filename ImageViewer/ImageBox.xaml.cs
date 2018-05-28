using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Frame.Annotations;

namespace Frame
{
  public sealed partial class ImageBox : INotifyPropertyChanged
  {
    Point? lastCenterPositionOnTarget;
    Point? lastMousePositionOnTarget;
    Point? lastDragPoint;
    double zoom = 100;

    public double Zoom
    {
      get => zoom;
      set
      {
        OnZoomChanged(this, new RoutedPropertyChangedEventArgs<double>(zoom, value));
        zoom = value;
        OnPropertyChanged();
      }
    }

    public ImageBox()
    {
      InitializeComponent();

      ScrollViewer.MouseMove += OnMouseMove;

      ScrollViewer.ScrollChanged            += OnScrollViewerScrollChanged;
      ScrollViewer.MouseLeftButtonUp        += OnMouseLeftButtonUp;
      ScrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
      ScrollViewer.PreviewMouseWheel        += OnPreviewMouseWheel;

      ScrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
      if (!lastDragPoint.HasValue)
      {
        return;
      }

      var posNow = e.GetPosition(ScrollViewer);

      var dX = posNow.X - lastDragPoint.Value.X;
      var dY = posNow.Y - lastDragPoint.Value.Y;

      lastDragPoint = posNow;

      ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset - dX);
      ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - dY);
    }

    void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var mousePos = e.GetPosition(ScrollViewer);
      if (!(mousePos.X <= ScrollViewer.ViewportWidth) || !(mousePos.Y <
                                                           ScrollViewer.ViewportHeight))
      {
        return;
      }

      ScrollViewer.Cursor = Cursors.SizeAll;
      lastDragPoint       = mousePos;
      Mouse.Capture(ScrollViewer);
    }

    void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      lastMousePositionOnTarget = Mouse.GetPosition(Grid);

      if (e.Delta > 0)
      {
        Zoom += 10 * (Zoom / 100);
      }

      if (e.Delta < 0)
      {
        Zoom -= 10 * (Zoom / 100);
      }

      e.Handled = true;
    }

    void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      ScrollViewer.Cursor = Cursors.Arrow;
      ScrollViewer.ReleaseMouseCapture();
      lastDragPoint = null;
    }

    void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      ScaleTransform.ScaleX = e.NewValue / 100;
      ScaleTransform.ScaleY = e.NewValue / 100;

      var centerOfViewport = new Point(ScrollViewer.ViewportWidth / 2,
                                       ScrollViewer.ViewportHeight / 2);
      lastCenterPositionOnTarget = ScrollViewer.TranslatePoint(centerOfViewport, Grid);
    }

    void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (!(Math.Abs(e.ExtentHeightChange) > 0.001) && !(Math.Abs(e.ExtentWidthChange) > 0.001))
      {
        return;
      }

      Point? targetBefore = null;
      Point? targetNow    = null;

      if (!lastMousePositionOnTarget.HasValue)
      {
        if (lastCenterPositionOnTarget.HasValue)
        {
          var centerOfViewport = new Point(ScrollViewer.ViewportWidth / 2,
                                           ScrollViewer.ViewportHeight / 2);
          var centerOfTargetNow =
            ScrollViewer.TranslatePoint(centerOfViewport, Grid);

          targetBefore = lastCenterPositionOnTarget;
          targetNow    = centerOfTargetNow;
        }
      }
      else
      {
        targetBefore = lastMousePositionOnTarget;
        targetNow    = Mouse.GetPosition(Grid);

        lastMousePositionOnTarget = null;
      }

      if (!targetBefore.HasValue)
      {
        return;
      }

      var dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
      var dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

      var multiplicatorX = e.ExtentWidth / Grid.Width;
      var multiplicatorY = e.ExtentHeight / Grid.Height;

      var newOffsetX = ScrollViewer.HorizontalOffset -
                       dXInTargetPixels * multiplicatorX;
      var newOffsetY = ScrollViewer.VerticalOffset -
                       dYInTargetPixels * multiplicatorY;

      if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
      {
        return;
      }

      ScrollViewer.ScrollToHorizontalOffset(newOffsetX);
      ScrollViewer.ScrollToVerticalOffset(newOffsetY);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}