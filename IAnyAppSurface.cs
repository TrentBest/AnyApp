using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>Platform-neutral GUI surface exposed by an Experience to the AnyApp host.</summary>
public interface IAnyAppSurface
{
    GuiNode Root { get; }
}
