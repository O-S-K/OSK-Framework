using UnityEngine;

namespace OSK
{
    public class TabGroup : MonoBehaviour
    {
        public TabButton[] tabButtons;
        public Tab[] tabs;
        public int defaultTab;
        private TabButton _tabSelected;

        public void Initialize()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].Initialize(this, i);
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Initialize();
            }
            _tabSelected = tabButtons[defaultTab];
        }

        public void SelectCurrentTab()
        {
            _tabSelected.Select();
        }

        public void SelectTab(TabButton tabButton)
        {
            ResetTab();
            _tabSelected = tabButton;
            tabs[tabButton.Index].Active();
        }

        public void ResetTab()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].DeSelect();
                tabs[i].DeActive();
            }
        }
    }
}