using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ShadUI;

namespace Bloomdo.Client.UI.Controls;

/// <summary>
/// A panel that reveals Edit + Delete actions when swiped left.
/// Properly distinguishes horizontal swipe from vertical scroll.
/// Works on Android (no Cursor, no layout mutations during attach).
/// </summary>
public class SwipeRevealPanel : ContentControl
{
    private const double SingleActionWidth = 70;
    private const double SnapThreshold = 0.35;
    private const double DirectionLockAngle = 1.2;

    private double _effectiveWidth = SingleActionWidth;
    private TranslateTransform _contentTranslate = new();
    private Border? _actionBorder;
    private Point _startPoint;
    private bool _isTracking;
    private bool _directionLocked;
    private bool _isHorizontal;
    private bool _isOpen;
    private bool _wrapped;

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, ICommand?>(nameof(ActionCommand));

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, object?>(nameof(ActionCommandParameter));

    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, ICommand?>(nameof(EditCommand));

    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly StyledProperty<object?> EditCommandParameterProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, object?>(nameof(EditCommandParameter));

    public object? EditCommandParameter
    {
        get => GetValue(EditCommandParameterProperty);
        set => SetValue(EditCommandParameterProperty, value);
    }

    public static readonly StyledProperty<bool> IsActionEnabledProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, bool>(nameof(IsActionEnabled), true);

    public bool IsActionEnabled
    {
        get => GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    public static readonly StyledProperty<ICommand?> TapCommandProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, ICommand?>(nameof(TapCommand));

    public ICommand? TapCommand
    {
        get => GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public static readonly StyledProperty<object?> TapCommandParameterProperty =
        AvaloniaProperty.Register<SwipeRevealPanel, object?>(nameof(TapCommandParameter));

    public object? TapCommandParameter
    {
        get => GetValue(TapCommandParameterProperty);
        set => SetValue(TapCommandParameterProperty, value);
    }

    private static SwipeRevealPanel? _currentlyOpenPanel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty && !_wrapped)
        {
            var ctrl = change.NewValue as Control;
            if (ctrl is not null)
            {
                _wrapped = true;
                Dispatcher.UIThread.Post(() => WrapContent(ctrl));
            }
        }
    }

    private void WrapContent(Control child)
    {
        var hasEdit = EditCommand is not null;
        _effectiveWidth = hasEdit ? SingleActionWidth * 2 : SingleActionWidth;

        _contentTranslate = new TranslateTransform
        {
            Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.XProperty,
                    Duration = TimeSpan.FromMilliseconds(220),
                    Easing = new CubicEaseOut()
                }
            }
        };
        child.RenderTransform = _contentTranslate;

        Control actionContent;

        if (hasEdit)
        {
            var editIcon = new PathIcon
            {
                Data = Icons.Marker,
                Foreground = Brushes.White,
                Width = 18, Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var editLabel = new TextBlock
            {
                Text = "Edit",
                Foreground = Brushes.White,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var editStack = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { editIcon, editLabel }
            };
            var editButton = new Button
            {
                Width = SingleActionWidth,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = editStack,
                Background = new SolidColorBrush(Color.Parse("#42A5F5")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(0)
            };
            editButton.Click += OnEditButtonClick;

            var deleteIcon = new PathIcon
            {
                Data = Icons.Cross,
                Foreground = Brushes.White,
                Width = 18, Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var deleteLabel = new TextBlock
            {
                Text = "Delete",
                Foreground = Brushes.White,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var deleteStack = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { deleteIcon, deleteLabel }
            };
            var deleteButton = new Button
            {
                Width = SingleActionWidth,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = deleteStack,
                Background = new SolidColorBrush(Color.Parse("#E53935")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(0)
            };
            deleteButton.Click += OnDeleteButtonClick;

            actionContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Children = { editButton, deleteButton }
            };
        }
        else
        {
            var deleteIcon = new PathIcon
            {
                Data = Icons.Cross,
                Foreground = Brushes.White,
                Width = 20, Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var deleteLabel = new TextBlock
            {
                Text = "Delete",
                Foreground = Brushes.White,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var deleteStack = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { deleteIcon, deleteLabel }
            };
            var deleteButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = deleteStack,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };
            deleteButton.Click += OnDeleteButtonClick;
            actionContent = deleteButton;
        }

        _actionBorder = new Border
        {
            Width = _effectiveWidth,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = hasEdit ? Brushes.Transparent : new SolidColorBrush(Color.Parse("#E53935")),
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Opacity = 0,
            Child = actionContent
        };

        Content = null;
        var root = new Panel { ClipToBounds = true };
        root.Children.Add(_actionBorder);
        root.Children.Add(child);
        Content = root;
    }

    private void OnEditButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!IsActionEnabled) return;
        var cmd = EditCommand;
        var param = EditCommandParameter;
        if (cmd?.CanExecute(param) == true)
            cmd.Execute(param);
        AnimateClose();
    }

    private void OnDeleteButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!IsActionEnabled) return;
        var cmd = ActionCommand;
        var param = ActionCommandParameter;
        if (cmd?.CanExecute(param) == true)
            cmd.Execute(param);
        AnimateClose();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        // Accept left mouse button OR touch/pen (for Android)
        if (!point.Properties.IsLeftButtonPressed && point.Pointer.Type == PointerType.Mouse) return;

        if (_currentlyOpenPanel is not null && !ReferenceEquals(_currentlyOpenPanel, this))
            _currentlyOpenPanel.AnimateClose();

        _startPoint = e.GetPosition(this);
        _isTracking = true;
        _directionLocked = false;
        _isHorizontal = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isTracking) return;

        var current = e.GetPosition(this);
        var dx = current.X - _startPoint.X;
        var dy = current.Y - _startPoint.Y;

        if (!_directionLocked)
        {
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 10) return;

            _directionLocked = true;
            _isHorizontal = Math.Abs(dx) > Math.Abs(dy) * DirectionLockAngle;

            if (!_isHorizontal || !IsActionEnabled)
            {
                _isTracking = false;
                return;
            }

            e.Pointer.Capture(this);
        }

        if (!_isHorizontal) return;

        var newOffset = Math.Clamp(_isOpen ? -_effectiveWidth + dx : dx, -_effectiveWidth, 0);
        _contentTranslate.X = newOffset;

        if (_actionBorder is not null)
            _actionBorder.Opacity = Math.Abs(newOffset) / _effectiveWidth;

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isTracking || !_isHorizontal)
        {
            // Tap detection: pointer was pressed and released without a swipe
            if (_isTracking && !_directionLocked)
            {
                if (_isOpen)
                {
                    AnimateClose();
                }
                else
                {
                    var cmd = TapCommand;
                    var param = TapCommandParameter;
                    if (cmd?.CanExecute(param) == true)
                        cmd.Execute(param);
                }
            }
            _isTracking = false;
            return;
        }

        _isTracking = false;
        e.Pointer.Capture(null);

        if (Math.Abs(_contentTranslate.X) > _effectiveWidth * SnapThreshold)
            AnimateOpen();
        else
            AnimateClose();

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_isTracking && _isHorizontal)
        {
            if (Math.Abs(_contentTranslate.X) > _effectiveWidth * SnapThreshold)
                AnimateOpen();
            else
                AnimateClose();
        }

        _isTracking = false;
    }

    private void AnimateOpen()
    {
        _contentTranslate.X = -_effectiveWidth;

        _actionBorder?
            .Animate(OpacityProperty)
            .From(_actionBorder.Opacity)
            .To(1.0)
            .WithDuration(TimeSpan.FromMilliseconds(220))
            .WithEasing(new CubicEaseOut())
            .Start();

        _isOpen = true;
        _currentlyOpenPanel = this;
    }

    public void AnimateClose()
    {
        _contentTranslate.X = 0.0;

        _actionBorder?
            .Animate(OpacityProperty)
            .From(_actionBorder.Opacity)
            .To(0.0)
            .WithDuration(TimeSpan.FromMilliseconds(220))
            .WithEasing(new CubicEaseOut())
            .Start();

        _isOpen = false;
        if (ReferenceEquals(_currentlyOpenPanel, this))
            _currentlyOpenPanel = null;
    }
}

