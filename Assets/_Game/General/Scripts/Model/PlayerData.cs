using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public List<ResourceData> Resources;

    public List<string> UnlockedStateIds;
    public List<string> PurchasedCityIds;

    public bool IsFirstSession;
}