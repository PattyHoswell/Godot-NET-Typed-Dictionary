using Godot;
using System.Linq;

namespace TypedDictionaryProject.addons.TypedDictionary;
[GlobalClass]
[Tool]
public partial class TypedDictionaryArrayButtonMover : Button
{
    public GodotObject EditingObject;
    public HBoxContainer WidthSeparator;
    public TypedDictionaryBase Contents;
    public Label LabelIndex;
    public TypedDictionaryArray ArrayParent;
    public string TargetPropertyName;
    public int CurrentIndex;
    public Variant DragData, CurrentData;

    public override void _Ready()
    {
        base._Ready();
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        CustomMinimumSize = new Vector2(20, 20);
        TooltipText = "Move Item Index";
        TextureRect texture = new();
        texture.SetAnchorsPreset(LayoutPreset.FullRect);
        texture.Texture = EditorInterface.Singleton.GetEditorTheme().GetIcon("TripleBar", "EditorIcons");
        Text = " ";
        MouseEntered += () =>
        {
            //Sort of mimicking the behaviour of when you hover into Array Indexer, the mouse shape will change to drag
            MouseDefaultCursorShape = CursorShape.Drag;
        };
        AddChild(texture);
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.ToString().StartsWith("TypedDictionaryArray") && data.ToString() != DragData.ToString())
        {
            string[] split = data.ToString().Split('_');
            if (split[1] == TargetPropertyName)
            {
                return true;
            }
        }
        return false;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        string[] split = data.ToString().Split('_');

        int targetIndex = int.Parse(split[2]);
        TypedDictionaryArrayButtonMover buttonMover = ArrayParent.ButtonMover[targetIndex];

        if (buttonMover.WidthSeparator.GetIndex() == WidthSeparator.GetIndex())
        {
            return;
        }
        Node dropdownParent = buttonMover.WidthSeparator.GetParent();

        int targetChildIndex = buttonMover.WidthSeparator.GetIndex();
        dropdownParent.MoveChild(buttonMover.WidthSeparator, WidthSeparator.GetIndex());
        dropdownParent.MoveChild(WidthSeparator, targetChildIndex);

        //This should be moved to TypedDictionaryArray instead, it basically just refresh everything to be in the correct order
        foreach (Node widthSeparator in dropdownParent.GetChildren().OrderBy(x => x.GetIndex()))
        {
            int currentIndex = widthSeparator.GetIndex();
            foreach (Node indexSeparator in widthSeparator.GetChildren())
            {
                if (indexSeparator.Name != "IndexSeparator")
                    continue;

                foreach (Node item in indexSeparator.GetChildren())
                {
                    if (item is TypedDictionaryArrayButtonMover buttonMover1)
                    {
                        ArrayParent.ItemArray[currentIndex] = buttonMover1.CurrentData;
                        buttonMover1.CurrentIndex = currentIndex;
                    }
                    if (item is Label labelText)
                    {
                        labelText.Text = currentIndex.ToString();
                    }
                }
            }
        }
        ArrayParent.EmitChanged(TargetPropertyName, ArrayParent.AttachedDictionary);

    }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview(WidthSeparator.Duplicate() as Control);
        return DragData;
    }
}
