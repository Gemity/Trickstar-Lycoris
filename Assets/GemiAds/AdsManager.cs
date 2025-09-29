using System;
using UnityEngine;

namespace Gemi.AdsManager
{
    public enum AdType { NotSet, Banner, Interstitial, Rewarded, OpenAd }
    public enum AdNetwork { NotSet, AdMob, UnityAds, AppLovin, IronSource }
    public enum AdStatus { NotSet, Loading, Loaded, Failed, Showing, Hide, Completed }
    public enum AdResult { Completed, Skipped, Failed }
    public enum ConsentStatus { Unknown, Granted, Denied }
    public enum BannerPosition { Top, Bottom }
    public enum BannerSize { Banner, LargeBanner, MediumRectangle, FullBanner, Leaderboard, SmartBanner }

    public class AdsManager : MonoBehaviour
    {
        [SerializeField] private GameObject _shield;

        private static AdsManager _instance;
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var prefab = Resources.Load<AdsManager>("AdsManager");
                    if (prefab != null)
                    {
                        _instance = Instantiate(prefab);
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                    else
                    {
                        Debug.LogError("AdsManager prefab not found in Resources folder.");
                    }
                }

                return _instance;
            }
        }

        private bool _isInitialized = false;
        private IAdsMediation _adsMediation;

        public Action<bool> OnInitalize;
        public Action<bool> OnToggleLoading;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _adsMediation = Resources.Load(typeof(IAdsMediation).Name) as IAdsMediation;
            if (_adsMediation == null)
            {
                Debug.LogError("Ads Mediation not found");
                OnInitalize?.Invoke(false);
                return;
            }

            _adsMediation.Initialize(success =>
            {
                _isInitialized = success;
                OnInitalize?.Invoke(success);
            });
        }
    }
}


