using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeDCore.Models.Data;
using TypeDTK.View.Viewer;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.TK.Contents;

namespace TypeDTK;

/// <summary>
/// </summary>
public class TypeDTKInitializer : TypeDModuleInitializer
{
    // Providers
    IContentProvider ContentProvider { get; set; }

    // Models
    IPanelModel PanelModel { get; set; }

    // Functions
    /// <inheritdoc/>
    public override void Initializer(Project project)
    {
        // Providers
        ContentProvider = Resources.Get<IContentProvider>();

        // Models
        PanelModel = Resources.Get<IPanelModel>();

        // Content
        ContentProvider.AddSupportedContentType<Texture2dContent, Texture, TKTexture>("png");

        // Viewers
        PanelModel.AddViewer<TKViewer>();
    }

    /// <inheritdoc/>
    public override void Uninitializer()
    {
        ContentProvider.RemoveSupportedContentType<Texture2dContent>("png");
    }
}