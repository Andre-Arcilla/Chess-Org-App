using SQLite4Unity3d;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class RegistrationManager : MonoBehaviour
{
    [Header("Registration Fields")]
    [SerializeField] private TMP_InputField studName;
    [SerializeField] private TMP_InputField studNum;
    [SerializeField] private TMP_InputField Email;
    [SerializeField] private TMP_InputField password1;
    [SerializeField] private TMP_InputField password2;

    [Header("Popup")]
    [SerializeField] private GameObject popupObject;

    public void RegisterButton()
    {
        //check inputs
        var inputStudName = studName.text;
        var inputStudNum = studNum.text;
        var inputEmail = Email.text;
        var inputPassword1 = password1.text;
        var inputPassword2 = password2.text;

        if (string.IsNullOrWhiteSpace(inputStudName))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid Name"));
            return;
        }

        if (string.IsNullOrWhiteSpace(inputStudNum) || inputStudNum.Contains(" "))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid Student Number"));
            return;
        }

        if (!Regex.IsMatch(inputEmail, @"^[a-z0-9\.]+@umak\.edu\.ph$", RegexOptions.IgnoreCase))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid Email"));
            return;
        }

        if (string.IsNullOrWhiteSpace(inputPassword1) || string.IsNullOrWhiteSpace(inputPassword2))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid Password"));
            return;
        }

        if (inputPassword1 != inputPassword2)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Passwords do not match"));
            return;
        }

        // Check if name is already in Profiles table
        if (GenerateDatabase.Instance.database.Table<ProfileModel>().Where(account => account.StudName.ToUpper() == inputStudName.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same name already exists"));
            return;
        }

        // Check if name is already in Registrations table
        if (GenerateDatabase.Instance.database.Table<RegisterModel>().Where(account => account.StudName.ToUpper() == inputStudName.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same name already registered"));
            return;
        }

        // Check if student number is already in Profiles table
        if (GenerateDatabase.Instance.database.Table<ProfileModel>().Where(account => account.StudNum.ToUpper() == inputStudNum.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same student number already exists"));
            return;
        }

        // Check if student number is already in Registrations table
        if (GenerateDatabase.Instance.database.Table<RegisterModel>().Where(account => account.StudNum.ToUpper() == inputStudNum.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same student number already registered"));
            return;
        }

        // Check if email is already in Profiles table
        if (GenerateDatabase.Instance.database.Table<ProfileModel>().Where(account => account.Email.ToUpper() == inputEmail.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same email already exists"));
            return;
        }

        // Check if email is already in Registrations table
        if (GenerateDatabase.Instance.database.Table<RegisterModel>().Where(account => account.Email.ToUpper() == inputEmail.ToUpper()).FirstOrDefault() != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account with the same email already registered"));
            return;
        }

        // Check if studName and studID is in org roster
        if (GenerateDatabase.Instance.database.Table<OrgMemberModel>().Where(account => account.StudName.ToUpper() == inputStudName.ToUpper()).FirstOrDefault() != null &&
            GenerateDatabase.Instance.database.Table<OrgMemberModel>().Where(account => account.StudNum.ToUpper() == inputStudNum.ToUpper()).FirstOrDefault() != null)
            {
            // Else add new item in registrations table
            GenerateDatabase.Instance.database.Execute(
                "INSERT INTO Profiles (StudName, StudNum, Email, Password) VALUES (?, ?, ?, ?)",
                inputStudName, inputStudNum, inputEmail, inputPassword1
            );

            StopAllCoroutines();
            StartCoroutine(ShowPopup("Registration Done! Account autonatically approved, you can now login."));
        }
        else
        {
            // Else add new item in registrations table
            GenerateDatabase.Instance.database.Execute(
                "INSERT INTO Registrations (StudName, StudNum, Email, Password) VALUES (?, ?, ?, ?)",
                inputStudName, inputStudNum, inputEmail, inputPassword1
            );

            StopAllCoroutines();
            StartCoroutine(ShowPopup("Registration Done! Please wait for your registration to be approved"));
        }

    }

    private IEnumerator ShowPopup(string message)
    {
        var group = popupObject.GetComponent<CanvasGroup>();
        var text = popupObject.GetComponentInChildren<TextMeshProUGUI>();

        text.text = message;

        // Fade in
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        group.alpha = 1f;

        // Wait 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        group.alpha = 0f;
    }
}
