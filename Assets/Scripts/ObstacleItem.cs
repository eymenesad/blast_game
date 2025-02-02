using UnityEngine;

public class ObstacleItem : MonoBehaviour
{
    private LevelManager manager;
    private int gridX, gridY;
    public int health = 1;

    public void Init(LevelManager mgr, int x, int y, int hp)
    {
        manager = mgr;
        gridX = x;
        gridY = y;
        health = hp;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            // Remove from the grid before destroying
            manager.SetGridObject(gridX, gridY, null);
            Destroy(gameObject);
        }
    }
}
