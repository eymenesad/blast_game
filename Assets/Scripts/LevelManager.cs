using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private GameObject failPopup;
    [SerializeField] private GameObject winPopup;

    [Header("Cube / Obstacle Prefabs")]
    [SerializeField] private GameObject redCubePrefab;
    [SerializeField] private GameObject greenCubePrefab;
    [SerializeField] private GameObject blueCubePrefab;
    [SerializeField] private GameObject yellowCubePrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject vasePrefab;

    [Header("Rocket Prefab")]
    [SerializeField] private GameObject rocketPrefab;

    private LevelData currentLevelData;
    private int moveCount;
    private bool levelComplete = false;
    private GameObject[,] gridArray;

    void Start()
    {
        int lastPlayedLevel = PlayerPrefs.GetInt("LastPlayedLevel", 1);
        string levelFileName = $"level_{lastPlayedLevel:00}";
        TextAsset levelTextAsset = Resources.Load<TextAsset>($"Levels/{levelFileName}");
        if (levelTextAsset == null)
        {
            Debug.LogError("Level file not found: " + levelFileName);
            return;
        }
        currentLevelData = JsonUtility.FromJson<LevelData>(levelTextAsset.text);
        moveCount = currentLevelData.move_count;
        UpdateMovesUI();
        SpawnGrid();
    }

    void SpawnGrid()
    {
        int w = currentLevelData.grid_width;
        int h = currentLevelData.grid_height;
        string[] items = currentLevelData.grid;
        gridArray = new GameObject[w, h];
        float scale = 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                if (index < items.Length)
                {
                    GameObject prefab = GetPrefabByCode(items[index]);
                    if (prefab)
                    {
                        Vector2 pos = new Vector2(x * scale, y * scale);
                        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
                        obj.transform.localScale = new Vector3(scale, scale, scale);
                        gridArray[x, y] = obj;

                        if (items[index] == "bo" || items[index] == "s" || items[index] == "v")
                        {
                            ObstacleItem o = obj.AddComponent<ObstacleItem>();
                            o.Init(this, x, y, 2);
                        }
                        else
                        {
                            CubeItem c = obj.AddComponent<CubeItem>();
                            c.Init(this, x, y, items[index]);
                        }
                    }
                }
            }
        }
    }

    public void TrySwap(int x1, int y1, int x2, int y2)
    {
        if (!ValidCoord(x2, y2)) return;
        GameObject objA = gridArray[x1, y1], objB = gridArray[x2, y2];
        if (!objA || !objB) return;
        CubeItem A = objA.GetComponent<CubeItem>(), B = objB.GetComponent<CubeItem>();
        if (!A || !B) return;

        gridArray[x1, y1] = objB;
        gridArray[x2, y2] = objA;
        int oldX = A.x, oldY = A.y;
        A.x = x2; A.y = y2;
        B.x = x1; B.y = y1;

        float s = 0.5f;
        objA.transform.position = new Vector3(x2 * s, y2 * s, 0);
        objB.transform.position = new Vector3(x1 * s, y1 * s, 0);

        if (CheckForMatches())
        {
            ApplyGravity();
            OnValidMove();
        }
        else
        {
            gridArray[x1, y1] = objA;
            gridArray[x2, y2] = objB;
            A.x = oldX; A.y = oldY;
            B.x = x2; B.y = y2; // correct if needed
            objA.transform.position = new Vector3(x1 * s, y1 * s, 0);
            objB.transform.position = new Vector3(x2 * s, y2 * s, 0);
        }
    }

    bool CheckForMatches()
    {
        HashSet<CubeItem> toRemove = new HashSet<CubeItem>();
        int w = currentLevelData.grid_width, h = currentLevelData.grid_height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                GameObject obj = gridArray[x, y];
                if (!obj) continue;
                CubeItem c = obj.GetComponent<CubeItem>();
                if (!c) continue;
                List<CubeItem> group = FloodColor(c);
                if (group.Count >= 2) toRemove.UnionWith(group);
            }
        }
        if (toRemove.Count == 0) return false;

        foreach (CubeItem ci in toRemove)
        {
            gridArray[ci.x, ci.y] = null;
            Destroy(ci.gameObject);
        }
        DamageObstaclesAdjacentTo(toRemove);
        return true;
    }

    List<CubeItem> FloodColor(CubeItem start)
    {
        List<CubeItem> group = new List<CubeItem>();
        Queue<CubeItem> q = new Queue<CubeItem>();
        group.Add(start);
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var n in GetColorNeighbors(cur.x, cur.y, cur.colorCode))
            {
                if (!group.Contains(n))
                {
                    group.Add(n);
                    q.Enqueue(n);
                }
            }
        }
        return group;
    }

    List<CubeItem> GetColorNeighbors(int x, int y, string color)
    {
        List<CubeItem> list = new List<CubeItem>();
        if (ValidCoord(x - 1, y)) list.AddCheck(gridArray[x - 1, y], color);
        if (ValidCoord(x + 1, y)) list.AddCheck(gridArray[x + 1, y], color);
        if (ValidCoord(x, y - 1)) list.AddCheck(gridArray[x, y - 1], color);
        if (ValidCoord(x, y + 1)) list.AddCheck(gridArray[x, y + 1], color);
        return list;
    }

    void DamageObstaclesAdjacentTo(IEnumerable<CubeItem> destroyed)
    {
        HashSet<ObstacleItem> hits = new HashSet<ObstacleItem>();
        foreach (var c in destroyed)
        {
            Vector2Int[] dirs = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            foreach (var d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (ValidCoord(nx, ny))
                {
                    var obj = gridArray[nx, ny];
                    if (!obj) continue;
                    var o = obj.GetComponent<ObstacleItem>();
                    if (o) hits.Add(o);
                }
            }
        }
        foreach (var o in hits) o.TakeDamage(1);
    }

    bool ValidCoord(int x, int y)
    {
        return x >= 0 && x < currentLevelData.grid_width && y >= 0 && y < currentLevelData.grid_height;
    }

    public void ApplyGravity()
    {
        int w = currentLevelData.grid_width, h = currentLevelData.grid_height;
        for (int x = 0; x < w; x++)
        {
            int writeY = 0;
            for (int y = 0; y < h; y++)
            {
                if (gridArray[x, y])
                {
                    if (writeY != y)
                    {
                        gridArray[x, writeY] = gridArray[x, y];
                        gridArray[x, y] = null;
                        var ci = gridArray[x, writeY].GetComponent<CubeItem>();
                        if (ci) ci.y = writeY;
                        float s = 0.5f;
                        gridArray[x, writeY].transform.position = new Vector3(x * s, writeY * s, 0);
                    }
                    writeY++;
                }
            }
            for (int y = writeY; y < h; y++) gridArray[x, y] = null;
        }
    }

    public void OnValidMove()
    {
        if (levelComplete) return;
        moveCount--;
        UpdateMovesUI();
        if (moveCount <= 0)
        {
            if (AreAllObstaclesCleared() && !levelComplete) OnWin();
            else OnLose();
        }
    }

    void UpdateMovesUI()
    {
        if (movesText) movesText.text = "Moves: " + moveCount;
    }

    bool AreAllObstaclesCleared()
    {
        var obs = Object.FindObjectsByType<ObstacleItem>(FindObjectsSortMode.None);
        return obs.Length == 0;
    }

    public void OnWin()
    {
        levelComplete = true;
        Debug.Log("Level complete!");
        int lvl = currentLevelData.level_number;
        PlayerPrefs.SetInt("LastPlayedLevel", lvl + 1);
        PlayerPrefs.Save();
        if (winPopup) winPopup.SetActive(true);
        else ReturnToMainScene();
    }

    public void OnLose()
    {
        if (levelComplete) return;
        levelComplete = true;
        Debug.Log("You lost.");
        if (failPopup) failPopup.SetActive(true);
        else ReturnToMainScene();
    }

    public void ReturnToMainScene() { SceneManager.LoadScene("MainScene"); }
    public void ReplayLevel() { SceneManager.LoadScene("LevelScene"); }

    GameObject GetPrefabByCode(string code)
    {
        switch (code)
        {
            case "r": return redCubePrefab;
            case "g": return greenCubePrefab;
            case "b": return blueCubePrefab;
            case "y": return yellowCubePrefab;
            case "bo": return boxPrefab;
            case "s":  return stonePrefab;
            case "v":  return vasePrefab;
            case "rand":
                GameObject[] arr = { redCubePrefab, greenCubePrefab, blueCubePrefab, yellowCubePrefab };
                return arr[Random.Range(0, arr.Length)];
        }
        return null;
    }

    public int CurrentLevelWidth  => currentLevelData.grid_width;
    public int CurrentLevelHeight => currentLevelData.grid_height;
    public GameObject GetGridObject(int x, int y)
    {
        if (x < 0 || x >= gridArray.GetLength(0)) return null;
        if (y < 0 || y >= gridArray.GetLength(1)) return null;
        return gridArray[x,y];
    }
    public void SetGridObject(int x, int y, GameObject obj)
    {
        if (x < 0 || x >= gridArray.GetLength(0)) return;
        if (y < 0 || y >= gridArray.GetLength(1)) return;
        gridArray[x,y] = obj;
    }
}

// Helpers
static class CubeExtensions
{
    public static void AddCheck(this List<CubeItem> list, GameObject obj, string color)
    {
        if (!obj) return;
        CubeItem c = obj.GetComponent<CubeItem>();
        if (c && c.colorCode == color) list.Add(c);
    }
}
