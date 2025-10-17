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
        pageManager.SelectPage(page, keepOldPage);
        navbarManager.SelectButton(sender);
    }
}
