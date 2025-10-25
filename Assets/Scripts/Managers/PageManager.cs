using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PageManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> pageList;
    [SerializeField] private ThemeManager themeManager;
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform mainView;

    public void SelectPage(GameObject btnPage, bool keepOldPage = false)
    {
        foreach (var page in pageList)
        {
            if (page == btnPage || (page.activeSelf == true && keepOldPage))
            {
                page.transform.SetParent(mainView);
                page.transform.SetSiblingIndex(1);
            }
            else
            {
                var scrollbar = page.GetComponentInChildren<Scrollbar>();
                if (scrollbar != null)
                {
                    scrollbar.value = 1f;
                }

                page.transform.SetParent(canvas);
                page.transform.SetAsFirstSibling();
            }
        }
    }
}