using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemi.AdsManager
{
    public enum AdType { NotSet, Banner, Interstitial, Rewarded, OpenAd }
    public enum AdNetwork { NotSet, AdMob, UnityAds, AppLovin, IronSource }
    public enum AdStatus { NotSet, Loading, Loaded, Failed, Showing, Completed }
    public enum AdResult { Completed, Skipped, Failed }
    public enum ConsentStatus { Unknown, Granted, Denied }

    public class AdInfo
    {
        public AdType adType = AdType.NotSet;
        public AdNetwork adNetwork = AdNetwork.NotSet;
        public string adUnitId = "";
        public AdStatus adStatus = AdStatus.NotSet;
        public object adObject = null; // Placeholder for actual ad object from SDK
    }

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