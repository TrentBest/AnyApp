using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace TheSingularityWorkshop.AnyApp.Environments;

/// <summary>
/// Bounded WPF host proof for the environment document. Pointer handling and the canvas
/// projection are intentionally local to AnyApp until GUI #75 and Visualize #43 define
/// their reusable contracts. This is not a replacement GUI or Visualize implementation.
/// </summary>
public sealed class EnvironmentAuthoringSurface : UserControl
{
    private const double UnitsToPixels = 30;
    private readonly List<EnvironmentObject> _objects = [];
    private readonly List<EnvironmentConnection> _connections = [];
    private readonly Canvas _world = new()
    {
        Width = 1200,
        Height = 800,
        Background = new SolidColorBrush(Color.FromRgb(20, 27, 39))
    };
    private readonly StackPanel _palette = new() { Margin = new Thickness(12) };
    private readonly StackPanel _inspector = new() { Margin = new Thickness(12) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
    private readonly TextBlock _hermit = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
    private readonly TextBox _name = new() { Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBlock _identity = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBox _x = new() { Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBox _y = new() { Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBox _width = new() { Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBox _height = new() { Margin = new Thickness(0, 2, 0, 8) };
    private readonly TextBox _appearance = new() { Margin = new Thickness(0, 2, 0, 8) };
    private EnvironmentDocument _document = new();
    private string? _selectedId;
    private Border? _dragTarget;
    private Point _dragOrigin;
    private double _dragStartX;
    private double _dragStartY;
    private bool _previewMode;

    public EnvironmentAuthoringSurface()
    {
        _document = new EnvironmentDocument();
        BuildLayout();
        RefreshWorld();
        SetStatus("Build mode. Add an object from the palette, select it, and edit its properties.");
    }

    private void BuildLayout()
    {
        var root = new DockPanel();
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };
        DockPanel.SetDock(toolbar, Dock.Top);
        AddButton(toolbar, "Build", () => SetMode(false));
        AddButton(toolbar, "Environment", () => SetMode(true));
        AddButton(toolbar, "Save JSON", SaveDocument);
        AddButton(toolbar, "Load JSON", LoadDocument);
        AddButton(toolbar, "Remove selected", RemoveSelected);
        root.Children.Add(toolbar);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(235) });

        var paletteScroll = new ScrollViewer { Content = _palette, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(paletteScroll, 0);
        body.Children.Add(paletteScroll);

        var worldScroll = new ScrollViewer
        {
            Content = _world,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(4)
        };
        Grid.SetColumn(worldScroll, 1);
        body.Children.Add(worldScroll);

        var right = new StackPanel();
        var inspectorScroll = new ScrollViewer { Content = _inspector, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        right.Children.Add(new TextBlock { Text = "Properties", FontWeight = FontWeights.Bold, Margin = new Thickness(12, 12, 12, 0) });
        right.Children.Add(inspectorScroll);
        AddButton(right, "Apply properties", ApplyProperties);
        AddButton(right, "Hermit: inspect selected", InspectSelected);
        right.Children.Add(new TextBlock { Text = "Hermit presence · deterministic local placeholder", FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8, 14, 8, 2) });
        right.Children.Add(_hermit);
        Grid.SetColumn(right, 2);
        body.Children.Add(right);

        DockPanel.SetDock(body, Dock.Top);
        root.Children.Add(body);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(_status);
        Content = root;

        _palette.Children.Add(new TextBlock { Text = "Object palette", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        foreach (var kind in new[] { "floor", "platform", "cube", "ball", "hoop", "crane-placeholder" })
        {
            var captured = kind;
            AddButton(_palette, $"+ {kind}", () => AddObject(captured));
        }

        _inspector.Children.Add(new TextBlock { Text = "Environment name" });
        _inspector.Children.Add(_name);
        _inspector.Children.Add(new TextBlock { Text = "Stable object ID" });
        _inspector.Children.Add(_identity);
        AddProperty("X", _x);
        AddProperty("Y", _y);
        AddProperty("Width", _width);
        AddProperty("Height", _height);
        AddProperty("Appearance", _appearance);
    }

    private void AddProperty(string label, TextBox field)
    {
        _inspector.Children.Add(new TextBlock { Text = label });
        _inspector.Children.Add(field);
    }

    private static void AddButton(Panel panel, string text, Action action)
    {
        var button = new Button { Content = text, Margin = new Thickness(3), Padding = new Thickness(7, 4, 7, 4) };
        button.Click += (_, _) => action();
        panel.Children.Add(button);
    }

    private void AddObject(string kind)
    {
        if (_previewMode) return;
        var id = $"{kind}-{Guid.NewGuid():N}";
        var size = kind == "floor" ? (Width: 20d, Height: 1d) : (Width: 2d, Height: 2d);
        _objects.Add(new EnvironmentObject
        {
            Id = id,
            Kind = kind,
            X = 2 + (_objects.Count % 8) * 2.5,
            Y = 2 + (_objects.Count / 8) * 2.5,
            Width = size.Width,
            Height = size.Height,
            Appearance = kind == "floor" ? "stone" : "default"
        });
        _selectedId = id;
        SyncDocument();
        RefreshWorld();
        RefreshInspector();
        SetStatus($"Added {kind}. Object identity is {id}.");
    }

    private void RefreshWorld()
    {
        _world.Width = _document.Bounds.Width * UnitsToPixels;
        _world.Height = _document.Bounds.Height * UnitsToPixels;
        _world.Children.Clear();
        foreach (var item in _document.Objects)
        {
            var color = item.Kind switch
            {
                "floor" => Color.FromRgb(105, 116, 132),
                "platform" => Color.FromRgb(91, 132, 161),
                "ball" => Color.FromRgb(232, 169, 66),
                "hoop" => Color.FromRgb(82, 190, 160),
                "crane-placeholder" => Color.FromRgb(171, 126, 196),
                _ => Color.FromRgb(111, 158, 220)
            };
            var visual = new Border
            {
                Width = Math.Max(12, item.Width * UnitsToPixels),
                Height = Math.Max(12, item.Height * UnitsToPixels),
                Background = new SolidColorBrush(color),
                BorderBrush = item.Id == _selectedId ? Brushes.White : Brushes.Black,
                BorderThickness = item.Id == _selectedId ? new Thickness(3) : new Thickness(1),
                CornerRadius = item.Kind == "ball" ? new CornerRadius(30) : new CornerRadius(3),
                Child = new TextBlock
                {
                    Text = item.Kind,
                    Foreground = Brushes.Black,
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                },
                Tag = item.Id,
                Cursor = _previewMode ? Cursors.Hand : Cursors.SizeAll
            };
            Canvas.SetLeft(visual, item.X * UnitsToPixels);
            Canvas.SetTop(visual, item.Y * UnitsToPixels);
            visual.MouseLeftButtonDown += OnObjectMouseDown;
            visual.MouseMove += OnObjectMouseMove;
            visual.MouseLeftButtonUp += OnObjectMouseUp;
            _world.Children.Add(visual);
        }
    }

    private void OnObjectMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string id) return;
        _selectedId = id;
        RefreshWorld();
        RefreshInspector();
        if (!_previewMode)
        {
            _dragTarget = _world.Children.OfType<Border>().FirstOrDefault(item => Equals(item.Tag, id));
            if (_dragTarget is not null)
            {
                _dragOrigin = e.GetPosition(_world);
                _dragStartX = Canvas.GetLeft(_dragTarget);
                _dragStartY = Canvas.GetTop(_dragTarget);
                _dragTarget.CaptureMouse();
            }
        }
        e.Handled = true;
    }

    private void OnObjectMouseMove(object sender, MouseEventArgs e)
    {
        if (_previewMode || _dragTarget is null || e.LeftButton != MouseButtonState.Pressed) return;
        var current = e.GetPosition(_world);
        Canvas.SetLeft(_dragTarget, Math.Max(0, _dragStartX + current.X - _dragOrigin.X));
        Canvas.SetTop(_dragTarget, Math.Max(0, _dragStartY + current.Y - _dragOrigin.Y));
    }

    private void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragTarget is null) return;
        var id = _dragTarget.Tag as string;
        var x = Canvas.GetLeft(_dragTarget) / UnitsToPixels;
        var y = Canvas.GetTop(_dragTarget) / UnitsToPixels;
        _dragTarget.ReleaseMouseCapture();
        _dragTarget = null;
        if (id is not null)
        {
            ReplaceSelected(id, item => item with { X = x, Y = y });
            SyncDocument();
            RefreshWorld();
            RefreshInspector();
            SetStatus($"Moved {id} to ({x.ToString("0.##", CultureInfo.InvariantCulture)}, {y.ToString("0.##", CultureInfo.InvariantCulture)}).");
        }
    }

    private void RefreshInspector()
    {
        _name.Text = _document.Name;
        var selected = _objects.FirstOrDefault(item => item.Id == _selectedId);
        _identity.Text = selected?.Id ?? "(none)";
        _x.Text = selected?.X.ToString(CultureInfo.InvariantCulture) ?? "";
        _y.Text = selected?.Y.ToString(CultureInfo.InvariantCulture) ?? "";
        _width.Text = selected?.Width.ToString(CultureInfo.InvariantCulture) ?? "";
        _height.Text = selected?.Height.ToString(CultureInfo.InvariantCulture) ?? "";
        _appearance.Text = selected?.Appearance ?? "";
        _inspector.IsEnabled = !_previewMode && selected is not null;
    }

    private void ApplyProperties()
    {
        if (_previewMode || _selectedId is null) return;
        if (!TryNumber(_x.Text, out var x) || !TryNumber(_y.Text, out var y) ||
            !TryNumber(_width.Text, out var width) || !TryNumber(_height.Text, out var height))
        {
            SetStatus("Properties rejected: coordinates and dimensions must be finite numbers.");
            return;
        }
        ReplaceSelected(_selectedId, item => item with
        {
            X = x, Y = y, Width = width, Height = height,
            Appearance = string.IsNullOrWhiteSpace(_appearance.Text) ? item.Appearance : _appearance.Text.Trim()
        });
        _document = _document with { Name = string.IsNullOrWhiteSpace(_name.Text) ? _document.Name : _name.Text.Trim() };
        SyncDocument();
        var validation = EnvironmentDocumentValidator.Validate(_document);
        if (!validation.IsValid)
        {
            SetStatus(string.Join(" ", validation.Diagnostics.Select(item => item.Message)));
            return;
        }
        RefreshWorld();
        RefreshInspector();
        SetStatus("Properties applied and structurally validated.");
    }

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    private void ReplaceSelected(string id, Func<EnvironmentObject, EnvironmentObject> update)
    {
        var index = _objects.FindIndex(item => item.Id == id);
        if (index >= 0) _objects[index] = update(_objects[index]);
    }

    private void RemoveSelected()
    {
        if (_previewMode || _selectedId is null) return;
        _objects.RemoveAll(item => item.Id == _selectedId);
        _connections.RemoveAll(item => item.SourceObjectId == _selectedId || item.TargetObjectId == _selectedId);
        _selectedId = null;
        SyncDocument();
        RefreshWorld();
        RefreshInspector();
        SetStatus("Selected object and its dependent connections were removed.");
    }

    private void SetMode(bool preview)
    {
        SyncDocument();
        if (preview)
        {
            var validation = EnvironmentDocumentValidator.Validate(_document);
            if (!validation.IsValid)
            {
                SetStatus(string.Join(" ", validation.Diagnostics.Select(item => item.Message)));
                return;
            }
        }
        _previewMode = preview;
        _palette.IsEnabled = !preview;
        RefreshWorld();
        RefreshInspector();
        _hermit.Text = preview
            ? "Hermit is present as a deterministic local placeholder. No model is connected and no capability is executed."
            : "Build and validate the world, then enter Environment mode to inspect the selected object.";
        SetStatus(preview
            ? "Environment mode: read-only preview of the same declarative document."
            : "Build mode: authoring controls enabled.");
    }

    private void InspectSelected()
    {
        var item = _objects.FirstOrDefault(value => value.Id == _selectedId);
        if (item is null)
        {
            _hermit.Text = "Select an object before requesting inspection.";
            return;
        }
        _hermit.Text = $"Read-only local inspection\nObject: {item.Id}\nKind: {item.Kind}\nPosition: ({item.X:0.##}, {item.Y:0.##})\nSize: {item.Width:0.##} × {item.Height:0.##}\nAppearance: {item.Appearance}\nNo external model, registry capability, or physical simulation is connected.";
    }

    private void SyncDocument()
    {
        _document = _document with
        {
            Objects = _objects.ToArray(),
            Connections = _connections.ToArray()
        };
    }

    private EnvironmentValidationResult ValidateCurrent()
    {
        SyncDocument();
        return EnvironmentDocumentValidator.Validate(_document);
    }

    private void SaveDocument()
    {
        var validation = ValidateCurrent();
        if (!validation.IsValid)
        {
            SetStatus(string.Join(" ", validation.Diagnostics.Select(item => item.Message)));
            return;
        }
        var dialog = new SaveFileDialog { Filter = "Environment JSON (*.json)|*.json", FileName = "environment.json" };
        if (dialog.ShowDialog() != true) return;
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_document, new JsonSerializerOptions { WriteIndented = true }));
        SetStatus($"Saved declarative environment: {dialog.FileName}");
    }

    private void LoadDocument()
    {
        var dialog = new OpenFileDialog { Filter = "Environment JSON (*.json)|*.json" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<EnvironmentDocument>(File.ReadAllText(dialog.FileName))
                ?? throw new InvalidDataException("The selected file did not contain an environment document.");
            var validation = EnvironmentDocumentValidator.Validate(loaded);
            if (!validation.IsValid)
            {
                SetStatus(string.Join(" ", validation.Diagnostics.Select(item => item.Message)));
                return;
            }
            _document = loaded;
            _objects.Clear();
            _objects.AddRange(loaded.Objects);
            _connections.Clear();
            _connections.AddRange(loaded.Connections);
            _selectedId = null;
            RefreshWorld();
            RefreshInspector();
            SetStatus($"Loaded {loaded.Name} ({_objects.Count} objects, {_connections.Count} connections).");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            SetStatus($"Load rejected: {exception.Message}");
        }
    }

    private void SetStatus(string message) => _status.Text = message;
}
