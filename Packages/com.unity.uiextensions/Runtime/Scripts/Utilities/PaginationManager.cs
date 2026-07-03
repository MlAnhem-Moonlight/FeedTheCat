using System.Collections.Generic;
using UnityEngine;
/// Credit Brogan King (@BroganKing)
/// Original Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/issues/158/pagination-script

using System.Linq;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Pagination Manager")]
    public class PaginationManager : ToggleGroup
    {
        private List<Toggle> m_PaginationChildren;

        [SerializeField]
        private ScrollSnapBase scrollSnap = null;

        private bool isAClick;

        public int CurrentPage
        {
            get { return scrollSnap.CurrentPage; }
        }

        protected PaginationManager()
        { }


        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            if (scrollSnap == null)
            {
                Debug.LogError("A ScrollSnap script must be attached");
                return;
            }

            // Set this GameObject as the Pagination container if not already set
            if (scrollSnap.Pagination == null)
                scrollSnap.Pagination = gameObject;

            // set scroll snap listeners
            scrollSnap.OnSelectionPageChangedEvent.AddListener(SetToggleGraphics);
            scrollSnap.OnSelectionChangeEndEvent.AddListener(OnPageChangeEnd);

            ResetPaginationChildren();
        }

        /// <summary>
        /// Remake the internal list of child toggles (m_PaginationChildren).
        /// Used after adding/removing a toggle.
        /// </summary>
        public void ResetPaginationChildren()
        {
            // add selectables to list (include inactive in case toggles are disabled at start)
            m_PaginationChildren = GetComponentsInChildren<Toggle>(true).Where(t => t != null).ToList<Toggle>();

            // clear existing listeners and set up toggles
            for (int i = 0; i < m_PaginationChildren.Count; i++)
            {
                var toggle = m_PaginationChildren[i];
                if (toggle == null) continue;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(ToggleClick);
                toggle.group = this;
                toggle.isOn = false;
            }

            // determine a safe start page clamped to available pages
            int availablePages = 0;
            if (scrollSnap != null && scrollSnap._scroll_rect != null && scrollSnap._scroll_rect.content != null)
                availablePages = scrollSnap._scroll_rect.content.childCount;

            int startPage = 0;
            if (scrollSnap != null)
                startPage = Mathf.Clamp(scrollSnap.CurrentPage, 0, Mathf.Max(0, availablePages - 1));

            // set toggles on start (clamped)
            SetToggleGraphics(startPage);

            // warn user that they have uneven amount of pagination toggles to page count
            // (only warn if both are non-zero and don't match - dynamically generated toggles should match perfectly)
            if (availablePages > 0 && m_PaginationChildren.Count > 0 && m_PaginationChildren.Count != availablePages)
                Debug.LogWarning($"Uneven pagination icon to page count: {m_PaginationChildren.Count} toggles for {availablePages} pages");
        }

        /// <summary>
        /// Calling from other scripts if you need to change screens programmatically
        /// </summary>
        /// <param name="pageNo"></param>
        public void GoToScreen(int pageNo)
        {
            scrollSnap.GoToScreen(pageNo, true);
        }


        /// <summary>
        /// Calls GoToScreen() based on the index of toggle that was pressed
        /// </summary>
        /// <param name="target"></param>
        private void ToggleClick(Toggle target)
        {
            if (!target.isOn)
            {
                isAClick = true;
                GoToScreen(m_PaginationChildren.IndexOf(target));
            }

        }

        private void ToggleClick(bool toggle)
        {
            if (toggle)
            {
                for (int i = 0; i < m_PaginationChildren.Count; i++)
                {
                    if (m_PaginationChildren[i].isOn && !scrollSnap._suspendEvents)
                    {
                        GoToScreen(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Calls GoToScreen() based on the index of toggle that was pressed
        /// </summary>
        /// <param name="target"></param>
        private void ToggleClick(int target)
        {
            isAClick = true;
            GoToScreen(target);
        }

        private void SetToggleGraphics(int pageNo)
        {
            if (isAClick)
                return;

            if (m_PaginationChildren == null || m_PaginationChildren.Count == 0)
                return;

            // clamp page number to available toggles/pages
            int maxPage = m_PaginationChildren.Count - 1;
            if (scrollSnap != null && scrollSnap._scroll_rect != null && scrollSnap._scroll_rect.content != null)
                maxPage = Mathf.Min(maxPage, scrollSnap._scroll_rect.content.childCount - 1);

            if (pageNo < 0) pageNo = 0;
            if (pageNo > maxPage) pageNo = maxPage;

            // suspend scroll events while we update toggle states (prevents feedback loops)
            bool prevSuspend = false;
            if (scrollSnap != null)
            {
                try { prevSuspend = scrollSnap._suspendEvents; scrollSnap._suspendEvents = true; } catch { }
            }

            for (int i = 0; i < m_PaginationChildren.Count; i++)
            {
                var t = m_PaginationChildren[i];
                if (t == null) continue;
                t.isOn = (i == pageNo);
            }

            if (scrollSnap != null)
            {
                try { scrollSnap._suspendEvents = prevSuspend; } catch { }
            }
        }

        private void OnPageChangeEnd(int pageNo)
        {
            isAClick = false;
        }
    }
}
