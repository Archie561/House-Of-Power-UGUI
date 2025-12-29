using System;
using UnityEngine;

public class UpgradeStoragePopupData
{
    public UpgradeStorageData UpgradeData;
    public bool CanAffordDefault;
    public bool CanAffordPremium;
    public Action OnDefaultClick;
    public Action OnPremiumClick;

    public UpgradeStoragePopupData(UpgradeStorageData upgradeData, bool canAffordDefault, bool canAffordPremium, Action onDefaultClick, Action onPremiumClick)
    {
        UpgradeData = upgradeData;
        CanAffordDefault = canAffordDefault;
        CanAffordPremium = canAffordPremium;
        OnDefaultClick = onDefaultClick;
        OnPremiumClick = onPremiumClick;
    }
}
