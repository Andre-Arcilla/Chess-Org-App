using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OrgMemberCard : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private OrgMemberModel orgMember;

    [Header("Card Texts")]
    [SerializeField] private TextMeshProUGUI cardStudName;
    [SerializeField] private TextMeshProUGUI cardStudNum;

    public void SetInformation(OrgMemberModel orgMember)
    {
        this.orgMember = orgMember;
        cardStudName.text = orgMember.StudName;
        cardStudNum.text = orgMember.StudNum;
    }

    public void OnClick()
    {
        OrgListManager.Instance.ShowItem(orgMember);
    }
}