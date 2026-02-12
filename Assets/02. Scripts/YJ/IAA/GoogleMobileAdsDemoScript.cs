using GoogleMobileAds;
using GoogleMobileAds.Api;
using UnityEngine;

public class GoogleMobileAdsDemoScript : MonoBehaviour
{
    public void Awake()
    {
        // Initialize Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // This callback is called once the MobileAds SDK is initialized.
        });
    }


}