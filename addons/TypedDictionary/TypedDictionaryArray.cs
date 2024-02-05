using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace TypedDictionaryProject.addons.TypedDictionary;

/// <summary>
/// For displaying array inside the custom dictionary, you might wonder why not just use the vanilla Array, well thing is. I can't, or at least Godot doesn't have this feature yet.
/// Currently doesn't support Array that have no Type assigned
/// </summary>
public partial class TypedDictionaryArray : TypedDictionaryKVP
{
    public Godot.Collections.Array ItemArray;
    public Godot.Collections.Array<TypedDictionaryArrayButtonMover> ButtonMover;
    public Dictionary AttachedDictionary;

    private VBoxContainer m_Dropdown, m_ContentDropdown;
    private Variant m_Item;
    private GodotObject m_EditingObject;
    private string m_PropertyName;
    private Type m_ExpectedType;

    public TypedDictionaryArray SetData(GodotObject editingObject, Dictionary attachedDictionary, string propertyName,
                                        Variant array, Type expectedType, KeyValuePair<Variant, Variant> keyValuePair,
                                        bool enabled = true)
    {
        //TODO: Use Nullable instead
        if (keyValuePair.Equals(default))
        {
            throw new ArgumentNullException(nameof(KeyValuePair));
        }
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        KVP = keyValuePair;
        AttachedDictionary = attachedDictionary;
        m_EditingObject = editingObject;
        m_PropertyName = propertyName;
        m_ExpectedType = expectedType;
        ItemArray = array.AsGodotArray();
        ButtonMover = new();

        Button enableButton = new()
        {
            Text = $"Array[{expectedType.Name}] (size {ItemArray.Count})",
            Disabled = !enabled
        };

        m_Dropdown = new VBoxContainer
        {
            Visible = !enabled
        };
        m_ContentDropdown = new VBoxContainer();
        m_Dropdown.AddChild(CreateSizeMenu());
        m_Dropdown.AddChild(m_ContentDropdown);

        foreach (Variant item in ItemArray)
        {
            CreateNewItem(editingObject, propertyName, expectedType, m_ContentDropdown, item, m_ContentDropdown.GetChildCount());
        }

        enableButton.Pressed += () => m_Dropdown.Visible = !m_Dropdown.Visible;
        AddChild(enableButton);
        m_Dropdown.AddChild(CreateAddElementButton());
        AddChild(m_Dropdown);
        SetBottomEditor(m_Dropdown);
        return this;
    }

    private void CreateNewItem(GodotObject editingObject, string propertyName, Type expectedType, VBoxContainer dropdown, Variant item, int currentIndex)
    {
        HBoxContainer widthSeparator = new()
        {
            Name = $"Item_{currentIndex}"
        };
        HBoxContainer indexSeparator = new()
        {
            Name = "IndexSeparator"
        };

        widthSeparator.AddThemeConstantOverride("separation", 100);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        //TODO:Move all this line to a SetData method
        TypedDictionaryArrayButtonMover indexMover = new()
        {
            CurrentData = item,
            WidthSeparator = widthSeparator,
            TargetPropertyName = propertyName,
            ArrayParent = this,
            CurrentIndex = currentIndex,
            EditingObject = editingObject,
            DragData = $"TypedDictionaryArray_{propertyName}_{currentIndex}"
        };

        Label indexLabel = new()
        {
            Name = "LabelText",
            Text = currentIndex.ToString(),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        indexMover.LabelIndex = indexLabel;
        indexSeparator.AddChild(new TypedDictionaryRemoveButton().SetArrayData(item, (Variant removedItem) =>
        {
            if (ItemArray.Remove(removedItem))
            {
                editingObject.NotifyPropertyListChanged();
            }
        }));
        indexSeparator.AddChild(indexMover);
        indexSeparator.AddChild(indexLabel);

        TypedDictionaryBase typedItemBase = new TypedDictionaryBase().SetData(editingObject, expectedType, propertyName, true, item, ItemType.Value);
        typedItemBase.OnValueChanged = (Variant newValue, Variant actualOldValue) =>
        {
            indexMover.CurrentData = newValue;
            ItemArray[indexMover.CurrentIndex] = newValue;
            typedItemBase.NotifyChanged();
            return true;
        };
        indexMover.Contents = typedItemBase;
        ButtonMover.Add(indexMover);

        widthSeparator.AddChild(indexSeparator);
        widthSeparator.AddChild(typedItemBase);

        dropdown.AddChild(widthSeparator);
        AttachedDictionary[KVP.Key] = ItemArray;
        EmitChanged(propertyName, AttachedDictionary);
    }

    public Control CreateSizeMenu()
    {
        HBoxContainer separator = new()
        {
            Name = "Separator"
        };
        Label sizeLabel = new()
        {
            Text = "Size:"
        };

        SpinBox spinBox = new()
        {
            MinValue = 0,
            Value = ItemArray.Count,

            //Why shrink end doesn't work????
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            GrowHorizontal = GrowDirection.Both
        };
        spinBox.ValueChanged += (double newValue) =>
        {
            if (newValue > ItemArray.Count)
            {
                double amountToAdd = newValue - ItemArray.Count;
                for (int i = 0; i < amountToAdd; i++)
                {
                    ItemArray.Add(default);
                    CreateNewItem(m_EditingObject, m_PropertyName, m_ExpectedType, m_ContentDropdown, default, m_ContentDropdown.GetChildCount());
                }
            }

            else if (newValue < ItemArray.Count)
            {
                //Wonky code to remove excessive item
                int amountToRemove = ItemArray.Count - (int)newValue;
                int endCount = ItemArray.Count;
                for (int i = ItemArray.Count - 1; i >= 0 && m_ContentDropdown.GetChildCount() >= i; i--)
                {
                    if (ItemArray.Count == newValue)
                        break;
                    Node toRemove = m_ContentDropdown.GetChild(i);
                    m_ContentDropdown.RemoveChild(toRemove);
                    toRemove.Free();
                    ItemArray.RemoveAt(i);
                    ButtonMover.RemoveAt(i);
                }
                EmitChanged(m_PropertyName, AttachedDictionary);
            }
        };

        separator.AddChild(sizeLabel);
        separator.AddChild(spinBox);
        return separator;
    }

    public Control CreateAddElementButton()
    {
        Button button = new()
        {
            ExpandIcon = true,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            GrowHorizontal = GrowDirection.Begin,
            Text = " +         Add Element          "
        };
        button.Pressed += () =>
        {
            ItemArray.Add(default);
            CreateNewItem(m_EditingObject, m_PropertyName, m_ExpectedType, m_ContentDropdown, default, m_ContentDropdown.GetChildCount());
        };

        TextureRect texture = new()
        {
            Texture = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons"),
            OffsetTop = 7,
            OffsetLeft = 5,
            CustomMinimumSize = new Vector2(15, 15)
        };
        button.AddChild(texture);
        return button;
    }
}
