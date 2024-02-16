using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypedDictionaryProject.addons.TypedDictionary;
/// <summary>
/// The button to remove an item from TypedDictionary or Array inside TypedDictionary
/// </summary>
public partial class TypedDictionaryRemoveButton : TypedDictionaryKVP
{
    public event Action<KeyValuePair<Variant, Variant>> OnRemoveDictionaryKVP;
    public event Action<Variant> OnRemoveArrayValue;
    public Variant ArrayValue { get; set; }
    public Button RemoveButton;
    public TypedDictionaryRemoveButton SetDictionaryData(KeyValuePair<Variant, Variant> kvp, Action<KeyValuePair<Variant, Variant>> onRemoveKVP)
    {
        OnRemoveDictionaryKVP += onRemoveKVP;
        KVP = kvp;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CreateButton().Pressed += () =>
        {
            OnRemoveDictionaryKVP?.Invoke(KVP);
        };
        return this;
    }

    public TypedDictionaryRemoveButton SetArrayData(Variant arrayValue, Action<Variant> onRemoveArrayValue)
    {
        OnRemoveArrayValue += onRemoveArrayValue;
        ArrayValue = arrayValue;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CreateButton().Pressed += () =>
        {
            OnRemoveArrayValue?.Invoke(ArrayValue);
        };
        return this;
    }
    private Button CreateButton()
    {
        RemoveButton = new();
        RemoveButton.TooltipText = "Remove entry";
        RemoveButton.ExpandIcon = true;
        RemoveButton.Text = " ";
        RemoveButton.SetAnchorsPreset(LayoutPreset.FullRect);
        RemoveButton.Pressed += () =>
        {
            OnRemoveDictionaryKVP?.Invoke(KVP);
        };
        TextureRect icon = new();
        icon.SetAnchorsPreset(LayoutPreset.FullRect);
        icon.Texture = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");
        RemoveButton.AddChild(icon);
        AddChild(RemoveButton);
        return RemoveButton;
    }
}
