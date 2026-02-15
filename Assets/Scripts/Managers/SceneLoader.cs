using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private List<string> scenes;

    public void LoadNewScene(string sceneName)
    {
        if (!scenes.Contains(sceneName))
        {
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}