using System;

[Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public string[] grid; // e.g. "r","b","bo","s","rand" etc.
}