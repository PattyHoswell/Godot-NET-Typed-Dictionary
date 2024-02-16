using Godot;
using System;
using System.ComponentModel;
using System.Reflection;

namespace TypedDictionaryProject.addons.TypedDictionary;

public partial class TypedDictionaryInspectorPlugin : EditorInspectorPlugin
{
    //Cache the stylebox so we only need to create it again when its on supported dictionary
    internal static StyleBoxFlat ContainerBG;
    public override bool _CanHandle(GodotObject @object)
    {
        return true;
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString,
        PropertyUsageFlags usageFlags, bool wide)
    {
        //You can choose to exclude whatever here

        if (type == Variant.Type.Dictionary && @object != null)
        {
            FieldInfo field = @object.GetType().GetField(name,
                BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            //Using reflection to get the assigned type from the dictionary
            Type[] arguments;
            if (field != null)
            {
                arguments = field.FieldType.GetGenericArguments();
            }
            else
            {
                PropertyInfo property = @object.GetType().GetProperty(name,
                    BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property == null) return false;
                arguments = property.PropertyType.GetGenericArguments();
            }
            if (arguments.Length <= 1)
                return false;

            ContainerBG = new StyleBoxFlat
            {
                BgColor = new Color(60 / 255f, 60 / 255f, 60 / 255f, 1f)
            };
            ContainerBG.BorderColor = ContainerBG.BgColor * 1.5f;
            ContainerBG.SetBorderWidthAll(2);
            ContainerBG.SetCornerRadiusAll(2);

            Type keyType = arguments[0];
            Type valueType = arguments[1];
            AddPropertyEditor(name, new TypedDictionaryDropdown().SetData((@object, @object.Get(name).AsGodotDictionary(), null,
                                                                          keyType, valueType, name, true)));
            return true;
        }

        return base._ParseProperty(@object, type, name, hintType, hintString, usageFlags, wide);
    }
}

