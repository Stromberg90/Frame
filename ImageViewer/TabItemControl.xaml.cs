using Dragablz;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace Frame {
    public partial class TabItemControl : IDisposable {
        public List<string> Paths;
        internal int Index;

        public TabItemControl() {
            InitializeComponent();
            
            Paths = new List<string>();

            MouseDoubleClick += mouseDoubleClick;
            PreviewKeyDown += previewKeyDown;
        }

        private void previewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            Debug.WriteLine("KeyDown");
            if(e.Key == Key.Right) {
                if(Keyboard.IsKeyDown(Key.LeftCtrl)) {
                    var parent = (TabablzControl)Parent;
                    if(parent == null) {
                        return;
                    }

                    var headers = parent.GetOrderedHeaders().ToList();
                    int index = 0;
                    foreach(var header in headers) {
                        if(header.Content == this) {
                            break;
                        }
                        index++;
                    }
                    if(index < headers.Count - 1) {
                        parent.SelectedIndex = parent.Items.IndexOf(headers[++index].Content);
                    }
                }
                else {
                    Index = (Index + 1) % Paths.Count;
                    switchImage(Index);
                }
            }
            else if(e.Key == Key.Left) {
                if(Keyboard.IsKeyDown(Key.LeftCtrl)) {
                    var parent = (TabablzControl)Parent;
                    if(parent == null) {
                        return;
                    }

                    var headers = parent.GetOrderedHeaders().ToList();
                    int index = 0;
                    foreach(var header in headers) {
                        if(header.Content == this) {
                            break;
                        }
                        index++;
                    }
                    if(index > 0) {
                        parent.SelectedIndex = parent.Items.IndexOf(headers[--index].Content);
                    }
                }
                else {
                    if(Index == 0) {
                        Index = Paths.Count;
                    }
                    Index--;
                    switchImage(Index);
                }
            }
            else if(e.Key == Key.W) {
                if(!Keyboard.IsKeyDown(Key.LeftCtrl)) {
                    return;
                }
                if(!(Parent is TabablzControl)) {
                    return;
                }
                // Do the same as in add tab, where I send the parent and tab into a static functions, so I can unsubscribe from certain events.
                if(Keyboard.IsKeyDown(Key.LeftAlt)) {
                    var parent = (TabablzControl)Parent;
                    foreach(TabItemControl item in parent.Items) {
                        item.Dispose();
                    }
                    parent.Items.Clear();
                }
                else {
                    var parent = (TabablzControl)Parent;
                    Dispose();
                    parent.Items.Remove(this);
                }
            }
            else if(e.Key == Key.F) {
                var image_height = ImagePresenter.ImageArea.ActualHeight;
                var image_width = ImagePresenter.ImageArea.ActualWidth;

                var grid_height = ImagePresenter.ScrollViewer.ActualHeight;
                var grid_width = ImagePresenter.ScrollViewer.ActualWidth;

                if(image_height > grid_height || image_width > grid_width) {
                    var zoom_by_height = (grid_height / image_height) * 100;
                    var zoom_by_width = (grid_width / image_width) * 100;
                    if(zoom_by_height < zoom_by_width) {
                        ImagePresenter.Zoom = zoom_by_height;
                    }
                    else {
                        ImagePresenter.Zoom = zoom_by_width;
                    }
                }
                else {
                    ImagePresenter.Zoom = 100;
                }
            }
        }

        internal void selectionChanged(object sender, SelectionChangedEventArgs e) {
            if(e.AddedItems.Count > 0) {
                if(((TabItemControl)e.AddedItems[0]) == this) {
                    Debug.WriteLine("Setting Focus to " + Header);
                    ImagePresenter.Focus();
                    Keyboard.Focus(ImagePresenter);
                }
            }
        }

        private void switchImage(int index) {
            var filepath = Paths[index];
            var ext = Path.GetExtension(filepath);
            if(ext == ".gif") {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    ImagePresenter.ImageArea.loadAimatedGif(filepath);
                    Header = Path.GetFileName(filepath);
                }, DispatcherPriority.Normal);
            }
            else {
                using(var image = new MagickImage(filepath)) {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        ImagePresenter.ImageArea.loadStillImage(filepath);
                        Header = Path.GetFileName(filepath);
                    }, DispatcherPriority.Normal);
                }
            }

            var image_height = ImagePresenter.ImageArea.Height;
            var grid_height = ImagePresenter.ScrollViewer.ActualHeight;
            var image_width = ImagePresenter.ImageArea.Width;
            var grid_width = ImagePresenter.ScrollViewer.ActualWidth;
            if(image_height > grid_height || image_width > grid_width) {
                var zoom_by_height = (grid_height / image_height) * 100;
                var zoom_by_width = (grid_width / image_width) * 100;
                if(zoom_by_height < zoom_by_width) {
                    ImagePresenter.Zoom = zoom_by_height;
                }
                else {
                    ImagePresenter.Zoom = zoom_by_width;
                }
            }
            else {
                ImagePresenter.Zoom = 100;
            }
        }

        void mouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if(!(Parent is TabablzControl)) {
                return;
            }
            var parent = (TabablzControl)Parent;
            using(var file_dialog = new OpenFileDialog { Multiselect = true, AddExtension = true, Filter = MainWindow.FilterString }) {
                file_dialog.ShowDialog();

                if(file_dialog.FileNames.Length > 0) {
                    ThreadStart add_tabs_thread = () => {
                        foreach(var filepath in file_dialog.FileNames) {
                            StaticMethods.addTab(parent, filepath);
                        }
                    };
                    new Thread(add_tabs_thread).Start();
                }
            }
        }

        public void Dispose() {
            if (Parent is TabablzControl)
            {
                //((TabablzControl)Parent).AfterSelectionChanged -= selectionChanged;
            }
            PreviewMouseDoubleClick -= mouseDoubleClick;
            PreviewKeyDown -= previewKeyDown;
            if(ImagePresenter != null) {
                ImagePresenter.Dispose();
                ImagePresenter = null;
            }
        }
    }
}