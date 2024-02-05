using Godot;
using System;
using System.Collections.Generic;

namespace TypedDictionaryProject.addons.TypedDictionary;
public partial class TypedDictionaryKVP : EditorProperty
{

    /// <summary>
    /// Unused right now
    /// </summary>
    public event Action<KeyValuePair<Variant, Variant>, KeyValuePair<Variant, Variant>> OnKVPChanged;
    private KeyValuePair<Variant, Variant> m_KVP;
    public KeyValuePair<Variant, Variant> KVP
    {
        get => m_KVP;
        set
        {
            KeyValuePair<Variant, Variant> oldVariant = m_KVP;
            m_KVP = value;
            OnKVPChanged?.Invoke(oldVariant, m_KVP);
        }
    }
}
