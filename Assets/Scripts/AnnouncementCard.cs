using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class AnnouncementCard : MonoBehaviour
{
    [SerializeField] private AnnouncementModel announcement;
    [SerializeField] private TextMeshProUGUI annTitle;
    [SerializeField] private TextMeshProUGUI annDate;
    [SerializeField] private GameObject parentList;
    [SerializeField] private GameObject announcementPost;
    [SerializeField] private Transform mainView;
    [SerializeField] private Transform canvas;

    public void SetInfo(AnnouncementModel announcementInfo, GameObject parentList, GameObject announcementPost, Transform mainView, Transform canvas)
    {
        if (announcement != null)
        {
            return;
        }

        this.parentList = parentList;
        this.mainView = mainView;
        this.canvas = canvas;
        this.announcementPost = announcementPost;
        announcement = announcementInfo;
        annTitle.text = announcement.Title;
        annDate.text = announcement.Date;
    }

    public void OpenAnnouncementPost()
    {
        TextMeshProUGUI title = announcementPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Title");
        TextMeshProUGUI date = announcementPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Date");
        TextMeshProUGUI content = announcementPost.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Post Content");

        title.text = announcement.Title;
        date.text = announcement.Date;
        content.text = announcement.Text;

        announcementPost.transform.SetParent(mainView);
        announcementPost.transform.SetSiblingIndex(1);

        parentList.transform.SetParent(canvas);
        parentList.transform.SetAsFirstSibling();
    }
}