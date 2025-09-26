using Gemi.AdsManager;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MockAdsManager : MonoBehaviour, IAdsMediation
{
    [SerializeField] private RectTransform _banner;

    public string ProviderId => throw new NotImplementedException();

    public event Action<string, double, string> OnPaidEvent;
    public event Action<string, string> OnLoadFailed;
    public event Action<string, string> OnShowFailed;

    private Dictionary<AdType, AdInfo> _adsCollected = new();

    public void Initialize(Action<bool> onInitialized)
    {
        onInitialized?.Invoke(true);
    }

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
    }

    #endregion

    public void DestroyBanner(string placement)
    {
        throw new NotImplementedException();
    }

    public void HideBanner(string placement)
    {
        throw new NotImplementedException();
    }

    public bool IsLoaded(string placement)
    {
        throw new NotImplementedException();
    }

    public void Load(string placement)
    {
        throw new NotImplementedException();
    }

    public void SetConsent(ConsentStatus status)
    {
        throw new NotImplementedException();
    }

    public void SetUserId(string userId)
    {
        throw new NotImplementedException();
    }

    public void Show(string placement, Action<AdResult> onClosed)
    {
        throw new NotImplementedException();
    }

    public void ShowBanner(string placement)
    {
        throw new NotImplementedException();
    }
}
