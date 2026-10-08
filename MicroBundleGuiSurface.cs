using System.Reflection;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Bridges a host-independent MicroBundle to AnyApp's semantic GUI renderer.
/// </summary>
/// <remarks>
/// Canonical Experiences intentionally do not reference AnyApp. The current
/// GUI capability is exposed by the public <c>Root</c> property on bundles
/// that provide a semantic GUI surface.
/// </remarks>
internal static class MicroBundleGuiSurface
{
    public static bool TryGetRoot(IMicroBundle bundle, out GuiNode? root)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var property = bundle.GetType().GetProperty(
            "Root",
            BindingFlags.Instance | BindingFlags.Public);

        if (property is null ||
            !typeof(GuiNode).IsAssignableFrom(property.PropertyType))
        {
            root = null;
            return false;
        }

        root = property.GetValue(bundle) as GuiNode;
        return root is not null;
    }
}
