using Godot;

/// <summary>
/// If you want your class to show on the Dictionary or Array inside Dictionary
/// <para>You need to add <see cref="GlobalClassAttribute"/> attribute and <see cref="ToolAttribute"/></para>
/// 
/// <para>The class might not display properly as I don't seem to find a way to draw the default property</para>
/// <para>You will have to implement your own way of how your class will display</para>
/// This is Godot Engine limitation
/// </summary>
[GlobalClass]
[Tool]
public partial class TestResources : Resource
{
    public string TestString;
}
