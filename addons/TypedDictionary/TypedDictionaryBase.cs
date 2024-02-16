using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedDictionaryProject.addons.TypedDictionary;
public partial class TypedDictionaryBase : TypedDictionaryKVP
{
    public Control Content;
    public HBoxContainer Container;
    public Variant.Type VariantType;
    public Func<Variant, Variant, bool> OnValueChanged;

    //This is added so that when the key changes, m_DictItem will also changes with it.
    public TypedDictionaryBase OriginalKey;
    public TypedDictionaryDropdown DropdownParent;
    public Godot.Collections.Dictionary AttachedDictionary;
    public Type ManagedType;

    private ItemType m_ItemType;
    private GodotObject m_EditingObject;
    private Variant m_Value;
    private string m_PropertyName;

    public TypedDictionaryBase SetData(GodotObject editingObject, Type managedItemType, string propertyName,
                                       bool createName = true, Variant defaultValue = default, ItemType itemType = ItemType.Key,
                                       KeyValuePair<Variant, Variant> keyValuePair = default)
    {
        //TODO: Use Nullable instead
        if (keyValuePair.Equals(default))
            throw new ArgumentNullException(nameof(keyValuePair));
        if (defaultValue.Equals(default) && itemType == ItemType.Key)
            throw new ArgumentNullException(nameof(defaultValue));

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        ManagedType = managedItemType ?? throw new ArgumentNullException("key");
        KVP = keyValuePair;
        m_PropertyName = propertyName;
        m_EditingObject = editingObject;
        m_ItemType = itemType;
        VariantType = GD.TypeToVariantType(managedItemType);
        m_Value = defaultValue;

        Container = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        if (CanCastToTensor())
        {
            if (defaultValue.Equals(default))
            {
                CreateDefaultTensorValue();
            }
            Content = CreateTensorProperty(VariantType);
            Content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        }
        else if (managedItemType.IsEnum)
        {
            OptionButton optionButton = new();
            foreach (string VARIABLE in Enum.GetNames(managedItemType))
            {
                optionButton.AddItem(VARIABLE);
            }
            optionButton.ItemSelected += (newValue) =>
            {
                if (UpdateValue(newValue, m_Value))
                {
                    NotifyChanged();
                }
                else
                {
                    optionButton.Selected = m_Value.AsInt32();
                }
            };

            optionButton.Selected = m_Value.AsInt32();
            Content = optionButton;
            Content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        }

        else if (VariantType == Variant.Type.Dictionary)
        {
            Godot.Collections.Dictionary dictionary = m_Value.AsGodotDictionary();
            Tuple<Type, Type> kvpType = Tuple.Create(managedItemType.GetGenericArguments()[0], managedItemType.GetGenericArguments()[1]);
            Content = new TypedDictionaryDropdown().SetData((editingObject, dictionary, editingObject.Get(propertyName).AsGodotDictionary(),
                                                             kvpType.Item1, kvpType.Item2, propertyName, true));
        }

        else if (VariantType == Variant.Type.Array)
        {
            Godot.Collections.Array array = m_Value.AsGodotArray();
            Type type = managedItemType.GetGenericArguments()[0];
            Content = new TypedDictionaryArray().SetData(editingObject, editingObject.Get(propertyName).AsGodotDictionary(), propertyName, array, type, KVP, true);
        }

        else
        {
            switch (VariantType)
            {
                case Variant.Type.String:
                case Variant.Type.StringName:
                    LineEdit lineEdit = new()
                    {
                        MaxLength = int.MaxValue
                    };
                    lineEdit.TextChanged += (string newValue) =>
                    {
                        if (UpdateValue(newValue, m_Value))
                        {
                            NotifyChanged();
                        }
                        else
                        {
                            lineEdit.Text = m_Value.AsString();
                        }
                    };
                    lineEdit.Text = m_Value.AsString();

                    Content = lineEdit;
                    break;

                case Variant.Type.Int:
                case Variant.Type.Float:
                    SpinBox spinBox = new();
                    if (VariantType == Variant.Type.Float)
                    {
                        spinBox.Rounded = false;
                        spinBox.Step = 0.0001;
                    }
                    SetSpinBoxMinMax(spinBox, ManagedType);
                    spinBox.ValueChanged += (double newValue) =>
                    {
                        if (UpdateValue(newValue, m_Value))
                        {
                            NotifyChanged();
                        }
                        else
                        {
                            spinBox.SetValueNoSignal(newValue);
                        }
                    };
                    //Not needed because SetSpinBoxMinMax already set the value
                    //spinBox.Value = m_Value.AsDouble();
                    
                    Content = spinBox;
                    break;

                case Variant.Type.Bool:
                    CheckBox button = new()
                    {
                        ToggleMode = true
                    };
                    button.Pressed += () =>
                    {
                        if (UpdateValue(button.ButtonPressed, m_Value))
                        {
                            NotifyChanged();
                        }
                        else
                        {
                            button.SetPressedNoSignal(m_Value.AsBool());
                        }
                        button.Text = button.ButtonPressed.ToString();
                    };
                    button.ButtonPressed = m_Value.AsBool();
                    button.Text = button.ButtonPressed.ToString();
                    m_Value = button.ButtonPressed;
                    Content = button;
                    break;

                case Variant.Type.Color:
                    ColorPicker colorPicker = new();
                    colorPicker.ColorChanged += (Color newColor) =>
                    {
                        if (UpdateValue(newColor, m_Value))
                        {
                            NotifyChanged();
                        }
                        else
                        {
                            colorPicker.Color = m_Value.AsColor();
                        }
                    };
                    colorPicker.Color = m_Value.AsColor();
                    Content = colorPicker;
                    break;

                default:
                    EditorResourcePicker editorResourcePicker = new()
                    {
                        //This is not foolproof solution, some resources may not work as intended
                        BaseType = managedItemType.Name
                    };
                    editorResourcePicker.ResourceChanged += (Resource newResources) =>
                    {
                        if (UpdateValue(newResources, m_Value))
                        {
                            NotifyChanged();
                        }
                        else
                        {
                            editorResourcePicker.EditedResource = m_Value.As<Resource>();
                        }
                    };
                    editorResourcePicker.EditedResource = m_Value.As<Resource>();
                    Content = editorResourcePicker;
                    break;
            }
        }
        //Originally I disabled creating name but right now creating name is always set to true
        if (createName)
        {
            Label title = new()
            {
                Text = propertyName,
                Name = "Title",
                HorizontalAlignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin
            };
            Content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            Container.AddChild(title);
        }

        Content.CustomMinimumSize = new Vector2(150, 0);
        Container.AddChild(Content);
        AddChild(Container);
        return this;
    }

    private void SetSpinBoxMinMax(SpinBox spinBox, Type numberType, bool setValue = true)
    {
        //The spinbox will go crazy and will stuck at 0 if we set the max value too high or min value too low

        //It would be better to use switch case but for compability reason, older version might not have switch case on type so i am using the cavemen check

        if (numberType == typeof(float))
        {
            //Single
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsSingle();
        }

        else if (numberType == typeof(double))
        {
            //Double
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsDouble();
        }

        else if (numberType == typeof(short))
        {
            //Int16
            spinBox.MaxValue = short.MaxValue;
            spinBox.MinValue = short.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsInt16();
        }

        else if (numberType == typeof(int))
        {
            //Int32
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsInt32();
        }

        else if (numberType == typeof(long))
        {
            //Int64
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsInt64();
        }

        else if (numberType == typeof(sbyte))
        {
            //SByte
            spinBox.MaxValue = sbyte.MaxValue;
            spinBox.MinValue = sbyte.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsSByte();
        }

        else if (numberType == typeof(byte))
        {
            //Byte
            spinBox.MaxValue = byte.MaxValue;
            spinBox.MinValue = byte.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsByte();
        }

        else if (numberType == typeof(ushort))
        {
            //UInt16
            spinBox.MaxValue = ushort.MaxValue;
            spinBox.MinValue = ushort.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsUInt16();
        }

        else if (numberType == typeof(uint))
        {
            //UInt32
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsUInt32();
        }

        else if (numberType == typeof(ulong))
        {
            spinBox.MaxValue = int.MaxValue;
            spinBox.MinValue = int.MinValue;
            if (setValue)
                spinBox.Value = m_Value.AsUInt64();
        }

        else
        {
            GD.PrintErr($"Unimplemented number type {ManagedType}");
        }
    }

    #region ValueRefresher
    /// <summary>
    /// Please do not use this if possible
    /// </summary>
    /// <param name="newValue"></param>
    public void RefreshValue(Variant newValue)
    {
        m_Value = newValue;
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        EmitChanged(m_PropertyName, m_Value);
    }

    private bool UpdateValue(Variant newValue, Variant actualOldValue)
    {
        if (OnValueChanged != null)
        {
            return OnValueChanged(newValue, actualOldValue);
        }

        m_Value = actualOldValue;
        Godot.Collections.Dictionary dict = DropdownParent.PropertyData.dict;

        if (m_ItemType == ItemType.Key && dict.ContainsKey(m_Value))
        {
            if (dict.ContainsKey(newValue))
            {
                GD.PrintErr($"Already have same key {newValue}");
                return false;
            }

            //Remove old value then add it again with our new key, essentially just changing the key
            Variant oldValue = dict[m_Value];
            dict.Remove(m_Value);

            //Update each key and value references
            KeyValuePair<TypedDictionaryKVP, KeyValuePair<Variant, Variant>> typedItem = DropdownParent.Contents.Single(x => x.Value.Equals(KVP));
            DropdownParent.Contents[typedItem.Key] = KeyValuePair.Create(newValue, oldValue);
            typedItem.Key.KVP = DropdownParent.Contents[typedItem.Key];
            KVP = typedItem.Key.KVP;

            dict[newValue] = oldValue;
        }

        else if (m_ItemType == ItemType.Value)
        {
            dict[DropdownParent.Contents[this].Key] = newValue;
        }
        m_Value = newValue;
        return true;
    }
    #endregion

    #region Tensor
    private Control CreateTensorProperty(Variant.Type variantType)
    {
        GridContainer gridContainer = new();
        switch (variantType)
        {
            case Variant.Type.Vector2:
            case Variant.Type.Vector2I:
                gridContainer.Columns = 2;
                break;
            case Variant.Type.Vector3:
            case Variant.Type.Vector3I:
                gridContainer.Columns = 3;
                break;
            case Variant.Type.Vector4:
            case Variant.Type.Vector4I:
                gridContainer.Columns = 4;
                break;
            case Variant.Type.Rect2:
            case Variant.Type.Rect2I:
                gridContainer.Columns = 3;
                break;
            case Variant.Type.Projection:
                gridContainer.Columns = 4;
                break;
            case Variant.Type.Aabb:
                gridContainer.Columns = 2;
                break;
            case Variant.Type.Basis:
                gridContainer.Columns = 3;
                break;
            case Variant.Type.Plane:
                gridContainer.Columns = 4;
                break;
            case Variant.Type.Quaternion:
                gridContainer.Columns = 4;
                break;
        }
        gridContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        gridContainer.SizeFlagsVertical = SizeFlags.ExpandFill;

        //Create a local method so that we can keep reusing it while also only exist within this
        void CreateTextBox(Type numberType, double defaultValue, int index, string key, string additionalField = "", string overrideColor = "")
        {
            HBoxContainer widthContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            SpinBox spinbox = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            SetSpinBoxMinMax(spinbox, numberType, false);
            if (GD.TypeToVariantType(numberType) == Variant.Type.Float)
            {
                spinbox.Rounded = false;
                spinbox.Step = 0.0001;
            }
            spinbox.Value = defaultValue;
            spinbox.ShowBehindParent = true;

            Label labelIndex = new();

            widthContainer.AddChild(labelIndex);
            widthContainer.AddChild(spinbox);
            gridContainer.AddChild(widthContainer);

            labelIndex.Text = key;
            string colorName = string.IsNullOrWhiteSpace(overrideColor) ? $"property_color_{key.Replace("d", "w")}" : overrideColor;
            if (!EditorInterface.Singleton.GetEditorTheme().HasColor(colorName, "Editor"))
            {
                GD.PrintErr($"Unable to find color property named {colorName}");
            }
            labelIndex.SelfModulate = EditorInterface.Singleton.GetEditorTheme().GetColor(colorName, "Editor");
            labelIndex.HorizontalAlignment = HorizontalAlignment.Center;

            Panel panelBG = new()
            {
                ShowBehindParent = true,
                Modulate = new Color(0.75f, 0.75f, 0.75f, 1)
            };
            panelBG.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            labelIndex.AddChild(panelBG);

            double previousTensorValue = spinbox.Value;
            spinbox.ValueChanged += (x) =>
            {
                Variant oldValue = m_Value;
                OnTensorValueChanged(index, x, additionalField);
                if (UpdateValue(m_Value, oldValue))
                {
                    previousTensorValue = x;
                    NotifyChanged();
                }
                else
                {
                    spinbox.SetValueNoSignal(previousTensorValue);
                    m_Value = oldValue;
                }
            };

        }
        //Most of these are only minor changes but i decided to go with cavemen route again for compability
        switch (variantType)
        {
            case Variant.Type.Vector2:
                Vector2 vector2 = m_Value.AsVector2();
                CreateTextBox(typeof(float), vector2.X, 0, "x");
                CreateTextBox(typeof(float), vector2.Y, 1, "y");
                break;
            case Variant.Type.Vector2I:
                Vector2I vector2I = m_Value.AsVector2I();
                CreateTextBox(typeof(int), vector2I.X, 0, "x");
                CreateTextBox(typeof(int), vector2I.Y, 1, "y");
                break;
            case Variant.Type.Vector3:
                Vector3 vector3 = m_Value.AsVector3();
                CreateTextBox(typeof(float), vector3.X, 0, "x");
                CreateTextBox(typeof(float), vector3.Y, 1, "y");
                CreateTextBox(typeof(float), vector3.Z, 2, "z");
                break;
            case Variant.Type.Vector3I:
                Vector3I vector3I = m_Value.AsVector3I();
                CreateTextBox(typeof(int), vector3I.X, 0, "x");
                CreateTextBox(typeof(int), vector3I.Y, 1, "y");
                CreateTextBox(typeof(int), vector3I.Z, 2, "z");
                break;
            case Variant.Type.Vector4:
                Vector4 vector4 = m_Value.AsVector4();
                CreateTextBox(typeof(float), vector4.X, 0, "x");
                CreateTextBox(typeof(float), vector4.Y, 1, "y");
                CreateTextBox(typeof(float), vector4.Z, 2, "z");
                CreateTextBox(typeof(float), vector4.W, 3, "w");
                break;
            case Variant.Type.Vector4I:
                Vector4I vector4I = m_Value.AsVector4I();
                CreateTextBox(typeof(int), vector4I.X, 0, "x");
                CreateTextBox(typeof(int), vector4I.Y, 1, "y");
                CreateTextBox(typeof(int), vector4I.Z, 2, "z");
                CreateTextBox(typeof(int), vector4I.W, 3, "w");
                break;
            case Variant.Type.Rect2:
                Rect2 rect2 = m_Value.AsRect2();
                CreateTextBox(typeof(float), rect2.Position.X, 0, "x", "Position");
                CreateTextBox(typeof(float), rect2.Position.Y, 1, "y", "Position");
                CreateTextBox(typeof(float), rect2.Size.X, 0, "w", "Size", "property_color_w");
                CreateTextBox(typeof(float), rect2.Size.Y, 1, "h", "Size", "property_color_z");
                break;
            case Variant.Type.Rect2I:
                Rect2I rect2I = m_Value.AsRect2I();
                CreateTextBox(typeof(int), rect2I.Position.X, 0, "x", "Position");
                CreateTextBox(typeof(int), rect2I.Position.Y, 1, "y", "Position");
                CreateTextBox(typeof(int), rect2I.Size.X, 0, "w", "Size", "property_color_w");
                CreateTextBox(typeof(int), rect2I.Size.Y, 1, "h", "Size", "property_color_z");
                break;
            case Variant.Type.Projection:
                Projection projection = m_Value.AsProjection();
                CreateTextBox(typeof(float), projection.X.X, 0, "xx", "X");
                CreateTextBox(typeof(float), projection.X.Y, 1, "xy", "X");
                CreateTextBox(typeof(float), projection.X.Z, 2, "xz", "X");
                CreateTextBox(typeof(float), projection.X.W, 3, "xw", "X");

                CreateTextBox(typeof(float), projection.Y.X, 0, "yx", "Y");
                CreateTextBox(typeof(float), projection.Y.Y, 1, "yy", "Y");
                CreateTextBox(typeof(float), projection.Y.Z, 2, "yz", "Y");
                CreateTextBox(typeof(float), projection.Y.W, 3, "yw", "Y");

                CreateTextBox(typeof(float), projection.Z.X, 0, "zx", "Z");
                CreateTextBox(typeof(float), projection.Z.Y, 1, "zy", "Z");
                CreateTextBox(typeof(float), projection.Z.Z, 2, "zz", "Z");
                CreateTextBox(typeof(float), projection.Z.W, 3, "zw", "Z");

                CreateTextBox(typeof(float), projection.W.X, 0, "wx", "W");
                CreateTextBox(typeof(float), projection.W.Y, 1, "wy", "W");
                CreateTextBox(typeof(float), projection.W.Z, 2, "wz", "W");
                CreateTextBox(typeof(float), projection.W.W, 3, "ww", "W");
                break;
            case Variant.Type.Aabb:
                Aabb aabb = m_Value.AsAabb();
                CreateTextBox(typeof(float), aabb.Position.X, 0, "x", "Position");
                CreateTextBox(typeof(float), aabb.Position.Y, 1, "y", "Position");
                CreateTextBox(typeof(float), aabb.Position.Z, 2, "z", "Position");

                CreateTextBox(typeof(float), aabb.Size.X, 0, "w", "Size", "property_color_w");
                CreateTextBox(typeof(float), aabb.Size.X, 1, "h", "Size", "property_color_y");
                CreateTextBox(typeof(float), aabb.Size.X, 2, "d", "Size", "property_color_z");
                break;
            case Variant.Type.Basis:
                Basis basis = m_Value.AsBasis();
                CreateTextBox(typeof(float), basis.X.X, 0, "xx", "X");
                CreateTextBox(typeof(float), basis.X.Y, 1, "xy", "X");
                CreateTextBox(typeof(float), basis.X.Z, 2, "xz", "X");

                CreateTextBox(typeof(float), basis.Y.X, 0, "yx", "Y");
                CreateTextBox(typeof(float), basis.Y.Y, 1, "yy", "Y");
                CreateTextBox(typeof(float), basis.Y.Z, 2, "yz", "Y");

                CreateTextBox(typeof(float), basis.Z.X, 0, "zx", "Z");
                CreateTextBox(typeof(float), basis.Z.Y, 1, "zy", "Z");
                CreateTextBox(typeof(float), basis.Z.Z, 2, "zz", "Z");
                break;
            case Variant.Type.Plane:
                Plane plane = m_Value.AsPlane();
                CreateTextBox(typeof(float), plane.X, 0, "x", "X");
                CreateTextBox(typeof(float), plane.Y, 1, "y", "Y");
                CreateTextBox(typeof(float), plane.Z, 2, "z", "Z");
                CreateTextBox(typeof(float), plane.D, 3, "d", "D", "property_color_w");
                break;
            case Variant.Type.Quaternion:
                Quaternion quat = m_Value.AsQuaternion();
                CreateTextBox(typeof(float), quat.X, 0, "x", "X");
                CreateTextBox(typeof(float), quat.Y, 1, "y", "Y");
                CreateTextBox(typeof(float), quat.Z, 2, "z", "Z");
                CreateTextBox(typeof(float), quat.W, 3, "w", "W");
                break;
        }
        return gridContainer;
    }

    //Wtf is this
    //TODO: Should change to cavemen route for compability
    public bool CanCastToTensor()
    {
        return VariantType switch
        {
            Variant.Type.Vector2 or Variant.Type.Vector2I or Variant.Type.Vector3 or Variant.Type.Vector3I or Variant.Type.Vector4 or Variant.Type.Vector4I or Variant.Type.Rect2 or Variant.Type.Rect2I or Variant.Type.Projection or Variant.Type.Aabb or Variant.Type.Basis or Variant.Type.Plane or Variant.Type.Quaternion => true,
            _ => false,
        };
    }
    public void CreateDefaultTensorValue()
    {
        switch (VariantType)
        {
            case Variant.Type.Vector2:
                m_Value = Variant.CreateFrom(new Vector2());
                break;
            case Variant.Type.Vector2I:
                m_Value = Variant.CreateFrom(new Vector2I());
                break;
            case Variant.Type.Vector3:
                m_Value = Variant.CreateFrom(new Vector3());
                break;
            case Variant.Type.Vector3I:
                m_Value = Variant.CreateFrom(new Vector3I());
                break;
            case Variant.Type.Vector4:
                m_Value = Variant.CreateFrom(new Vector4());
                break;
            case Variant.Type.Vector4I:
                m_Value = Variant.CreateFrom(new Vector4I());
                break;
            case Variant.Type.Rect2:
                m_Value = Variant.CreateFrom(new Rect2());
                break;
            case Variant.Type.Rect2I:
                m_Value = Variant.CreateFrom(new Rect2I());
                break;
            case Variant.Type.Projection:
                m_Value = Variant.CreateFrom(new Projection());
                break;
            case Variant.Type.Aabb:
                m_Value = Variant.CreateFrom(new Aabb());
                break;
            case Variant.Type.Basis:
                m_Value = Variant.CreateFrom(new Basis());
                break;
            case Variant.Type.Plane:
                m_Value = Variant.CreateFrom(new Plane());
                break;
            case Variant.Type.Quaternion:
                m_Value = Variant.CreateFrom(new Quaternion());
                break;
            default:
                GD.PrintErr($"Unimplemented CreateDefaultStructValue on type {VariantType}");
                break;
        }
    }

    //When any of the Tensor value is changed, we cast the value then set the index from the Struct to the updated value
    private void OnTensorValueChanged(int index, double value, string additionalField)
    {
        Variant result = default;
        switch (VariantType)
        {
            case Variant.Type.Vector2:
                Vector2 vector2 = m_Value.AsVector2();
                vector2[index] = (float)value;
                result = vector2;
                break;

            case Variant.Type.Vector2I:
                Vector2I vector2i = m_Value.AsVector2I();
                vector2i[index] = (int)value;
                result = vector2i;
                break;

            case Variant.Type.Vector3:
                Vector3 vector3 = m_Value.AsVector3();
                vector3[index] = (float)value;
                result = vector3;
                break;

            case Variant.Type.Vector3I:
                Vector3I vector3i = m_Value.AsVector3I();
                vector3i[index] = (int)value;
                result = vector3i;
                break;

            case Variant.Type.Vector4:
                Vector4 vector4 = m_Value.AsVector4();
                vector4[index] = (float)value;
                result = vector4;
                break;

            case Variant.Type.Vector4I:
                Vector4I vector4i = m_Value.AsVector4I();
                vector4i[index] = (int)value;
                result = vector4i;
                break;

            case Variant.Type.Rect2:
                Rect2 rect2 = m_Value.AsRect2();
                switch (additionalField)
                {
                    case "Position":
                        Vector2 rect2Position = rect2.Position;
                        rect2Position[index] = (float)value;
                        break;

                    case "Size":
                        Vector2 rect2Size = rect2.Size;
                        rect2Size[index] = (float)value;
                        break;
                }
                result = rect2;
                break;

            case Variant.Type.Rect2I:
                Rect2I rect2i = m_Value.AsRect2I();
                switch (additionalField)
                {
                    case "Position":
                        Vector2I rect2Position = rect2i.Position;
                        rect2Position[index] = (int)value;
                        break;

                    case "Size":
                        Vector2I rect2Size = rect2i.Size;
                        rect2Size[index] = (int)value;
                        break;
                }
                result = rect2i;
                break;

            case Variant.Type.Projection:
                Projection projection = m_Value.AsProjection();
                switch (additionalField)
                {
                    case "X":
                        Vector4 projectionX = projection.X;
                        projectionX[index] = (float)value;
                        break;

                    case "Y":
                        Vector4 projectionY = projection.Y;
                        projectionY[index] = (float)value;
                        break;

                    case "Z":
                        Vector4 projectionZ = projection.Z;
                        projectionZ[index] = (float)value;
                        break;

                    case "W":
                        Vector4 projectionW = projection.W;
                        projectionW[index] = (float)value;
                        break;
                }
                result = projection;
                break;

            case Variant.Type.Aabb:
                Aabb aabb = m_Value.AsAabb();
                switch (additionalField)
                {
                    case "Position":
                        Vector3 aabbPosition = aabb.Position;
                        aabbPosition[index] = (float)value;
                        break;

                    case "Size":
                        Vector3 aabbSize = aabb.Position;
                        aabbSize[index] = (float)value;
                        break;
                }
                result = aabb;
                break;

            case Variant.Type.Basis:
                Basis basis = m_Value.AsBasis();
                switch (additionalField)
                {
                    case "X":
                        Vector3 basisX = basis.X;
                        basisX[index] = (float)value;
                        break;

                    case "Y":
                        Vector3 basisY = basis.Y;
                        basisY[index] = (float)value;
                        break;

                    case "Z":
                        Vector3 basisZ = basis.Z;
                        basisZ[index] = (float)value;
                        break;
                }
                result = basis;
                break;

            case Variant.Type.Plane:
                Plane plane = m_Value.AsPlane();
                switch (additionalField)
                {
                    case "X":
                        plane.X = (float)value;
                        break;

                    case "Y":
                        plane.Y = (float)value;
                        break;

                    case "Z":
                        plane.Z = (float)value;
                        break;

                    case "D":
                        plane.D = (float)value;
                        break;
                }
                result = plane;
                break;

            case Variant.Type.Quaternion:
                Quaternion quaternion = m_Value.AsQuaternion();
                quaternion[index] = (float)value;
                result = quaternion;
                break;
            default:
                GD.PrintErr($"Unimplemented OnTensorValueChange on type {VariantType}");
                break;
        }

        m_Value = result;
    }
    #endregion


    public Variant GetValue()
    {
        if (Content is LineEdit lineEdit)
            return lineEdit.Text;

        if (Content is SpinBox spinBox)
            return spinBox.Value;

        if (Content is OptionButton optionButton)
            return optionButton.Selected;

        if (Content is Button button)
            return button.ButtonPressed;

        if (Content is ColorPicker colorPicker)
            return colorPicker.Color;

        if (Content is EditorResourcePicker editorResourcePicker)
            return editorResourcePicker.EditedResource;

        if (VariantType == Variant.Type.Dictionary)
            return m_Value.AsGodotDictionary();

        if (VariantType == Variant.Type.Array)
            return m_Value.AsGodotArray();

        if (CanCastToTensor())
            return m_Value;

        GD.PrintErr($"Unknown value type {VariantType}");
        return default;
    }

    //This is currently unused, it supposed to trigger when the user press Clear button, but i dont know how to add Clear button right now
    public void ResetValue()
    {
        if (Content is LineEdit lineEdit)
            lineEdit.Text = string.Empty;
        if (Content is SpinBox spinBox)
            spinBox.Value = 0;
        if (Content is Button button)
            button.Text = string.Empty;
        if (Content is ColorPicker colorPicker)
            colorPicker.Color = new Color(1, 1, 1, 1);
        if (Content is EditorResourcePicker editorResourcePicker)
            editorResourcePicker.EditedResource = null;
        if (CanCastToTensor())
            CreateDefaultTensorValue();
    }
}