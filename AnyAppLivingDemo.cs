using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FsmApi = TheSingularityWorkshop.FSM_API.FSM_API;
using TheSingularityWorkshop.FSM_API;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Desktop Flex proof: 2056 independently instantiated FSM_API actors share one
/// processing group while retaining separate state contexts.
/// </summary>
public sealed class AnyAppLivingDemo : FrameworkElement, IDisposable
{
    public const int ActorCount = 2056;

    private const string ProcessingGroup = "AnyAppLiving";
    private const string DefinitionName = "AnyAppLivingActor";
    private readonly ActorContext[] _actors = new ActorContext[ActorCount];
    private readonly FSMHandle[] _handles = new FSMHandle[ActorCount];
    private readonly DispatcherTimer _paintTimer;
    private bool _disposed;

    private AnyAppLivingDemo()
    {
        FsmApi.Create.CreateProcessingGroup(ProcessingGroup);

        FsmApi.Create.CreateFiniteStateMachine(
                DefinitionName,
                processRate: -1,
                processingGroup: ProcessingGroup)
            .State(
                "LIVE",
                onEnter: null,
                onUpdate: context => ((ActorContext)context).Advance(),
                onExit: null)
            .WithInitialState("LIVE")
            .BuildDefinition();

        for (var index = 0; index < ActorCount; index++)
        {
            var context = new ActorContext(index);
            _actors[index] = context;
            _handles[index] = FsmApi.Create.CreateInstance(
                DefinitionName,
                context,
                ProcessingGroup);
        }

        _paintTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(33),
            DispatcherPriority.Render,
            (_, _) =>
            {
                FsmApi.Interaction.Update(ProcessingGroup);
                InvalidateVisual();
            },
            Dispatcher);

        SnapsToDevicePixels = true;
        Focusable = false;
    }

    public static AnyAppLivingDemo Create() => new();

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        drawingContext.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(2, 7, 17)),
            null,
            new Rect(0, 0, width, height));

        foreach (var actor in _actors)
        {
            var x = actor.X * width;
            var y = actor.Y * height;
            var radius = actor.Size;

            drawingContext.DrawEllipse(
                actor.Brush,
                null,
                new Point(x, y),
                radius,
                radius);
        }

        DrawHud(drawingContext, width, height);
    }

    private void DrawHud(DrawingContext drawingContext, double width, double height)
    {
        var title = new FormattedText(
            $"LIVING GUI // {ActorCount:N0} FSM_API ACTORS",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            15,
            Brushes.White,
            1.0);

        drawingContext.DrawText(title, new Point(28, 24));

        var detail = new FormattedText(
            "2056 independent FSM instances · one processing group · native desktop flex",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            11,
            new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            1.0);

        drawingContext.DrawText(detail, new Point(28, 48));

        var sampleStep = Math.Max(1, ActorCount / 18);
        for (var index = 0; index < ActorCount; index += sampleStep)
        {
            var actor = _actors[index];
            var label = new FormattedText(
                actor.Lineage,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                8,
                Brushes.White,
                1.0);

            drawingContext.DrawText(
                label,
                new Point(actor.X * width + 5, actor.Y * height - 4));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _paintTimer.Stop();

        foreach (var actor in _actors)
            actor.IsValid = false;
    }

    private sealed class ActorContext : IStateContext
    {
        private readonly double _phase;
        private readonly double _speed;

        public ActorContext(int index)
        {
            Name = $"AnyAppLiving:{index}";
            IsValid = true;

            var hash = unchecked((uint)(index * 747796405 + 2891336453));
            _phase = (hash % 10000) / 10000d * Math.PI * 2;
            _speed = .0015 + ((hash % 1000) / 1000000d);
            X = .5 + Math.Cos(_phase) * .42;
            Y = .5 + Math.Sin(_phase) * .42;
            Size = 1.2 + ((hash >> 8) % 10) * .07;
            Brush = new SolidColorBrush(
                Color.FromRgb(
                    (byte)(80 + ((hash >> 16) & 0x7F)),
                    (byte)(80 + ((hash >> 8) & 0x7F)),
                    (byte)(150 + (hash & 0x5F))));
            Brush.Freeze();
        }

        public string Name { get; set; }
        public bool IsValid { get; set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Size { get; }
        public Brush Brush { get; }
        public string Lineage => Name[(Name.LastIndexOf(':') + 1)..];

        public void Advance()
        {
            if (!IsValid)
                return;

            var phase = _phase + (DateTime.UtcNow.Ticks * _speed / TimeSpan.TicksPerSecond);
            X = .5 + Math.Cos(phase) * .46;
            Y = .5 + Math.Sin(phase * 1.13) * .46;
        }
    }
}
