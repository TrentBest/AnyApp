using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
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
    private readonly RendererDesktopProofSurface _surface = new();
    private readonly TextBlock _metrics = new();

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
            Text = "Shared deterministic command workload · WPF diagnostic projection · not a GPU/Visualize claim",
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
            button.Click += (_, _) => _surface.SetDetail((RendererRepresentation)button.Tag);
            controls.Children.Add(button);
        }
        Children.Add(controls);
        SetRow(controls, 1);

        Children.Add(_surface);
        SetRow(_surface, 2);

        _metrics.Margin = new Thickness(24, 8, 24, 18);
        _metrics.Foreground = new SolidColorBrush(Color.FromRgb(0, 234, 255));
        _metrics.FontFamily = new FontFamily("Consolas");
        _metrics.FontSize = 11;
        Children.Add(_metrics);
        SetRow(_metrics, 3);

        _surface.MetricsChanged += metrics =>
        {
            _metrics.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"Entities represented: {metrics.EntitiesRepresented:N0}  |  Candidate commands: {metrics.InputCommands:N0}  |  Selected: {metrics.OutputCommands:N0}  |  Omitted: {metrics.OmittedCommands:N0}");
        };
    }

    private sealed class RendererDesktopProofSurface : FrameworkElement
    {
        private readonly IReadOnlyList<RenderDrawCommand> _candidates =
            RendererDiagnosticScene.CreateCommands(1_000);

        private RenderCommandReductionResult _result = null!;
        private VisualizerFrame _frame = null!;

        public event Action<RenderCommandReductionMetrics>? MetricsChanged;

        public RendererDesktopProofSurface()
        {
            ClipToBounds = true;
            SetDetail(RendererRepresentation.Interactive);
        }

        public void SetDetail(RendererRepresentation detail)
        {
            _result = RenderCommandReducer.Reduce(_candidates, detail);
            var sourceFrame = new RenderFrame(
                new RenderView(
                    System.Numerics.Vector3.Zero,
                    System.Numerics.Matrix4x4.Identity,
                    System.Numerics.Matrix4x4.Identity),
                _result.Commands);
            _frame = new RendererFrameAdapter().Adapt(sourceFrame);
            MetricsChanged?.Invoke(_result.Metrics);
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
                $"DETAIL CEILING: {_frame.Commands.FirstOrDefault().Representation.ToString().ToUpperInvariant()}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                10,
                new SolidColorBrush(Color.FromRgb(145, 170, 180)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(note, new Point(12, 10));
        }
    }
}
