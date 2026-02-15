using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PageManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> pageList;
    [SerializeField] private Transform canvas;
    [SerializeField] private Button announcementButton;

    public void SelectPage()
    {
        foreach (var page in pageList.Where(p => p.activeSelf))
        {
            var scrollbar = page.GetComponentInChildren<Scrollbar>();
            if (scrollbar != null)
            {
                scrollbar.value = 1f;
            }

            page.transform.SetParent(canvas);
            page.transform.SetAsFirstSibling();
            page.SetActive(false);
        }
    }

    public void OpenPosts()
    {
        announcementButton.onClick.Invoke();
    }

    public void GoToScene(string sceneName)
    {
        SceneLoader.Instance.LoadNewScene(sceneName);
    }

    public void GoToChessScene(int depth)
    {
        SceneLoader.Instance.LoadNewScene("ChessScene");
        StaticDataString.depth = depth;
    }
}