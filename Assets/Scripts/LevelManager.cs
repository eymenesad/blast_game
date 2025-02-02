using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshProUGUI
using System.Collections.Generic;

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
    [Header("Rocket Prefab")]
    [SerializeField] private GameObject rocketPrefab;


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

    private GameObject[,] gridArray; // We'll store references to every spawned cube/obstacle here

    private void SpawnGrid()
    {
        int w = currentLevelData.grid_width;
        int h = currentLevelData.grid_height;
        string[] items = currentLevelData.grid;

        // Create the array
        gridArray = new GameObject[w, h];

        // The scale factor you already have
        float gridScale = 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                if (index < items.Length)
                {
                    string code = items[index];
                    GameObject prefab = GetPrefabByCode(code);
                    if (prefab != null)
                    {
                        // Calculate a spawn position
                        Vector2 spawnPos = new Vector2(x * gridScale, y * gridScale);

                        // Instantiate
                        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                        obj.transform.SetParent(this.transform);
                        obj.transform.localScale = new Vector3(gridScale, gridScale, gridScale);

                        // Keep a reference in the array
                        gridArray[x, y] = obj;

                        // Attach CubeItem if it's a color cube
                        // (For obstacles, you may use a separate ObstacleItem script.)
                        CubeItem cubeComp = obj.AddComponent<CubeItem>();
                        cubeComp.Init(this, x, y, code);
                    }
                }
            }
        }
    }

    private void ApplyGravity()
    {
        int w = currentLevelData.grid_width;
        int h = currentLevelData.grid_height;

        // For each column, move cubes down
        for (int x = 0; x < w; x++)
        {
            int writeY = 0; // We'll "write" cubes starting from row 0 (bottom) upwards
            for (int y = 0; y < h; y++)
            {
                if (gridArray[x, y] != null)
                {
                    CubeItem ci = gridArray[x, y].GetComponent<CubeItem>();

                    // If it’s a color cube (or anything that has CubeItem), move it down
                    if (ci != null)
                    {
                        if (writeY != y)
                        {
                            gridArray[x, writeY] = gridArray[x, y];
                            gridArray[x, y] = null;

                            ci.y = writeY;

                            Vector3 newPos = new Vector3(x * 0.5f, writeY * 0.5f, 0f);
                            gridArray[x, writeY].transform.position = newPos;
                        }
                        writeY++;
                    }
            }else{
                // If it's an obstacle, just skip it code it's not affected by gravity

            }

            // Anything above writeY is now empty, so ensure those slots are null
            for (int y = writeY; y < h; y++)
            {
                gridArray[x, y] = null;
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

            case "rand":
                GameObject[] colorPrefabs = { redCubePrefab, greenCubePrefab, blueCubePrefab, yellowCubePrefab };
                int randIndex = Random.Range(0, colorPrefabs.Length);
                return colorPrefabs[randIndex];

            default:
                Debug.LogWarning($"No matching prefab for code: {code}");
                return null;
        }
    }

    private List<CubeItem> FindConnectedGroup(CubeItem start)
    {
        List<CubeItem> results = new List<CubeItem>();
        Queue<CubeItem> toCheck = new Queue<CubeItem>();

        string targetColor = start.colorCode;
        toCheck.Enqueue(start);
        results.Add(start);

        while (toCheck.Count > 0)
        {
            CubeItem current = toCheck.Dequeue();

            // Get up to 4 neighbors (left, right, up, down)
            foreach (CubeItem neighbor in GetNeighbors(current.x, current.y))
            {
                if (!results.Contains(neighbor) && neighbor.colorCode == targetColor)
                {
                    results.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        return results;
    }


    private List<CubeItem> GetNeighbors(int x, int y)
    {
        List<CubeItem> neighbors = new List<CubeItem>();

        // left
        if (x > 0 && gridArray[x-1, y] != null)
            neighbors.Add(gridArray[x-1, y].GetComponent<CubeItem>());
        // right
        if (x < currentLevelData.grid_width - 1 && gridArray[x+1, y] != null)
            neighbors.Add(gridArray[x+1, y].GetComponent<CubeItem>());
        // down
        if (y > 0 && gridArray[x, y-1] != null)
            neighbors.Add(gridArray[x, y-1].GetComponent<CubeItem>());
        // up
        if (y < currentLevelData.grid_height - 1 && gridArray[x, y+1] != null)
            neighbors.Add(gridArray[x, y+1].GetComponent<CubeItem>());

        return neighbors;
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
    public void HandleCubeTap(CubeItem tappedCube)
{
    // 1. Find all connected cubes of the same color
    List<CubeItem> connected = FindConnectedGroup(tappedCube);

    // 2. Check group size
    if (connected.Count >= 2)
    {
        // Decide if we spawn a rocket
        bool createRocket = (connected.Count >= 4);

        // 3. Remove them from the board
        foreach (var c in connected)
        {
            // Clear from grid array
            gridArray[c.x, c.y] = null;
            Destroy(c.gameObject);
        }

        // 4. If 4+ in the group, spawn rocket at the tapped position
        if (createRocket && rocketPrefab != null)
        {
            Vector2 rocketPos = tappedCube.transform.position;
            GameObject rocketObj = Instantiate(rocketPrefab, rocketPos, Quaternion.identity, this.transform);
            // Optionally set rocket orientation (horizontal/vertical) at random
            // rocketObj.GetComponent<RocketItem>().isHorizontal = (Random.value > 0.5f);
        }

        // 5. Apply gravity so empty spots get filled
        ApplyGravity();

        // 6. Subtract a move
        OnValidMove();

        // 7. Maybe check obstacles
        if (AreAllObstaclesCleared())
        {
            OnWin();
        }
    }
    else
    {
        // Group < 2 => no blast => do nothing
        // (This could be considered an "invalid move" that doesn't consume a turn.)
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
