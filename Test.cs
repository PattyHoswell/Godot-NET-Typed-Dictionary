using Godot;
using System;
using Godot.Collections;
/// <summary>
/// Example of using TypedDictionary
/// <para>The dictionary should derive from <see cref="Godot.Collections.Dictionary{TKey, TValue}"/> instead of <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/></para>
/// <para>You need to add <see cref="ToolAttribute"/>, otherwise the plugin won't be able to find them</para>
/// This is Godot Engine limitation
/// 
/// <para>If you want to check whether you are on editor or in game. Use <see cref="Engine.IsEditorHint"/></para>
/// </summary>
[Tool]
public partial class Test : Node2D
{
    [Export] public Dictionary<string, TypeCode> TypeCodes = null;
    [Export] public Dictionary<Vector3, TestResources> TestResourcesDict = null;
    [Export] public Dictionary<Plane, Array<PackedScene>> PackedScenesDict = null;
    [Export] public Dictionary<int, Dictionary<string, AudioStream>> AudioStreamDict = null;
    [Export] public Dictionary<bool, Variant.Type> VariantTypesDict = null;
    [Export] public Array<string> Yes;

    public void PrintAllDictionary()
    {
        PrintDictionary(TypeCodes, nameof(TypeCodes));
        PrintDictionary(TestResourcesDict, nameof(TestResourcesDict));
        PrintDictionary(PackedScenesDict, nameof(PackedScenesDict));
        PrintDictionary(AudioStreamDict, nameof(AudioStreamDict));
        PrintDictionary(VariantTypesDict, nameof(VariantTypesDict));
    }
    public override void _Ready()
    {
        PrintAllDictionary();
    }

    void PrintDictionary<[MustBeVariant] K, [MustBeVariant] V>(Dictionary<K, V> dict, string dictionaryName)
    {
        if (dict != null &&  dict.Count > 0)
        {
            GD.Print("<------", dictionaryName, "------>");
            foreach (var item in dict)
            {
                GD.Print(item); 
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            PrintAllDictionary();
        }
    }
}
