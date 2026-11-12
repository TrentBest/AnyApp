using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TheSingularityWorkshop.Renderer;
using TheSingularityWorkshop.Renderer.Diagnostics;
using TheSingularityWorkshop.Renderer.VisualizeAdapter;
using TheSingularityWorkshop.Visualize;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Native desktop harness for the shared Renderer diagnostic workload.
/// Its WPF marks are a diagnostic projection, not the Visualize backend or a GPU proof.
/// </summary>
public sealed class RendererDesktopProofView : Grid
{
    private const string ObserverId = "desktop-proof-observer";
    private const long SourceRevision = 1;

    private readonly IReadOnlyList<RenderDrawCommand> _candidates =
        RendererDiagnosticScene.CreateCommands(1_000);
    private readonly ObserverPanelFrameRouter _router = new();
    private readonly RendererDesktopProofSurface _leftSurface = new("LEFT PANEL");
    private readonly RendererDesktopProofSurface _rightSurface = new("RIGHT PANEL");
    private readonly TextBlock _metrics = new();
    private long _observerRevision;
    private long _frameSequence;

    public RendererDesktopProofView()
    {
        Background = new SolidColorBrush(Color.FromRgb(2, 7, 17));
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
            Text = "One completed observer frame · two WPF panels · CPU diagnostic projection · not a GPU claim",
            Foreground = new SolidColorBrush(Color.FromRgb(145, 170, 180)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        });
        Children.Add(heading);
        SetRow(heading, 0);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(24, 0, 24, 12)
        };
        foreach (var detail in Enum.GetValues<RendererRepresentation>())
        {
            var button = new Button
            {
                Content = detail.ToString().ToUpperInvariant(),
                Tag = detail,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 7, 12, 7),
                Background = new SolidColorBrush(Color.FromRgb(4, 18, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => SetDetail((RendererRepresentation)button.Tag);
            controls.Children.Add(button);
        }
        Children.Add(controls);
        SetRow(controls, 1);

        var panels = new Grid { Margin = new Thickness(24, 0, 24, 0) };
        panels.ColumnDefinitions.Add(new ColumnDefinition());
        panels.ColumnDefinitions.Add(new ColumnDefinition());
        panels.Children.Add(CreatePanel("PANEL A · SHARED FRAME", _leftSurface, 0));
        panels.Children.Add(CreatePanel("PANEL B · SHARED FRAME", _rightSurface, 1));
        Children.Add(panels);
        SetRow(panels, 2);

        _metrics.Margin = new Thickness(24, 8, 24, 18);
        _metrics.Foreground = new SolidColorBrush(Color.FromRgb(0, 234, 255));
        _metrics.FontFamily = new FontFamily("Consolas");
        _metrics.FontSize = 11;
        Children.Add(_metrics);
        SetRow(_metrics, 3);

        SetDetail(RendererRepresentation.Interactive);
    }

    private static Border CreatePanel(
        string heading,
        RendererDesktopProofSurface surface,
        int column)
    {
        var panel = new Grid { Margin = new Thickness(column == 0 ? 0 : 8, 0, column == 0 ? 8 : 0, 0) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.Children.Add(new TextBlock
        {
            Text = heading,
            Foreground = new SolidColorBrush(Color.FromRgb(145, 170, 180)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Margin = new Thickness(8, 0, 8, 8)
        });
        panel.Children.Add(surface);
        Grid.SetRow(surface, 1);

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
            BorderThickness = new Thickness(1),
            Child = panel
        };
        Grid.SetColumn(border, column);
        return border;
    }

    private void SetDetail(RendererRepresentation detail)
    {
        // Evaluate and construct exactly one frame for this observer revision.
        var result = RenderCommandReducer.Reduce(_candidates, detail);
        var frame = new RenderFrame(
            new RenderView(
                System.Numerics.Vector3.Zero,
                System.Numerics.Matrix4x4.Identity,
                System.Numerics.Matrix4x4.Identity),
            result.Commands);
        var envelope = new ObserverFrameEnvelope(
            ObserverId,
            ++_observerRevision,
            SourceRevision,
            ++_frameSequence,
            frame);

        // Fan out the same completed envelope; panels do not evaluate policy or build frames.
        _router.Route(envelope, ["panel-left", "panel-right"]);
        if (_router.TryGetFrame("panel-left", out var left) && left is not null)
            _leftSurface.SetFrame(left);
        if (_router.TryGetFrame("panel-right", out var right) && right is not null)
            _rightSurface.SetFrame(right);

        _metrics.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Observer: {envelope.ObserverId}  |  Observer revision: {envelope.ObserverRevision}  |  Source revision: {envelope.SourceRevision}  |  Frame: {envelope.FrameSequence}  |  Entities: {result.Metrics.EntitiesRepresented:N0}  |  Candidates: {result.Metrics.InputCommands:N0}  |  Selected: {result.Metrics.OutputCommands:N0}  |  Omitted: {result.Metrics.OmittedCommands:N0}");
    }

    private sealed class RendererDesktopProofSurface(string panelName) : FrameworkElement
    {
        private VisualizerFrame? _frame;
        private ObserverFrameEnvelope? _envelope;

        public RendererDesktopProofSurface()
        {
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

            var note = new FormattedText(
                $"{panelName} · {_envelope.ObserverId} · OBS {_envelope.ObserverRevision} · SRC {_envelope.SourceRevision} · FRAME {_envelope.FrameSequence} · {_frame.Commands.FirstOrDefault().Representation.ToString().ToUpperInvariant()}",
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
