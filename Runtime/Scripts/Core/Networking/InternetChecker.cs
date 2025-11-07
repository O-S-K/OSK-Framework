#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OSK
{
    public class InternetChecker : MonoBehaviour
    {
        private readonly List<string> _theListOfIPs = new() { "4.2.2.4", "www.unity.com" };
        private int _counter;
        [SerializeField] private float Timeout;

        public async Task<bool> CheckNetwork()
        {
            var internetPossiblyAvailable = Application.internetReachability switch
            {
                NetworkReachability.ReachableViaCarrierDataNetwork => true,
                NetworkReachability.ReachableViaLocalAreaNetwork => true,
                NetworkReachability.NotReachable => false,
                _ => false
            };

            if (!internetPossiblyAvailable)
            {
                return false;
            }

            return await CheckPing();
        }

        private async Task<bool> CheckPing()
        {
            _counter = 0;
            var tcs = new TaskCompletionSource<bool>();
            Ping(tcs).Run();
            await tcs.Task;
            return tcs.Task.Result;
        }

        private IEnumerator Ping(TaskCompletionSource<bool> task)
        {
            var ping = new Ping(_theListOfIPs[_counter]);
            while (!ping.isDone || Timeout > 0)
            {
                Timeout -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (!ping.isDone)
            {
                _counter++;
                if (_counter >= _theListOfIPs.Count)
                {
                    task.SetResult(false);
                }
                else
                     Ping(task).Run();
            }
            else
            {
                task.SetResult(true);
            }
        }

        public bool IsConnected()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
#endif