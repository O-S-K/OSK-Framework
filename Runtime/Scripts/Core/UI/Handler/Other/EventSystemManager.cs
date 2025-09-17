using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace OSK
{
    
    [DefaultExecutionOrder( 1000 )]
    public class EventSystemManager : MonoBehaviour
    {
        private void Awake()
        {
            EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

            if (eventSystems.Length > 1)
            {
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    Destroy(eventSystems[i].gameObject);
                }
                OSKLogger.LogWarning("UI","There are more than one EventSystem in the scene. Destroying all except the first one.");
            }
            else if (eventSystems.Length == 0)
            {
                GameObject newEventSystem = new GameObject("EventSystem");
                newEventSystem.AddComponent<EventSystem>();
                newEventSystem.AddComponent<StandaloneInputModule>(); 
                
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
		        StandaloneInputModule legacyInputModule = newEventSystem.GetComponent<StandaloneInputModule>();
		        if(legacyInputModule)
		        {
			        DestroyImmediate(legacyInputModule );
                    newEventSystem.AddComponent<InputSystemUIInputModule>();
		        }
#endif
            }
        }
    }
}
