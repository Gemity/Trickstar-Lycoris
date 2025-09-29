using Gemi.AdsManager;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MockAdsManager : MonoBehaviour, IAdsMediation
{
    [SerializeField] private GameObject _banner;

    public string ProviderId => throw new NotImplementedException();

    public event Action<string, double, string> OnPaidEvent;
    public event Action<string, string> OnLoadFailed;
    public event Action<string, string> OnShowFailed;

    private Dictionary<AdType, AdInfo> _adsCollected = new();

    public void Initialize(Action<bool> onInitialized)
    {
        onInitialized?.Invoke(true);
    }

    #region General
    public void SetConsent(ConsentStatus status)
    {
        // Mock implementation, do nothing
    }

    public void SetUserId(string userId)
    {
        // Mock implementation, do nothing
    }

    public bool IsAdLoaded(AdType adType)
    {
        if (_adsCollected.TryGetValue(adType, out AdInfo adInfo))
        {
            return adInfo.adStatus == AdStatus.Loaded;
        }
        return false;
    }
    
    public bool IsAdShowing(AdType adType)
    {
        if (_adsCollected.TryGetValue(adType, out AdInfo adInfo))
        {
            return adInfo.adStatus == AdStatus.Showing;
        }
        return false;
    }



    #endregion

    #region Banner
    public void LoadBanner()
    {
        BannerInfo bannerInfo = new ()
        {
            adType = AdType.Banner,
            adNetwork = AdNetwork.AdMob,
            adUnitId = "mock-banner",
            adStatus = AdStatus.Loading,
            retryAttempt = 0,
            adObject = _banner
        };

        _adsCollected.Add(AdType.Banner, bannerInfo);
    }

    public void ShowBanner(string placement)
    {
        if (_adsCollected.TryGetValue(AdType.Banner, out AdInfo adInfo))
        {
            if (adInfo is BannerInfo bannerInfo && bannerInfo.adObject != null)
            {
                bannerInfo.adStatus = AdStatus.Showing;
                (bannerInfo.adObject as GameObject).SetActive(true);
            }
        }
    }

    public void HideBanner(string placement)
    {
        if (_adsCollected.TryGetValue(AdType.Banner, out AdInfo adInfo))
        {
            if (adInfo is BannerInfo bannerInfo && bannerInfo.adObject != null)
            {
                bannerInfo.adStatus = AdStatus.Hide;
                (bannerInfo.adObject as GameObject).SetActive(false);
            }
        }
    }

    public void DestroyBanner(string placement)
    {
        if (_adsCollected.TryGetValue(AdType.Banner, out AdInfo adInfo))
        {
            if (adInfo is BannerInfo bannerInfo && bannerInfo.adObject != null)
            {
                bannerInfo.adStatus = AdStatus.NotSet;
                (bannerInfo.adObject as GameObject).SetActive(false);
                _adsCollected.Remove(AdType.Banner);
            }
        }
    }

    public bool IsBannerLoaded()
    {
        return IsAdLoaded(AdType.Banner);
    }

    public bool IsBannerShowing()
    {
        return IsAdShowing(AdType.Banner);
    }
    #endregion

    public bool IsLoaded(string placement)
    {
        throw new NotImplementedException();
    }

    public void Load(string placement)
    {
        throw new NotImplementedException();
    }

    public void Show(string placement, Action<AdResult> onClosed)
    {
        throw new NotImplementedException();
    }
}
