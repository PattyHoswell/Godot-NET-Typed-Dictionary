using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace TypedDictionaryProject.addons.TypedDictionary;

/// <summary>
/// The main class for editing dictionary
/// </summary>
public partial class TypedDictionaryDropdown : EditorProperty
{
    /// <summary>
    /// Save the item so that if the key is changed, value will refresh its key references, it is better to use <see cref="TypedDictionaryKVP.KVP"/> but i got tired
    /// </summary>
    public System.Collections.Generic.Dictionary<TypedDictionaryKVP, KeyValuePair<Variant, Variant>> Contents;
    public Button KeyValueAdder, SelfButton;

    private PanelContainer m_MainPanel;
    private GridContainer m_KVPMenuGrid, m_ContentGrid;
    private TypedDictionaryBase m_Key, m_Value;
    public (GodotObject editingObject, Dictionary dict, Dictionary parentDictionary,
             Type keyType, Type valueType, string propertyName, bool interactable) PropertyData;

    public TypedDictionaryDropdown SetData((GodotObject editingObject, Dictionary dict, Dictionary parentDictionary,
                                            Type keyType, Type valueType, string propertyName, bool interactable) propertyData)
    {
        PropertyChanged += TypedDictionaryDropdown_PropertyChanged;
        Contents = new();
        SelfButton = new();
        m_MainPanel = new();
        m_KVPMenuGrid = new();
        m_ContentGrid = new();
        PropertyData = propertyData;
        VBoxContainer dropDown = new()
        {
            Visible = false
        };

        m_MainPanel = new PanelContainer();
        m_MainPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        m_MainPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        m_MainPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        m_MainPanel.AddThemeStyleboxOverride("panel", TypedDictionaryInspectorPlugin.ContainerBG);

        m_KVPMenuGrid.SizeFlagsVertical = SizeFlags.ExpandFill;
        m_KVPMenuGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;


        m_ContentGrid.SizeFlagsVertical = SizeFlags.ExpandFill;
        m_ContentGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        m_ContentGrid.Columns = 3;

        m_MainPanel.AddChild(m_KVPMenuGrid);
        SelfButton.Disabled = !propertyData.interactable;
        
        //We not gonna specify the type because the name would be too long
        //SelfButton.Text = $"Dictionary[{PropertyData.keyType.Name},{PropertyData.valueType.Name}] (size {propertyData.dict.Count})";

        SelfButton.Text = $"Dictionary (size {propertyData.dict.Count})";
        SelfButton.Pressed += () => dropDown.Visible = !dropDown.Visible;

        PanelContainer panelBG = new();
        panelBG.SetAnchorsPreset(LayoutPreset.FullRect);
        panelBG.SizeFlagsVertical = SizeFlags.ExpandFill;
        panelBG.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panelBG.AddChild(m_ContentGrid);
        panelBG.AddThemeStyleboxOverride("panel", TypedDictionaryInspectorPlugin.ContainerBG);
        dropDown.AddChild(panelBG);
        dropDown.AddChild(m_MainPanel);

        foreach (KeyValuePair<Variant, Variant> item in propertyData.dict)
        {
            TypedDictionaryItem key = new TypedDictionaryItem().SetData(propertyData.editingObject, propertyData.propertyName,
                                                        item.Key.ToString(), item.Key, propertyData.keyType, ItemType.Key, item);
            TypedDictionaryItem value = new TypedDictionaryItem().SetData(propertyData.editingObject, propertyData.propertyName,
                                                          item.Value.ToString(), item.Value, propertyData.valueType, ItemType.Value, item);
            key.DropdownParent = this;
            value.DropdownParent = this;
            value.OriginalKey = key;

            Contents.Add(value, item);

            TypedDictionaryRemoveButton removeButton = new TypedDictionaryRemoveButton().SetDictionaryData(item, (KeyValuePair<Variant, Variant> removedItem) =>
            {
                if (PropertyData.dict.Remove(removedItem.Key))
                {
                    EmitChanged(PropertyData.propertyName, PropertyData.parentDictionary ?? PropertyData.dict);
                }
            });
            removeButton.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            removeButton.SizeFlagsVertical = SizeFlags.ShrinkBegin;

            m_ContentGrid.AddChild(removeButton);
            m_ContentGrid.AddChild(key);
            m_ContentGrid.AddChild(value);
        }

        AddKVPMenu();
        AddChild(SelfButton);
        AddChild(dropDown);

        dropDown.AddChild(CreateAddKVPButton());
        SetBottomEditor(dropDown);
        return this;
    }
    private Control CreateAddKVPButton()
    {
        KeyValueAdder = new()
        {
            ExpandIcon = true,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            GrowHorizontal = GrowDirection.Begin,
            Text = " +         Add Key/Value pairs          "
        };
        KeyValueAdder.Pressed += () =>
        {

            Dictionary godotDict = PropertyData.dict;
            if (godotDict.ContainsKey(m_Key.GetValue()))
            {
                GD.PrintErr($"Already have same key {m_Key.GetValue()}");
                return;
            }
            godotDict.Add(m_Key.GetValue(), m_Value.GetValue());
            EmitChanged(PropertyData.propertyName, PropertyData.parentDictionary ?? godotDict);
        };

        TextureRect texture = new()
        {
            Texture = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons"),
            OffsetTop = 7,
            OffsetLeft = 5,
            CustomMinimumSize = new Vector2(15, 15)
        };
        KeyValueAdder.AddChild(texture);

        return KeyValueAdder;
    }

    /// <summary>
    /// Used for resetting dictionary
    /// </summary>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="field"></param>
    /// <param name="changing"></param>
    private void TypedDictionaryDropdown_PropertyChanged(StringName property, Variant value, StringName field, bool changing)
    {
        if (property == PropertyData.propertyName)
        {
            PropertyData.editingObject.NotifyPropertyListChanged();
        }
    }

    private void AddKVPMenu()
    {
        VBoxContainer vBoxContainer = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        m_Key = new TypedDictionaryBase().SetData(PropertyData.editingObject, PropertyData.keyType, PropertyData.propertyName);
        m_Key.Container.GetNode<Label>("Title").Text = "Key";
        m_Key.OnValueChanged += (newValue, _) =>
        {
            m_Key.RefreshValue(newValue);
            return true;
        };
        if (m_Key.CanCastToTensor())
        {
            m_Key.CreateDefaultTensorValue();
        }

        m_Value = new TypedDictionaryBase().SetData(PropertyData.editingObject, PropertyData.valueType, PropertyData.propertyName);
        m_Value.Container.GetNode<Label>("Title").Text = "Value";
        m_Value.OnValueChanged += (newValue, _) =>
        {
            m_Value.RefreshValue(newValue);
            return true;
        };
        if (m_Value.CanCastToTensor())
        {
            m_Value.CreateDefaultTensorValue();
        }

        m_Key.DropdownParent = this;
        m_Value.DropdownParent = this;

        //Disable adding item from add element/key value pairs to avoid unexpected behaviour
        if (GD.TypeToVariantType(PropertyData.keyType) == Variant.Type.Dictionary && m_Value.Content is TypedDictionaryDropdown dropdown)
        {
            dropdown.SelfButton.Disabled = true;
        }
        if (GD.TypeToVariantType(PropertyData.valueType) == Variant.Type.Dictionary && m_Value.Content is TypedDictionaryDropdown dropdown2)
        {
            dropdown2.SelfButton.Disabled = true;
        }
        if (GD.TypeToVariantType(PropertyData.keyType) == Variant.Type.Array && m_Value.Content is TypedDictionaryArray array)
        {
            array.SelfButton.Disabled = true;
        }
        if (GD.TypeToVariantType(PropertyData.valueType) == Variant.Type.Array && m_Value.Content is TypedDictionaryArray array2)
        {
            array2.SelfButton.Disabled = true;
        }

        vBoxContainer.AddChild(m_Key);
        vBoxContainer.AddChild(m_Value);
        m_KVPMenuGrid.AddChild(vBoxContainer);
    }

}

