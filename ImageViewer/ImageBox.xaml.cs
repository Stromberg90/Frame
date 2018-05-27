using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Frame
{
  public partial class ImageBox
  {
    Point? lastCenterPositionOnTarget;
    Point? lastMousePositionOnTarget;
    Point? lastDragPoint;
    double zoom = 100;

    public double Zoom
    {
      get { return zoom; }
      set
      {
        OnZoomChanged(this, new RoutedPropertyChangedEventArgs<double>(zoom, value));
        zoom = value;
      }
    }

    public ImageBox()
    {
      InitializeComponent();

      ImageAreaScrollViewwer.MouseMove += OnMouseMove;

      ImageAreaScrollViewwer.ScrollChanged            += OnImageAreaScrollViewwerScrollChanged;
      ImageAreaScrollViewwer.MouseLeftButtonUp        += OnMouseLeftButtonUp;
      ImageAreaScrollViewwer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
      ImageAreaScrollViewwer.PreviewMouseWheel        += OnPreviewMouseWheel;

      ImageAreaScrollViewwer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
      if (lastDragPoint.HasValue)
      {
        var posNow = e.GetPosition(ImageAreaScrollViewwer);

        var dX = posNow.X - lastDragPoint.Value.X;
        var dY = posNow.Y - lastDragPoint.Value.Y;

        lastDragPoint = posNow;

        ImageAreaScrollViewwer.ScrollToHorizontalOffset(ImageAreaScrollViewwer.HorizontalOffset - dX);
        ImageAreaScrollViewwer.ScrollToVerticalOffset(ImageAreaScrollViewwer.VerticalOffset - dY);
      }
    }

    void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var mousePos = e.GetPosition(ImageAreaScrollViewwer);
      if (mousePos.X <= ImageAreaScrollViewwer.ViewportWidth && mousePos.Y <
          ImageAreaScrollViewwer.ViewportHeight)
      {
        ImageAreaScrollViewwer.Cursor = Cursors.SizeAll;
        lastDragPoint                 = mousePos;
        Mouse.Capture(ImageAreaScrollViewwer);
      }
    }

    void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      lastMousePositionOnTarget = Mouse.GetPosition(grid);

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
      ImageAreaScrollViewwer.Cursor = Cursors.Arrow;
      ImageAreaScrollViewwer.ReleaseMouseCapture();
      lastDragPoint = null;
    }

    void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      scaleTransform.ScaleX = e.NewValue / 100;
      scaleTransform.ScaleY = e.NewValue / 100;

      var centerOfViewport = new Point(ImageAreaScrollViewwer.ViewportWidth / 2,
                                       ImageAreaScrollViewwer.ViewportHeight / 2);
      lastCenterPositionOnTarget = ImageAreaScrollViewwer.TranslatePoint(centerOfViewport, grid);
    }

    void OnImageAreaScrollViewwerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
      {
        Point? targetBefore = null;
        Point? targetNow    = null;

        if (!lastMousePositionOnTarget.HasValue)
        {
          if (lastCenterPositionOnTarget.HasValue)
          {
            var centerOfViewport = new Point(ImageAreaScrollViewwer.ViewportWidth / 2,
                                             ImageAreaScrollViewwer.ViewportHeight / 2);
            Point centerOfTargetNow =
              ImageAreaScrollViewwer.TranslatePoint(centerOfViewport, grid);

            targetBefore = lastCenterPositionOnTarget;
            targetNow    = centerOfTargetNow;
          }
        }
        else
        {
          targetBefore = lastMousePositionOnTarget;
          targetNow    = Mouse.GetPosition(grid);

          lastMousePositionOnTarget = null;
        }

        if (targetBefore.HasValue)
        {
          double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
          double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

          double multiplicatorX = e.ExtentWidth / grid.Width;
          double multiplicatorY = e.ExtentHeight / grid.Height;

          double newOffsetX = ImageAreaScrollViewwer.HorizontalOffset -
                              dXInTargetPixels * multiplicatorX;
          double newOffsetY = ImageAreaScrollViewwer.VerticalOffset -
                              dYInTargetPixels * multiplicatorY;

          if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
          {
            return;
          }

          ImageAreaScrollViewwer.ScrollToHorizontalOffset(newOffsetX);
          ImageAreaScrollViewwer.ScrollToVerticalOffset(newOffsetY);
        }
      }
    }
  }
}