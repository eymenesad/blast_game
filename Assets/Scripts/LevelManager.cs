using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshProUGUI

public class LevelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;          // optional, shows moves in LevelScene
    [SerializeField] private GameObject failPopup;              // optional, a "lose" popup
    [SerializeField] private GameObject winPopup;               // optional, a "win" popup

    [Header("Cube / Obstacle Prefabs")]
    [SerializeField] private GameObject redCubePrefab;
    [SerializeField] private GameObject greenCubePrefab;
    [SerializeField] private GameObject blueCubePrefab;
    [SerializeField] private GameObject yellowCubePrefab;

    [SerializeField] private GameObject boxPrefab;   // "bo"
    [SerializeField] private GameObject stonePrefab; // "s"
    [SerializeField] private GameObject vasePrefab;  // "v"

    // If you plan to handle "rand" or rockets, you'd add them here too

    private LevelData currentLevelData;
    private int moveCount;
    private bool levelComplete = false; // so we don't double-trigger win/lose

    void Start()
    {
        // 1) Which level to load?
        int lastPlayedLevel = PlayerPrefs.GetInt("LastPlayedLevel", 1);

        // 2) Build the file name: "level_01", "level_02", etc.
        //    If your files are named "level_1.json" with no zero padding, adjust accordingly.
        string levelFileName = $"level_{lastPlayedLevel:00}"; // e.g. "level_01"

        // 3) Load from Resources/Levels/level_XX.json
        //    Make sure you put your .json file in "Assets/Resources/Levels/level_01.json" etc.
        TextAsset levelTextAsset = Resources.Load<TextAsset>($"Levels/{levelFileName}");
        if (levelTextAsset == null)
        {
            Debug.LogError($"Level file not found: {levelFileName}. Defaulting to level_01 or handle error.");
            // Optionally handle error or fallback
            return;
        }

        // 4) Parse JSON
        currentLevelData = JsonUtility.FromJson<LevelData>(levelTextAsset.text);

        // 5) Grab move count from that data
        moveCount = currentLevelData.move_count;
        UpdateMovesUI();

        // 6) Spawn the grid
        SpawnGrid();
    }

    private void SpawnGrid()
{
    int w = currentLevelData.grid_width;
    int h = currentLevelData.grid_height;
    string[] items = currentLevelData.grid;

    // Define a scaling factor for the grid and objects
    float gridScale = 0.5f; // Adjust this value to fit your needs

    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            int index = y * w + x;
            if (index < items.Length)
            {
                string code = items[index]; // "r", "g", "b", "y", "bo", "s", "v", "rand" etc.
                GameObject prefab = GetPrefabByCode(code);
                if (prefab != null)
                {
                    // Adjust the position based on the grid scale
                    Vector2 spawnPos = new Vector2(x * gridScale, y * gridScale);

                    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                    obj.transform.SetParent(this.transform);

                    // Scale the object to fit the grid
                    obj.transform.localScale = new Vector3(gridScale, gridScale, gridScale);
                }
            }
        }
    }
}

    // This method chooses which prefab to instantiate based on the code in JSON
    private GameObject GetPrefabByCode(string code)
    {
        // Adjust to match your color codes
        switch (code)
        {
            case "r": return redCubePrefab;
            case "g": return greenCubePrefab;
            case "b": return blueCubePrefab;
            case "y": return yellowCubePrefab;

            case "bo": return boxPrefab;   // box
            case "s":  return stonePrefab; // stone
            case "v":  return vasePrefab;  // vase

            // case "rand":
            //   pick a random color cube, or handle differently
            //   return SomethingRandom();

            default:
                Debug.LogWarning($"No matching prefab for code: {code}");
                return null;
        }
    }

    // Called whenever a valid move is performed (tap / blast).
    // You'd call this from your "TapManager" or "BoardManager" after a group is destroyed, for example.
    public void OnValidMove()
    {
        if (levelComplete) return; // no further moves if level ended

        moveCount--;
        UpdateMovesUI();

        if (moveCount <= 0)
        {
            // Check if maybe user cleared obstacles at the last move
            if (AreAllObstaclesCleared() && !levelComplete)
            {
                OnWin();
            }
            else
            {
                OnLose();
            }
        }
    }

    private void UpdateMovesUI()
    {
        if (movesText != null)
        {
            movesText.text = $"Moves: {moveCount}";
        }
    }

    // Example check to see if all obstacles are cleared
    // This depends on your game object structure. One naive approach:
    private bool AreAllObstaclesCleared()
    {
        // If you tag obstacles as "Obstacle", or check the scene for any "BoxItem"/"StoneItem" scripts, etc.
        // For simplicity, let's just return false for now.
        // In real code, you'd do something like:
        //
        //   var obstacles = FindObjectsOfType<ObstacleItem>();
        //   return obstacles.Length == 0;
        //
        return false;
    }

    public void OnWin()
    {
        levelComplete = true;
        Debug.Log("Level complete!");

        // Save progress
        int currentLvl = currentLevelData.level_number;
        PlayerPrefs.SetInt("LastPlayedLevel", currentLvl + 1);
        PlayerPrefs.Save();

        // Show Win Popup if you want
        if (winPopup != null)
            winPopup.SetActive(true);
        else
            ReturnToMainScene(); // or directly load main if no popup
    }

    public void OnLose()
    {
        if (levelComplete) return; // already ended?
        levelComplete = true;

        Debug.Log("You lost. Moves ended.");

        // Show fail popup
        if (failPopup != null)
            failPopup.SetActive(true);
        else
            ReturnToMainScene();
    }

    // Called by a "Close" or "Main" button on the fail/win popup
    public void ReturnToMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    // Called by a "Try Again" button on the fail popup
    public void ReplayLevel()
    {
        // reload same scene
        SceneManager.LoadScene("LevelScene");
    }
}
