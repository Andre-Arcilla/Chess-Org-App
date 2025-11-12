using UnityEngine;
using UnityEngine.UI;

public class PageBtnScript : MonoBehaviour
{
    [SerializeField] private PageManager pageManager;
    [SerializeField] private NavbarManager navbarManager;
    [SerializeField] private GameObject sender;
    [SerializeField] private bool StartupButton;

    private void Start()
    {
        if (StartupButton)
        {
            gameObject.GetComponent<Button>().onClick.Invoke();
        }
    }

    public void OnClick()
    {
        // Switch pages
        pageManager.SelectPage();
        // Change button visuals
        navbarManager.SelectButton(sender);
    }
}
