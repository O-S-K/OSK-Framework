using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace OSK
{
    public class TabButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int index;
        [SerializeField] private UnityEvent onTabSelected;
        [SerializeField] private UnityEvent onTabDeselected;

        private TabGroup _tabGroup;
        
        public int Index => index;
        
        public void Initialize(TabGroup tabGroup, int index)
        {
            this.index = index;
            this._tabGroup = tabGroup;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Select();
        }
        
        public virtual void Select()
        {
            _tabGroup.SelectTab(this);
            onTabSelected?.Invoke();
        }
        
        public virtual void DeSelect()
        {
            onTabDeselected?.Invoke();
        }
    }
}

