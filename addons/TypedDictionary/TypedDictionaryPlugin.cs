
#if TOOLS
using Godot;

namespace TypedDictionaryProject.addons.TypedDictionary;

[Tool]
public partial class TypedDictionaryPlugin : EditorPlugin
{
    private TypedDictionaryInspectorPlugin m_InspectorPlugin;

    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        AddInspectorPlugin(m_InspectorPlugin = new TypedDictionaryInspectorPlugin());
    }


    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        RemoveInspectorPlugin(m_InspectorPlugin);
    }
}
#endif