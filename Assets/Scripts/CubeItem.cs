using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CubeItem : MonoBehaviour
{
    private LevelManager board;
    public int x, y;
    public string colorCode;

    public void Init(LevelManager boardRef, int gridX, int gridY, string code)
    {
        board = boardRef;
        x = gridX;
        y = gridY;
        colorCode = code;
    }

    private void OnMouseDown()
    {
        board.HandleCubeTap(this);
    }
}
