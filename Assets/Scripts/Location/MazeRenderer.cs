using System.Collections.Generic;
using UnityEngine;

public class MazeRenderer : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private DungeonGenerator generator;

    [Header("Настройки")]
    [SerializeField] private Vector3 gridOffset = Vector3.zero;
    [SerializeField] private float cellSize = 1f;           // размер ОДНОГО тайла

    [Header("Цвета тайлов")]
    [SerializeField] private Color wallColor = new Color(0.15f, 0.15f, 0.35f);
    [SerializeField] private Color floorEmptyColor = new Color(0.4f, 0.4f, 0.4f);

    private List<GameObject> spawnedObjects = new List<GameObject>();

    public void Render()
    {
        ClearOldVisuals();
        if (generator == null) return;

        for (int x = 0; x < generator.TileCountX; x++)
        {
            for (int y = 0; y < generator.TileCountY; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, y * cellSize, 0) + gridOffset;
                var tile = generator.GetTile(new Vector2Int(x, y));

                if (tile.type == TileType.Wall)
                    CreateTile(pos, wallColor);
                else
                    CreateTile(pos, GetEventColor(tile.eventType));
            }
        }

        //Debug.Log("🧱 визуализация обновлена");
    }

    private void CreateTile(Vector3 pos, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(cellSize, cellSize, 1);
        obj.transform.parent = transform;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        obj.GetComponent<Renderer>().material = mat;

        spawnedObjects.Add(obj);
    }

    private Color GetEventColor(EventType type)
    {
        return type switch
        {
            EventType.Enemy => new Color(1f, 0.2f, 0.2f),
            EventType.RandomEvent => new Color(1f, 0.8f, 0f),
            EventType.Reward => new Color(0.2f, 1f, 0.2f),
            EventType.Quest => new Color(1f, 0f, 1f),
            EventType.Merchant => new Color(0f, 0.8f, 1f),
            EventType.Exit => new Color(0f, 0.7f, 1f),
            _ => floorEmptyColor
        };
    }

    private void ClearOldVisuals()
    {
        foreach (var obj in spawnedObjects)
            if (obj != null) Destroy(obj);
        spawnedObjects.Clear();
    }

    [ContextMenu("Перегенерировать")]
    public void Regenerate() => generator?.GenerateDungeon();

    // Перерисовывает весь лабиринт (чтобы сразу видеть очищенные клетки)
    public void Refresh()
    {
        Render();
    }
}