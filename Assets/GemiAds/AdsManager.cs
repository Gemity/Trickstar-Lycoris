using UnityEngine;



namespace Gemi.AdsManager
{
    public class AdsManager
    {
        private static AdsManager _instance;
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ();
                }
                return _instance;
            }
        }
    }
}


