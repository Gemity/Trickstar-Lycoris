using System;

namespace Gemi.AdsManager
{
    public interface IAdsMediation
    {
        // Adapter identity
        string ProviderId { get; }

        // Lifecycle
        void Initialize(Action<bool> onInitialized);
        void SetConsent(ConsentStatus status);
        void SetUserId(string userId);

        #region Banner
        void LoadBanner();
        void ShowBanner(string placement);
        void HideBanner(string placement);
        void DestroyBanner(string placement);
        #endregion

        // Loading
        bool IsLoaded(string placement);
        void Load(string placement);

        // Showing
        void Show(string placement, Action<AdResult> onClosed);


        // Optional: revenue callback (eCPM, currency micros, etc.)
        event Action<string /*placement*/, double /*value*/, string /*currency*/> OnPaidEvent;

        // Optional: low-level errors
        event Action<string /*placement*/, string /*error*/> OnLoadFailed;
        event Action<string /*placement*/, string /*error*/> OnShowFailed;
    }
}
