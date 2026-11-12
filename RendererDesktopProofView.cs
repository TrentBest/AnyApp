using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TheSingularityWorkshop.Renderer;
using TheSingularityWorkshop.Renderer.Diagnostics;
using TheSingularityWorkshop.Renderer.VisualizeAdapter;
using TheSingularityWorkshop.Visualize;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Native desktop harness for independent Renderer observer workloads.
/// Its WPF marks are a diagnostic projection, not the Visualize backend or a GPU proof.
/// </summary>
public sealed class RendererDesktopProofView : Grid
{
    private const string NearObserverId = "desktop-near-observer";
    private const string DistantObserverId = "desktop-distant-observer";
    private const long SourceRevision = 1;

    private readonly IReadOnlyList<RenderDrawCommand> _candidates =
        RendererDiagnosticScene.CreateCommands(1_000);
    private readonly ObserverPanelFrameRouter _router = new();
    private readonly RendererDesktopProofSurface _nearLeft = new("NEAR / PANEL A");
    private readonly RendererDesktopProofSurface _nearRight = new("NEAR / PANEL B");
    private readonly RendererDesktopProofSurface _distantLeft = new("DISTANT / PANEL A");
    private readonly RendererDesktopProofSurface _distantRight = new("DISTANT / PANEL B");
    private readonly TextBlock _metrics = new();
    private long _nearObserverRevision;
    private long _distantObserverRevision;
    private long _nearFrameSequence;
    private long _distantFrameSequence;

    public RendererDesktopProofView()
    {
        Background = new SolidColorBrush(Color.FromRgb(2, 7, 17));
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel { Margin = new Thickness(24, 18, 24, 12) };
        heading.Children.Add(new TextBlock
        {
            Text = "RENDERER // DESKTOP DIAGNOSTIC",
            Foreground = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 15,
            FontWeight = FontWeights.Bold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Two independent observers · one source revision · two panels per observer · CPU/WPF diagnostic · not GPU",
            Foreground = new SolidColorBrush(Color.FromRgb(145, 170, 180)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        });
        Children.Add(heading);
        SetRow(heading, 0);

        var nearControls = CreateControls(
            "NEAR OBSERVER · INTERACTIVE VIEW",
            detail => SetDetail(NearObserverId, detail),
            new Thickness(24, 0, 24, 8));
        Children.Add(nearControls);
        SetRow(nearControls, 1);

        var nearPanels = CreatePanelPair(
            "NEAR OBSERVER FRAME · SHARED BY TWO PANELS",
            _nearLeft,
            _nearRight);
        Children.Add(nearPanels);
        SetRow(nearPanels, 2);

        var distantControls = CreateControls(
            "DISTANT OBSERVER · SEMANTIC VIEW",
            detail => SetDetail(DistantObserverId, detail),
            new Thickness(24, 12, 24, 8));
        Children.Add(distantControls);
        SetRow(distantControls, 3);

        var distantPanels = CreatePanelPair(
            "DISTANT OBSERVER FRAME · SHARED BY TWO PANELS",
            _distantLeft,
            _distantRight);
        Children.Add(distantPanels);
        SetRow(distantPanels, 4);

        _metrics.Margin = new Thickness(24, 8, 24, 18);
        _metrics.Foreground = new SolidColorBrush(Color.FromRgb(0, 234, 255));
        _metrics.FontFamily = new FontFamily("Consolas");
        _metrics.FontSize = 10;
        Children.Add(_metrics);
        SetRow(_metrics, 5);

        SetDetail(NearObserverId, RendererRepresentation.Interactive);
        SetDetail(DistantObserverId, RendererRepresentation.Semantic);
    }

    private static StackPanel CreateControls(
        string heading,
        Action<RendererRepresentation> onSelected,
        Thickness margin)
    {
        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = margin
        };
        controls.Children.Add(new TextBlock
        {
            Text = heading,
            Foreground = new SolidColorBrush(Color.FromRgb(145, 170, 180)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });

        foreach (var detail in Enum.GetValues<RendererRepresentation>())
        {
            var button = new Button
            {
                Content = detail.ToString().ToUpperInvariant(),
                Tag = detail,
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(9, 5, 9, 5),
                Background = new SolidColorBrush(Color.FromRgb(4, 18, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => onSelected((RendererRepresentation)button.Tag);
            controls.Children.Add(button);
        }

        return controls;
    }

    private static Grid CreatePanelPair(
        string heading,
        RendererDesktopProofSurface leftSurface,
        RendererDesktopProofSurface rightSurface)
    {
        var panels = new Grid { Margin = new Thickness(24, 0, 24, 0) };
        panels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panels.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panels.ColumnDefinitions.Add(new ColumnDefinition());
        panels.ColumnDefinitions.Add(new ColumnDefinition());

        var label = new TextBlock
        {
            Text = heading,
            Foreground = new SolidColorBrush(Color.FromRgb(145, 170, 180)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Margin = new Thickness(0, 0, 0, 6)
        };
        panels.Children.Add(label);
        SetColumnSpan(label, 2);

        panels.Children.Add(CreatePanel(leftSurface, 0));
        panels.Children.Add(CreatePanel(rightSurface, 1));
        return panels;
    }

    private static Border CreatePanel(RendererDesktopProofSurface surface, int column)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(column == 0 ? 0 : 6, 0, column == 0 ? 6 : 0, 0),
            Child = surface
        };
        SetRow(border, 1);
        SetColumn(border, column);
        return border;
    }

    private void SetDetail(string observerId, RendererRepresentation detail)
    {
        var isNear = string.Equals(observerId, NearObserverId, StringComparison.Ordinal);
        var observerRevision = isNear ? ++_nearObserverRevision : ++_distantObserverRevision;
        var frameSequence = isNear ? ++_nearFrameSequence : ++_distantFrameSequence;

        // Each observer independently evaluates the same authoritative source snapshot.
        var result = RenderCommandReducer.Reduce(_candidates, detail);
        var observerPosition = isNear ? Vector3.Zero : new Vector3(0, 0, 100);
        var frame = new RenderFrame(
            new RenderView(
                observerPosition,
                Matrix4x4.Identity,
                Matrix4x4.Identity),
            result.Commands);
        var envelope = new ObserverFrameEnvelope(
            observerId,
            observerRevision,
            SourceRevision,
            frameSequence,
            frame);

        // Only the completed envelope is fanned out; its two panels do not re-evaluate policy.
        var panelIds = isNear
            ? new[] { "near-panel-left", "near-panel-right" }
            : new[] { "distant-panel-left", "distant-panel-right" };
        _router.Route(envelope, panelIds);

        if (isNear)
        {
            if (_router.TryGetFrame(panelIds[0], out var left) && left is not null)
                _nearLeft.SetFrame(left);
            if (_router.TryGetFrame(panelIds[1], out var right) && right is not null)
                _nearRight.SetFrame(right);
        }
        else
        {
            if (_router.TryGetFrame(panelIds[0], out var left) && left is not null)
                _distantLeft.SetFrame(left);
            if (_router.TryGetFrame(panelIds[1], out var right) && right is not null)
                _distantRight.SetFrame(right);
        }

        _metrics.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"SOURCE REV {SourceRevision}  |  NEAR OBS REV {_nearObserverRevision} / FRAME {_nearFrameSequence}  |  DISTANT OBS REV {_distantObserverRevision} / FRAME {_distantFrameSequence}  |  LAST: {observerId} {detail} · {result.Metrics.OutputCommands:N0}/{result.Metrics.InputCommands:N0} commands");
    }

    private sealed class RendererDesktopProofSurface : FrameworkElement
    {
        private readonly string _panelName;
        private VisualizerFrame? _frame;
        private ObserverFrameEnvelope? _envelope;

        public RendererDesktopProofSurface(string panelName)
        {
            _panelName = panelName;
            ClipToBounds = true;
        }

        public void SetFrame(ObserverFrameEnvelope envelope)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            _envelope = envelope;
            _frame = new RendererFrameAdapter().Adapt(envelope.Frame);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var width = ActualWidth;
            var height = ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(1, 5, 13)),
                null,
                new Rect(0, 0, width, height));

            if (_frame is null || _envelope is null)
                return;

            var step = Math.Max(1, (int)Math.Ceiling(_frame.Commands.Count / 2_500d));
            for (var index = 0; index < _frame.Commands.Count; index += step)
            {
                var command = _frame.Commands[index];
                var x = (command.WorldTransform.M41 + 1f) * 0.5f * width;
                var y = (command.WorldTransform.M42 + 1f) * 0.5f * height;
                var radius = command.Representation switch
                {
                    VisualizerRepresentation.Semantic => 1.4,
                    VisualizerRepresentation.Aggregate => 2.0,
                    VisualizerRepresentation.Distant => 2.6,
                    VisualizerRepresentation.Near => 3.4,
                    VisualizerRepresentation.Interactive => 4.4,
                    _ => 2.0
                };
                var color = command.Representation switch
                {
                    VisualizerRepresentation.Semantic => Color.FromRgb(95, 120, 140),
                    VisualizerRepresentation.Aggregate => Color.FromRgb(0, 168, 255),
                    VisualizerRepresentation.Distant => Color.FromRgb(0, 234, 255),
                    VisualizerRepresentation.Near => Color.FromRgb(255, 47, 255),
                    VisualizerRepresentation.Interactive => Color.FromRgb(255, 211, 77),
                    _ => Colors.White
                };
                var brush = new SolidColorBrush(color);
                drawingContext.DrawEllipse(brush, null, new Point(x, y), radius, radius);

                if (command.Representation == VisualizerRepresentation.Interactive)
                {
                    var pen = new Pen(brush, 0.8);
                    drawingContext.DrawLine(pen, new Point(x - radius - 1, y), new Point(x + radius + 1, y));
                    drawingContext.DrawLine(pen, new Point(x, y - radius - 1), new Point(x, y + radius + 1));
                }
            }

            var representation = _frame.Commands.FirstOrDefault()?.Representation.ToString().ToUpperInvariant() ?? "EMPTY";
            var note = new FormattedText(
                $"{_panelName} · {_envelope.ObserverId} · OBS {_envelope.ObserverRevision} · SRC {_envelope.SourceRevision} · FRAME {_envelope.FrameSequence} · {representation}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                9,
                new SolidColorBrush(Color.FromRgb(145, 170, 180)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(note, new Point(8, 8));
        }
    }
}
