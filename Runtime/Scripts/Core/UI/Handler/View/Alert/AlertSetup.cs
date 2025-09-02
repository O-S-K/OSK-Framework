using System;

namespace OSK
{
    [System.Serializable]
    public class AlertSetup
    {
        public string title = "";
        public string message = ""; 
        public Action onOk = null;
        public Action onCancel = null;
        public float timeHide = 0;
    }
}