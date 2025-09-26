 using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemi.AdsManager
{

    [Serializable]
    public class PlacementConfig
    {
        public string name;
        public AdType adType;
        public string androidAdUnitId;
        public string iosAdUnitId;
    }

    [CreateAssetMenu(menuName = "Ads/Ads Settings")]
    public class AdsSettings : ScriptableObject
    {
        public string activeProviderId;         // "admob", "max", "ironsource", "mock"
        public List<PlacementConfig> placements = new();
        public bool enablePaidEvent = true;
        public bool childDirected = false;      // COPPA tagged
        public bool underAgeOfConsent = false;
    }
}