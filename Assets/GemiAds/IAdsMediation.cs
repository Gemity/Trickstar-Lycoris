using System;

namespace Gemi.AdsManager
{
    public interface IAdsMediation
    {
        void Initialize(Action<bool> onInitialized);
        void SetConsent(ConsentStatus status);
        void SetUserId(string userId);

        #region Banner
        void LoadBanner(string placement, BannerPosition position = BannerPosition.Bottom, BannerSize size = BannerSize.FullBanner, bool adaptive = true, bool collapse = false);
        void ShowBanner(string placement);
        void HideBanner(string placement);
        void DestroyBanner(string placement);
        bool IsBannerLoaded();
        bool IsBannerShowing();
        #endregion




        // Optional: revenue callback (eCPM, currency micros, etc.)
        event Action<string /*placement*/, double /*value*/, string /*currency*/> OnPaidEvent;

        // Optional: low-level errors
        event Action<string /*placement*/, string /*error*/> OnLoadFailed;
        event Action<string /*placement*/, string /*error*/> OnShowFailed;
    }
}
