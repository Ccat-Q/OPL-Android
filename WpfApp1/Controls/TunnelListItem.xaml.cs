using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OPL_WpfApp.Controls
{
    public partial class TunnelListItem : UserControl
    {
        private bool _updating;
        private Point _dragStart;
        private UIElement _dragRoot;
        private AdornerLayer _dragLayer;
        private DragPreviewAdorner _dragPreview;

        public int Index { get; private set; }
        public bool IsTunnelEnabled => EnabledCheckBox.IsChecked == true;

        public event EventHandler EnabledChanged;
        public event RoutedEventHandler CopyRequested;
        public event RoutedEventHandler EditRequested;
        public event RoutedEventHandler DeleteRequested;
        public event Action<int, int> MoveRequested;

        public TunnelListItem()
        {
            InitializeComponent();
        }

        public void Bind(int index, string name, string uid, string protocol, int remotePort,
            int localPort, string address, bool enabled, Brush statusBrush, string statusText)
        {
            _updating = true;
            Index = index;
            NameText.Text = name + " 隧道";
            UidText.Text = "UID  " + uid;
            ProtocolText.Text = protocol.ToUpperInvariant();
            PortText.Text = remotePort + " → " + localPort;
            AddressText.Text = address;
            EnabledCheckBox.IsChecked = enabled;
            StatusDot.Fill = statusBrush;
            StatusText.Text = statusText;
            _updating = false;
        }

        public void SetEditingEnabled(bool enabled)
        {
            EnabledCheckBox.IsEnabled = enabled;
            EditButton.IsEnabled = enabled;
            DeleteButton.IsEnabled = enabled;
            DragHandle.IsHitTestVisible = enabled;
            DragHandle.Opacity = enabled ? 1 : 0.45;
            DragHandle.Cursor = enabled ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!_updating) EnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Copy_Click(object sender, RoutedEventArgs e) => CopyRequested?.Invoke(this, e);
        private void Edit_Click(object sender, RoutedEventArgs e) => EditRequested?.Invoke(this, e);
        private void Delete_Click(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(this, e);

        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _dragStart = e.GetPosition(this);

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            Point current = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(current.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(current.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                Animate(CardScale, ScaleTransform.ScaleXProperty, 0.98);
                Animate(CardScale, ScaleTransform.ScaleYProperty, 0.98);
                Animate(this, OpacityProperty, 0.35);
                ShowDragPreview();
                try
                {
                    DragDrop.DoDragDrop(this, Index, DragDropEffects.Move);
                }
                finally
                {
                    HideDragPreview();
                    Animate(CardScale, ScaleTransform.ScaleXProperty, 1);
                    Animate(CardScale, ScaleTransform.ScaleYProperty, 1);
                    Animate(this, OpacityProperty, 1);
                }
            }
        }

        private void ShowDragPreview()
        {
            _dragRoot = Window.GetWindow(this)?.Content as UIElement;
            if (_dragRoot == null) return;
            _dragLayer = AdornerLayer.GetAdornerLayer(_dragRoot);
            if (_dragLayer == null) return;
            _dragPreview = new DragPreviewAdorner(_dragRoot, CardBorder, _dragStart);
            _dragLayer.Add(_dragPreview);
            GiveFeedback += DragHandle_GiveFeedback;
            _dragPreview.Update(Mouse.GetPosition(_dragRoot));
        }

        private void HideDragPreview()
        {
            GiveFeedback -= DragHandle_GiveFeedback;
            if (_dragLayer != null && _dragPreview != null) _dragLayer.Remove(_dragPreview);
            _dragPreview = null;
            _dragLayer = null;
            _dragRoot = null;
        }

        private void DragHandle_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (_dragPreview != null && _dragRoot != null)
                _dragPreview.Update(Mouse.GetPosition(_dragRoot));
            e.UseDefaultCursors = true;
            e.Handled = true;
        }

        private void TunnelListItem_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(int))) return;
            int sourceIndex = (int)e.Data.GetData(typeof(int));
            if (sourceIndex == Index) return;
            Animate(CardOffset, TranslateTransform.YProperty, sourceIndex < Index ? -6 : 6);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void TunnelListItem_DragLeave(object sender, DragEventArgs e)
            => Animate(CardOffset, TranslateTransform.YProperty, 0);

        private void TunnelListItem_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(int))) return;
            int sourceIndex = (int)e.Data.GetData(typeof(int));
            Animate(CardOffset, TranslateTransform.YProperty, 0);
            if (sourceIndex != Index) MoveRequested?.Invoke(sourceIndex, Index);
            e.Handled = true;
        }

        private static void Animate(IAnimatable target, DependencyProperty property, double value)
        {
            target.BeginAnimation(property, new DoubleAnimation(value, TimeSpan.FromMilliseconds(140))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private sealed class DragPreviewAdorner : Adorner
        {
            private readonly VisualBrush _brush;
            private readonly Size _size;
            private readonly Point _offset;
            private Point _position;

            public DragPreviewAdorner(UIElement adornedElement, UIElement preview, Point offset)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
                Opacity = 0.9;
                _brush = new VisualBrush(preview);
                _size = preview.RenderSize;
                _offset = offset;
            }

            public void Update(Point position)
            {
                _position = position;
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                drawingContext.DrawRoundedRectangle(_brush, null,
                    new Rect(_position.X - _offset.X, _position.Y - _offset.Y, _size.Width, _size.Height), 8, 8);
            }
        }
    }
}
