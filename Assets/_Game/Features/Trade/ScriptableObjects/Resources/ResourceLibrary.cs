using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceLibrary", menuName = "Game/Resources/Library")]
public class ResourceLibrary : ScriptableObject
{
    [SerializeField] private List<ResourceDefinition> _definitions;

    private Dictionary<ResourceType, ResourceDefinition> _lookup;

    public ResourceDefinition GetDef(ResourceType type)
    {
        if (_lookup == null || _lookup.Count == 0) UpdateLookup();

        if (_lookup.TryGetValue(type, out var def))
        {
            return def;
        }

        Debug.LogError($"Resource Definition not found for type: {type}");
        return null;
    }

    private void UpdateLookup()
    {
        _lookup = new Dictionary<ResourceType, ResourceDefinition>();
        foreach (var def in _definitions)
        {
            if (def != null && !_lookup.ContainsKey(def.Type))
            {
                _lookup.Add(def.Type, def);
            }
        }
    }
}