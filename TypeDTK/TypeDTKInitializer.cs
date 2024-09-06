using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeDTK.View.Viewer;

namespace TypeDTK;

/// <summary>
/// </summary>
public class TypeDTKInitializer : TypeDModuleInitializer
{
    // Models
    IPanelModel PanelModel { get; set; }

    // Functions
    /// <inheritdoc/>
    public override void Initializer(Project project)
    {
        // Models
        PanelModel = Resources.Get<IPanelModel>();

        // Viewers
        PanelModel.AddViewer<TKViewer>();
    }

    /// <inheritdoc/>
    public override void Uninitializer() { }
}