using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainSceneManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelButtonText;
    [SerializeField] private Button levelButton;
    [SerializeField] private int totalLevels = 10;

    private void Start()
    {
        // 1) Read last played level from PlayerPrefs, default to 1 if none is set
        int lastPlayedLevel = PlayerPrefs.GetInt("LastPlayedLevel", 1);

        // 2) Check if all levels are finished
        if (lastPlayedLevel > totalLevels)
        {
            levelButtonText.text = "Finished";
        }
        else
        {
            levelButtonText.text = $"Level {lastPlayedLevel}";
        }

        // 3) Add a click event so tapping the button loads the LevelScene
        levelButton.onClick.AddListener(OnLevelButtonClicked);
    }

    private void OnLevelButtonClicked()
    {
        // Possibly pass the current level to the next scene, or just load "LevelScene".
        SceneManager.LoadScene("LevelScene");
    }
}
