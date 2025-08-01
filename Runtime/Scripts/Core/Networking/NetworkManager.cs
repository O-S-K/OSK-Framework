using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OSK
{
    public class NetworkManager : GameFrameworkComponent
    {
        public InternetChecker InternetChecker { get; private set; }
        public bool IsOnline;

        public override void OnInit() {}
 
        private async void Start()
        {
            try
            {
                InternetChecker = gameObject.GetOrAdd<InternetChecker>();
                IsOnline = await InternetChecker.CheckNetwork();
                OSK.Logg.Log($"Is online: {IsOnline}");
            }
            catch (Exception e)
            {
                OSK.Logg.LogError($"NetworkManager Start error: {e.Message}");
            }
        }
    }
}