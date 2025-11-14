using SQLite4Unity3d;
using System.Collections;
using TMPro;
using UnityEngine;

public class SigninManager : MonoBehaviour
{
    // CHECK SERVER IF ACCOUNT EXISTS
    [Header("Scene Manager")]
    [SerializeField] private SceneLoader SceneLoader;

    [Header("Login Fields")]
    [SerializeField] private TMP_InputField studNum;
    [SerializeField] private TMP_InputField password;

    [Header("Popup")]
    [SerializeField] private GameObject popupObject;

    [SerializeField] private ProfileModel profile;

    public void SignInButton()
    {
        var inputStudNum = studNum.text;
        var inputPassword = password.text;

        // CHANGE TO CHECK FROM ONLINE SERVER INSTEAD OF LOCAL
        // Get profile with same student ID
        var user = GenerateDatabase.Instance.database.Table<ProfileModel>().Where(profile => profile.StudNum.ToUpper() == inputStudNum.ToUpper()).FirstOrDefault();

        // No profile with same student ID found in local database
        if (user == null || string.IsNullOrWhiteSpace(inputStudNum))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid student number"));
            return;
        }

        // Check if inputpassword and profile password match
        if (user.Password != inputPassword || string.IsNullOrWhiteSpace(inputPassword))
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Invalid password"));
            return;
        }

        GenerateDatabase.Instance.currentUser = user;

        // Load scene based on profile role
        if (user.Role.ToUpper() == "MEMBER")
        {
            SceneLoader.LoadNewScene("MemberScene");
        }
        else if (user.Role.ToUpper() == "COACH")
        {
            SceneLoader.LoadNewScene("CoachScene");
        }
        else if (user.Role.ToUpper() == "ADMIN")
        {
            SceneLoader.LoadNewScene("AdminScene");
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ShowPopup("Account disabled"));
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
