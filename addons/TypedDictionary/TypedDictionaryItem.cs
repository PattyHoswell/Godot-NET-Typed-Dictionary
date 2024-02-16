using Godot;
using System;
using System.Collections.Generic;

namespace TypedDictionaryProject.addons.TypedDictionary;

/// <summary>
/// Used for displaying dictionary item
/// </summary>
public partial class TypedDictionaryItem : TypedDictionaryBase
{

    private Variant m_Item;
    private GodotObject m_EditingObject;
    private string m_PropertyName, m_ItemName;
    private ItemType m_ItemType;

    public TypedDictionaryItem SetData(GodotObject editingObject, string propertyName, string itemName,
                                       Variant item, Type expectedType, ItemType itemType,
                                       KeyValuePair<Variant, Variant> keyValuePair)
    {
        base.MouseFilter = MouseFilterEnum.Pass;
        m_EditingObject = editingObject;
        m_ItemName = itemName;
        m_PropertyName = propertyName;
        m_Item = item;
        m_ItemType = itemType;
        _ = SetData(editingObject, expectedType, m_PropertyName, false, m_Item, m_ItemType, keyValuePair);
        Container.Reparent(this);
        return this;
    }
}

public enum ItemType
{
    Key,
    Value
}