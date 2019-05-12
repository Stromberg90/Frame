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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;

namespace Frame {
    // Nearest neighbour filtering
    public class AnimatedGIFControl : System.Windows.Controls.Image, IDisposable {
        public Bitmap _bitmap; // Local bitmap member to cache image resource
        BitmapSource _bitmapSource;
        delegate void FrameUpdatedEventHandler();

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        internal void loadStillImage(string filepath) {
            stopAnimate();

            if(_bitmap != null) {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _bitmapSource = null;

            _bitmap = new Bitmap(filepath);
            Width = _bitmap.Width;
            Height = _bitmap.Height;

            updateSource();
        }

        public void startAnimate() {
            ImageAnimator.Animate(_bitmap, onFrameChanged);
        }

        public void stopAnimate() {
            ImageAnimator.StopAnimate(_bitmap, onFrameChanged);
        }

        void onFrameChanged(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(DispatcherPriority.Render,
                                   new FrameUpdatedEventHandler(frameUpdatedCallback));
        }

        void frameUpdatedCallback() {
            ImageAnimator.UpdateFrames();

            if(_bitmapSource != null) {
                _bitmapSource.Freeze();
            }

            if(_bitmap == null) {
                Dispose();
            }
            else {
                _bitmapSource = GetBitmapSource();
                Source = _bitmapSource;
                InvalidateVisual();
            }
        }

        public BitmapSource GetBitmapSource() {
            IntPtr handle = IntPtr.Zero;

            try {
                handle = _bitmap.GetHbitmap();
                _bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally {
                if(handle != IntPtr.Zero) {
                    DeleteObject(handle);
                }
            }

            return _bitmapSource;
        }

        public void loadAimatedGif(string filepath) {
            stopAnimate();

            if(_bitmap != null) {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _bitmapSource = null;

            _bitmap = new Bitmap(filepath);
            Width = _bitmap.Width;
            Height = _bitmap.Height;

            _bitmapSource = GetBitmapSource();
            Source = _bitmapSource;
            startAnimate();
        }

        public void Dispose() {
            stopAnimate();
            if(_bitmap != null) {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _bitmapSource = null;
            Source = null;
        }

        public void updateSource() {
            if(_bitmapSource != null) {
                _bitmapSource.Freeze();
            }

            _bitmapSource = GetBitmapSource();
            Source = _bitmapSource;
            InvalidateVisual();
        }
    }
}
