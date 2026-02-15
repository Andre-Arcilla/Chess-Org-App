using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class AnnouncementCard : MonoBehaviour
{
    [SerializeField] private AnnouncementModel announcement;
    [SerializeField] private TextMeshProUGUI annTitle;
    [SerializeField] private TextMeshProUGUI annDate;

    public void SetInformation(AnnouncementModel announcementInfo)
    {
        announcement = announcementInfo;
        annTitle.text = announcement.Title;
        annDate.text = announcement.Date.ToString("MMMM dd, yyyy hh:mm:ss tt");
    }

    public void OnClick()
    {
        AnnouncementList.Instance.ShowItem(announcement);
    }
}