using UnityEngine;

namespace Gemi.AdsManager
{
    public class AdInfo
    {
        public AdType adType = AdType.NotSet;
        public AdNetwork adNetwork = AdNetwork.NotSet;
        public string adUnitId = "";
        public AdStatus adStatus = AdStatus.NotSet;
        public int retryAttempt = 0;
        public object adObject = null; // Placeholder for actual ad object from SDK
    }

    public class BannerInfo : AdInfo
    {
        public BannerSize size = BannerSize.Banner;
        public BannerPosition position = BannerPosition.Bottom;
        public bool collapse = false;
        public bool adaptive = false;
    }

    public class InterstitialInfo : AdInfo
    {

    }

    public class RewardedInfo : AdInfo
    {

    }
}