using UnityEngine;

public class PageBtnScript : MonoBehaviour
{
    [SerializeField] private PageManager pageManager;
    [SerializeField] private NavbarManager navbarManager;
    [SerializeField] private GameObject page;
    [SerializeField] private bool keepOldPage;
    [SerializeField] private GameObject sender;

    public void OnClick()
    {
        // Switch pages
        pageManager.SelectPage(page, keepOldPage);
        // Change button visuals
        navbarManager.SelectButton(sender);
    }
}
