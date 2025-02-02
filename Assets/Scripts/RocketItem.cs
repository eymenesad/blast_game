using UnityEngine;

public class RocketItem : MonoBehaviour
{
    private bool isHorizontal;
    private const float gridScale = 0.5f;

    public void SetOrientation(bool horizontal)
    {
        isHorizontal = horizontal;
        if (isHorizontal)
            transform.eulerAngles = new Vector3(0f, 0f, 90f);
    }

    private void OnMouseDown()
    {
        Explode();
    }

    private void Explode()
    {
        LevelManager manager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        if (manager == null) return;

        // find rocket's integer coords
        int x = Mathf.RoundToInt(transform.position.x / gridScale);
        int y = Mathf.RoundToInt(transform.position.y / gridScale);

        if (isHorizontal)
        {
            for (int col = 0; col < manager.CurrentLevelWidth; col++)
            {
                GameObject obj = manager.GetGridObject(col, y);
                if (obj != null)
                {
                    ObstacleItem obs = obj.GetComponent<ObstacleItem>();
                    if (obs) obs.TakeDamage(1);
                    else
                    {
                        manager.SetGridObject(col, y, null);
                        Destroy(obj);
                    }
                }
            }
        }
        else
        {
            for (int row = 0; row < manager.CurrentLevelHeight; row++)
            {
                GameObject obj = manager.GetGridObject(x, row);
                if (obj != null)
                {
                    ObstacleItem obs = obj.GetComponent<ObstacleItem>();
                    if (obs) obs.TakeDamage(1);
                    else
                    {
                        manager.SetGridObject(x, row, null);
                        Destroy(obj);
                    }
                }
            }
        }

        Destroy(gameObject);
        manager.ApplyGravity();
        manager.OnValidMove();
    }
}
