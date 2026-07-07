using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    internal sealed class DropdownItemRaycastFix : MonoBehaviour
    {
        public Image Target;

        private void OnEnable()
        {
            if (Target == null)
            {
                Target = GetComponent<Image>();
            }

            if (Target == null)
            {
                return;
            }

            Target.enabled = true;
            Target.raycastTarget = true;
        }
    }
}