using UnityEngine;

// Make sure each cube prefab has a Collider2D so OnMouseDown works in 2D
[RequireComponent(typeof(Collider2D))]
public class CubeItem : MonoBehaviour
{
    // We'll set these in Init()
    private LevelManager board;
    public int x, y;
    public string colorCode;

    // Called right after we spawn the cube
    public void Init(LevelManager boardRef, int gridX, int gridY, string code)
    {
        board = boardRef;
        x = gridX;
        y = gridY;
        colorCode = code;
    }

    // This is a simple way to detect clicks in 2D
    private void OnMouseDown()
    {
        // Tell the board: "Hey, this cube was tapped."
        board.HandleCubeTap(this);
    }
}
