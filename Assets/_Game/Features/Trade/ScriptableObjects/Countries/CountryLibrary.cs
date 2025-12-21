using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CountryLibrary", menuName = "Game/Trade/Country Library")]
public class CountryLibrary : ScriptableObject
{
    [SerializeField] private List<CountryDefinition> _definitions;

    private Dictionary<CountryId, CountryDefinition> _lookup;

    public CountryDefinition GetDef(CountryId id)
    {
        if (_lookup == null || _lookup.Count == 0) UpdateLookup();

        if (_lookup.TryGetValue(id, out var def))
        {
            return def;
        }

        Debug.LogError($"Country Definition not found for id: {id}");
        return null;
    }

    private void UpdateLookup()
    {
        _lookup = new Dictionary<CountryId, CountryDefinition>();
        foreach (var def in _definitions)
        {
            if (def != null && !_lookup.ContainsKey(def.Id))
            {
                _lookup.Add(def.Id, def);
            }
        }
    }
}
