using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TheSingularityWorkshop.Workshop.Gui;
using TheSingularityWorkshop.GUI.WPF;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Native manifestation shell for the Workshop Moniker Experience.
/// The shell is presentation infrastructure; the Moniker content is still
/// composed as a GUI semantic tree before it reaches WPF.
/// </summary>
internal static class AnyAppMonikerShell
{
    public static Grid Create(
        GuiNode monikerRoot,
        Action<string>? navigate = null)
    {
        ArgumentNullException.ThrowIfNull(monikerRoot);

        var shell = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(2, 7, 17))
        };

        shell.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(230)
        });
        shell.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        shell.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(64)
        });
        shell.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star)
        });

        var navigation = BuildNavigation(navigate);
        Grid.SetColumn(navigation, 0);
        Grid.SetRowSpan(navigation, 2);
        shell.Children.Add(navigation);

        var header = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = new SolidColorBrush(Color.FromRgb(3, 8, 17)),
            Child = new TextBlock
            {
                Text = "THE WORKSHOP",
                Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(header, 1);
        Grid.SetRow(header, 0);
        shell.Children.Add(header);

        var renderedMoniker = WpfGuiRenderer.Render(monikerRoot);
        renderedMoniker.HorizontalAlignment = HorizontalAlignment.Stretch;
        renderedMoniker.VerticalAlignment = VerticalAlignment.Stretch;

        var stage = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(2, 7, 17)),
            Child = renderedMoniker,
            ClipToBounds = true
        };

        Grid.SetColumn(stage, 1);
        Grid.SetRow(stage, 1);
        shell.Children.Add(stage);

        shell.Loaded += (_, _) => AnimateGlyphs(renderedMoniker);

        return shell;
    }

    private static Border BuildNavigation(Action<string>? navigate)
    {
        var panel = new Grid
        {
            Background = new LinearGradientBrush(
                Color.FromRgb(4, 8, 18),
                Color.FromRgb(5, 10, 21),
                90),
        };

        panel.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(178)
        });
        panel.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star)
        });

        var brand = new StackPanel
        {
            Margin = new Thickness(24, 34, 16, 0)
        };

        brand.Children.Add(new TextBlock
        {
            Text = "THE",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 168, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        brand.Children.Add(new TextBlock
        {
            Text = "SINGULARITY",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 44, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        brand.Children.Add(new TextBlock
        {
            Text = "WORKSHOP",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var menu = new StackPanel
        {
            Margin = new Thickness(30, 32, 20, 20)
        };

        AddMenuItem(menu, "UNDERSTAND", "#FF2CFF", null);
        AddMenuItem(menu, "EXPERIENCES", "#A6BCD0", null);
        AddMenuItem(menu, "CREATE", "#FFD34D", () => navigate?.Invoke("CREATE"));
        AddMenuItem(menu, "RENDERING", "#00EAFB", () => navigate?.Invoke("RENDERING"));
        AddMenuItem(menu, "EDUCATION", "#A6BCD0", null);
        AddMenuItem(menu, "MADMEN", "#A6BCD0", null);

        var support = new TextBlock
        {
            Text = "♥  SUPPORT",
            FontFamily = new FontFamily("Consolas"),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207)),
            Margin = new Thickness(18, 28, 0, 0)
        };
        menu.Children.Add(support);

        Grid.SetRow(brand, 0);
        panel.Children.Add(brand);
        Grid.SetRow(menu, 1);
        panel.Children.Add(menu);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = panel
        };
    }

    private static void AddMenuItem(
        Panel parent,
        string text,
        string foreground,
        Action? action)
    {
        var item = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground)),
            Margin = new Thickness(18, 0, 0, 30)
        };

        if (action is not null)
        {
            item.Cursor = System.Windows.Input.Cursors.Hand;
            item.MouseLeftButtonUp += (_, _) => action();
        }

        parent.Children.Add(item);
    }

    private static void AnimateGlyphs(FrameworkElement root)
    {
        var glyphs = new List<FrameworkElement>();
        CollectGlyphs(root, glyphs);

        for (var index = 0; index < glyphs.Count; index++)
        {
            var glyph = glyphs[index];
            var phase = (index * 0.17) % 1.2;

            var transforms = new TransformGroup();
            var translate = new TranslateTransform();
            var rotate = new RotateTransform();
            var scale = new ScaleTransform(1, 1);
            transforms.Children.Add(scale);
            transforms.Children.Add(translate);
            transforms.Children.Add(rotate);
            glyph.RenderTransformOrigin = new Point(.5, 1);
            glyph.RenderTransform = transforms;

            var x = new DoubleAnimation
            {
                From = -4,
                To = 4,
                Duration = TimeSpan.FromSeconds(3.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(phase),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            translate.BeginAnimation(TranslateTransform.XProperty, x);

            var y = new DoubleAnimation
            {
                From = 1,
                To = -8,
                Duration = TimeSpan.FromSeconds(3.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(phase),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            translate.BeginAnimation(TranslateTransform.YProperty, y);

            var angle = new DoubleAnimation
            {
                From = -2.5,
                To = 2.5,
                Duration = TimeSpan.FromSeconds(3.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(phase),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, angle);

            var breathing = new DoubleAnimation
            {
                From = 1,
                To = 1.035,
                Duration = TimeSpan.FromSeconds(3.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(phase),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, breathing);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, breathing);
        }
    }

    private static bool IsGlyphName(string name)
    {
        if (!name.StartsWith("moniker-", StringComparison.Ordinal))
            return false;

        var lastDash = name.LastIndexOf('-');
        return lastDash > 8 &&
               lastDash < name.Length - 1 &&
               int.TryParse(name[(lastDash + 1)..], out _);
    }

    private static void CollectGlyphs(
        DependencyObject root,
        ICollection<FrameworkElement> glyphs)
    {
        if (root is FrameworkElement element &&
            IsGlyphName(element.Name))
        {
            glyphs.Add(element);
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            CollectGlyphs(VisualTreeHelper.GetChild(root, index), glyphs);
    }
}
