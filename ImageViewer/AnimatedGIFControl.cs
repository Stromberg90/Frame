/****************************** Module Header ******************************\
Module Name:	AnimatedGIFControl.cs
Project:	    CSWPFAnimatedGIF
Copyright (c) Microsoft Corporation.

The CSWPFAnimatedGIF demonstrates how to implement 
an animated GIF image in WPF.

This source is subject to the Microsoft Public License.
See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
All other rights reserved.

THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Frame {
    public class AnimatedGIFControl : System.Windows.Controls.Image, IDisposable {
        bool IsAnimating;
        private Bitmap _bitmap; // Local bitmap member to cache image resource
        private BitmapSource _bitmapSource;
        public delegate void FrameUpdatedEventHandler();

        /// <summary>
        /// Delete local bitmap resource
        /// Reference: http://msdn.microsoft.com/en-us/library/dd183539(VS.85).aspx
        /// </summary>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Override the OnInitialized method
        /// </summary>
        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            this.Unloaded += AnimatedGIFControl_Unloaded;
        }

        /// <summary>
        /// Load the embedded image for the Image.Source
        /// </summary>
        public void LoadGif(string path, GifMode gifMode) {
            IsAnimating = gifMode == GifMode.Animated;
            _bitmap = new System.Drawing.Bitmap(path);
            Width = _bitmap.Width;
            Height = _bitmap.Height;

            _bitmapSource = GetBitmapSource();
            Source = _bitmapSource;
        }

        /// <summary>
        /// Close the FileStream to unlock the GIF file
        /// </summary>
        private void AnimatedGIFControl_Unloaded(object sender, RoutedEventArgs e) {
            StopAnimate();
        }

        /// <summary>
        /// Start animation
        /// </summary>
        public void StartAnimate() {
            IsAnimating = true;
            ImageAnimator.Animate(_bitmap, OnFrameChanged);
        }

        /// <summary>
        /// Stop animation
        /// </summary>
        public void StopAnimate() {
            IsAnimating = false;
            ImageAnimator.StopAnimate(_bitmap, OnFrameChanged);
        }

        /// <summary>
        /// Event handler for the frame changed
        /// </summary>
        private void OnFrameChanged(object sender, EventArgs e) {
            if (IsAnimating) {
                Dispatcher.BeginInvoke(DispatcherPriority.Render,
                                       new FrameUpdatedEventHandler(FrameUpdatedCallback));
            }
        }

        private void FrameUpdatedCallback() {
            ImageAnimator.UpdateFrames();

            if (_bitmapSource != null) {
                _bitmapSource.Freeze();
            }

            // Convert the bitmap to BitmapSource that can be display in WPF Visual Tree
            _bitmapSource = GetBitmapSource();
            Source = _bitmapSource;
            InvalidateVisual();
        }

        private BitmapSource GetBitmapSource() {
            IntPtr handle = IntPtr.Zero;

            try {
                handle = _bitmap.GetHbitmap();
                _bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally {
                if (handle != IntPtr.Zero) {
                    DeleteObject(handle);
                }
            }

            return _bitmapSource;
        }

        public void Dispose() {
            _bitmap.Dispose();
        }
    }
}
