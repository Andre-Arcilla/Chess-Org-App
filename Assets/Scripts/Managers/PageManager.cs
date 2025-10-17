using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PageManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> pageList;
    [SerializeField] private ThemeManager themeManager;

    public void SelectPage(GameObject btnPage, bool keepOldPage = false)
    {
        foreach (var page in pageList)
        {
            if (page == btnPage || (page.activeSelf == true && keepOldPage))
            {
                page.SetActive(true);
            }
            else
            {
                var scrollbar = page.GetComponentInChildren<Scrollbar>();
                if (scrollbar != null)
                {
                    scrollbar.value = 1f;
                }

                page.SetActive(false);
            }
        }
    }
}