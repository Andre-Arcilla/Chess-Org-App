using TMPro;
using UnityEngine;

public class ProfileCard : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private ProfileModel profile;

    [Header("Card Texts")]
    [SerializeField] private TextMeshProUGUI cardStudName;
    [SerializeField] private TextMeshProUGUI cardStudNum;

    public void SetInformation(ProfileModel profile)
    {
        this.profile = profile;
        cardStudName.text = profile.StudName;
        cardStudNum.text = profile.StudNum;
    }

    public void OnClick()
    {
        ProfileListManager.Instance.ShowItem(profile);
    }
}