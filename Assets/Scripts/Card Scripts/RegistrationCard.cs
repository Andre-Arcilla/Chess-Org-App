using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RegistrationCard : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private RegisterModel registration;

    [Header("Card Texts")]
    [SerializeField] private TextMeshProUGUI cardStudName;
    [SerializeField] private TextMeshProUGUI cardDate;

    public void SetInformation(RegisterModel registration)
    {
        this.registration = registration;
        cardStudName.text = registration.StudName;
        cardDate.text = registration.Date.ToString("MMMM dd, yyyy");
    }

    public void OnClick()
    {
        RegistrationListManager.Instance.ShowItem(registration);
    }
}