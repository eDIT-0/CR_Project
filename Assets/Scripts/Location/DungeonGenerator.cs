using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType { Wall, Floor }
public enum EventType { Empty, RandomEvent, Enemy, Reward, Quest, Merchant, Exit, WallHole, SideQuest }

public enum Direction
{
    North,
    East,
    South,
    West
}

[Serializable]
public class TileData
{
    public Vector2Int position;
    public TileType type = TileType.Wall;
    public EventType eventType = EventType.Empty;
    public bool visited = false;

    [NonSerialized]                    // Не сохраняем в инспекторе
    public SideQuestEventSO sideQuestData;   // Храним данные квеста только для SideQuest тайлов
}

public class DungeonGenerator : MonoBehaviour
{
    [Header("Настройки локации")]
    [SerializeField] private LocationDataSO locationData;
    [Header("Размер лабиринта (кол-во комнат)")]

    [SerializeField] private int roomCountX = 8;
    [SerializeField] private int roomCountY = 8;

    [Header("Настройки генерации")]
    [SerializeField, Range(0f, 1f)] private float enemyChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float randomEventChance = 0.2f;
    [SerializeField, Range(0f, 1f)] private float rewardChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float emptyChance = 0.4f;

    [Header("Текущая сложность уровня")]
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Гарантированные награды и торговец")]
    [SerializeField] private int minRewardCount = 2;           // минимум наград
    [SerializeField, Range(0f, 1f)] private float merchantChance = 0.8f;  // 80% шанс на торговца

    [Header("Расстояние до квеста")]
    [SerializeField, Range(1, 10)] private int minQuestDistance = 5;

    [Header("Визуализация")]
    [SerializeField] private MazeRenderer mazeRenderer;

    private TileData[,] tiles;
    private int tileCountX => roomCountX * 2 + 1;
    private int tileCountY => roomCountY * 2 + 1;

    public int RoomCountX => roomCountX;
    public int RoomCountY => roomCountY;
    public int TileCountX => tileCountX;
    public int TileCountY => tileCountY;

    private void Start()
    {
        //if (generateOnStart) GenerateDungeon();
    }
    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterDungeonGenerator(this);
    }

    public void GenerateDungeon()
    {
        // Если LocationData не назначен вручную — берём из GameManager
        if (locationData == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentLocation != null)
            {
                locationData = GameManager.Instance.CurrentLocation;
            }
            else
            {
                Debug.LogError("DungeonGenerator: Ни LocationDataSO, ни GameManager не найден!");
                return;
            }
        }

        // Применяем настройки локации
        roomCountX = locationData.roomCountX;
        roomCountY = locationData.roomCountY;
        minQuestDistance = locationData.minQuestDistance;

        emptyChance = locationData.emptyChance;
        enemyChance = locationData.enemyChance;
        randomEventChance = locationData.randomEventChance;
        rewardChance = locationData.rewardChance;
        merchantChance = locationData.merchantChance;

        InitializeTiles();
        CarveRoomsAndPassages();
        AssignEventsToRooms();

        if (mazeRenderer != null)
            mazeRenderer.Render();

        Debug.Log($"✅ Лабиринт сгенерирован для локации: <b>{locationData.locationName}</b> " +
                  $"({roomCountX}×{roomCountY} комнат)");
    }

    private void CarveRoomsAndPassages()
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int startRoom = new Vector2Int(0, 0);
        Vector2Int startTile = RoomToTile(startRoom);

        tiles[startTile.x, startTile.y].type = TileType.Floor;
        stack.Push(startRoom);

        while (stack.Count > 0)
        {
            Vector2Int currentRoom = stack.Pop();
            List<Vector2Int> neighbors = GetUnvisitedRoomNeighbors(currentRoom);

            if (neighbors.Count > 0)
            {
                stack.Push(currentRoom);
                Vector2Int chosenRoom = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];

                // Прорубаем проход между комнатами
                CarvePassage(currentRoom, chosenRoom);

                Vector2Int chosenTile = RoomToTile(chosenRoom);
                tiles[chosenTile.x, chosenTile.y].type = TileType.Floor;
                stack.Push(chosenRoom);
            }
        }
    }

    private Vector2Int RoomToTile(Vector2Int room) => new Vector2Int(room.x * 2 + 1, room.y * 2 + 1);

    private List<Vector2Int> GetUnvisitedRoomNeighbors(Vector2Int room)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int neighborRoom = room + dir;
            if (neighborRoom.x >= 0 && neighborRoom.x < roomCountX &&
                neighborRoom.y >= 0 && neighborRoom.y < roomCountY)
            {
                Vector2Int neighborTile = RoomToTile(neighborRoom);
                if (tiles[neighborTile.x, neighborTile.y].type == TileType.Wall)
                    neighbors.Add(neighborRoom);
            }
        }
        return neighbors;
    }

    private void CarvePassage(Vector2Int roomA, Vector2Int roomB)
    {
        Vector2Int tileA = RoomToTile(roomA);
        Vector2Int tileB = RoomToTile(roomB);
        Vector2Int mid = (tileA + tileB) / 2;
        tiles[mid.x, mid.y].type = TileType.Floor;
    }

    private void InitializeTiles()
    {
        tiles = new TileData[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = new TileData
                {
                    position = new Vector2Int(x, y),
                    type = TileType.Wall
                };
            }
        }
    }

    /// Пытается разместить тайл сайд-квеста ДО случайного назначения обычных событий.
    private void TryPlaceSideQuestTile()
    {
        if (QuestManager.Instance == null || locationData == null)
            return;

        if (string.IsNullOrEmpty(locationData.locationID))
            return;

        SideQuestEventSO sideQuest = QuestManager.Instance.GetActiveSideQuest(
            locationData.locationID,
            locationData.difficulty
        );

        if (sideQuest == null)
            return;

        int targetDistance = Mathf.Max(1, locationData.minQuestDistance - 2);

        Vector2Int sideQuestRoom = Vector2Int.zero;
        int attempts = 0;

        do
        {
            sideQuestRoom = new Vector2Int(
                UnityEngine.Random.Range(0, roomCountX),
                UnityEngine.Random.Range(0, roomCountY)
            );
            attempts++;
        }
        while (Mathf.Abs(sideQuestRoom.x) + Mathf.Abs(sideQuestRoom.y) < targetDistance && attempts < 150);

        Vector2Int tilePos = RoomToTile(sideQuestRoom);

        if (IsPassableTile(tilePos))
        {
            var tile = tiles[tilePos.x, tilePos.y];

            if (tile.eventType == EventType.Empty || tile.eventType == EventType.RandomEvent)
            {
                tile.eventType = EventType.SideQuest;
                tile.sideQuestData = sideQuest;           // ← сохраняем данные квеста прямо в тайл
                Debug.Log($"[SideQuest] Размещён: {sideQuest.questID} в тайле {tilePos}");
            }
        }
    }

    private void AssignEventsToRooms()
    {
        TryPlaceSideQuestTile();
        // 1. Случайное назначение ивентов на все Floor-тайлы
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                var tile = tiles[x, y];
                if (tile.type != TileType.Floor) continue;

                float roll = UnityEngine.Random.value;

                if (roll < emptyChance)
                    tile.eventType = EventType.Empty;
                else if (roll < emptyChance + enemyChance)
                    tile.eventType = EventType.Enemy;
                else if (roll < emptyChance + enemyChance + randomEventChance)
                    tile.eventType = EventType.RandomEvent;
                else if (roll < emptyChance + enemyChance + randomEventChance + rewardChance)
                    tile.eventType = EventType.Reward;
                else
                    tile.eventType = EventType.Empty;
            }
        }

        // 2. Стартовая зона всегда пустая
        Vector2Int startRoomTile = RoomToTile(new Vector2Int(0, 0));
        tiles[startRoomTile.x, startRoomTile.y].eventType = EventType.Empty;

        Vector2Int[] aroundStart = {
            startRoomTile + Vector2Int.up,
            startRoomTile + Vector2Int.down,
            startRoomTile + Vector2Int.left,
            startRoomTile + Vector2Int.right
        };
        foreach (var pos in aroundStart)
        {
            if (IsPassableTile(pos))
                tiles[pos.x, pos.y].eventType = EventType.Empty;
        }

        // 3. Путь отхода — ВСЕГДА на стартовой клетке
        tiles[startRoomTile.x, startRoomTile.y].eventType = EventType.Exit;

        // 3. Гарантируем минимум наград
        List<Vector2Int> rewardCandidates = new List<Vector2Int>();
        int currentRewards = 0;

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                var tile = tiles[x, y];
                if (tile.type != TileType.Floor) continue;
                if (tile.eventType == EventType.Quest ||
                    tile.position == startRoomTile) continue;

                if (tile.eventType == EventType.Reward)
                    currentRewards++;
                else
                    rewardCandidates.Add(new Vector2Int(x, y));
            }
        }

        while (currentRewards < minRewardCount && rewardCandidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, rewardCandidates.Count);
            Vector2Int pos = rewardCandidates[index];
            tiles[pos.x, pos.y].eventType = EventType.Reward;
            rewardCandidates.RemoveAt(index);
            currentRewards++;
        }

        // 4. Торговец (ровно один, с шансом 80%)
        if (UnityEngine.Random.value < merchantChance)
        {
            List<Vector2Int> merchantCandidates = new List<Vector2Int>();

            for (int x = 0; x < tileCountX; x++)
            {
                for (int y = 0; y < tileCountY; y++)
                {
                    var tile = tiles[x, y];
                    if (tile.type == TileType.Floor &&
                        tile.eventType != EventType.Quest &&
                        tile.position != startRoomTile)
                    {
                        merchantCandidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (merchantCandidates.Count > 0)
            {
                Vector2Int chosen = merchantCandidates[UnityEngine.Random.Range(0, merchantCandidates.Count)];
                tiles[chosen.x, chosen.y].eventType = EventType.Merchant;
                Debug.Log($"🛒 Торговец появился в тайле {chosen}");
            }
        }

        
        Vector2Int questRoom;
        int distance;
        int attempts = 0;

        do
        {
            questRoom = new Vector2Int(UnityEngine.Random.Range(0, roomCountX), UnityEngine.Random.Range(0, roomCountY));
            distance = Mathf.Abs(questRoom.x) + Mathf.Abs(questRoom.y);
            attempts++;
        }
        while (distance < minQuestDistance && attempts < 100);

        Vector2Int questTile = RoomToTile(questRoom);
        tiles[questTile.x, questTile.y].eventType = EventType.Quest;

        Debug.Log($"🎯 Квест в комнате {questRoom} | Расстояние: {distance} | Наград: {currentRewards}");
    }


    // ==================== ПОЛЕЗНЫЕ МЕТОДЫ ДЛЯ ДРУГИХ СКРИПТОВ ====================
    public TileData GetTile(Vector2Int tilePos)
    {
        if (tilePos.x >= 0 && tilePos.x < tileCountX && tilePos.y >= 0 && tilePos.y < tileCountY)
            return tiles[tilePos.x, tilePos.y];
        return null;
    }

    public EventType GetEventAtRoom(Vector2Int roomPos)
    {
        Vector2Int tilePos = RoomToTile(roomPos);
        return GetTile(tilePos)?.eventType ?? EventType.Empty;
    }

    public bool IsPassableTile(Vector2Int tilePos)
    {
        var tile = GetTile(tilePos);
        return tile != null && tile.type == TileType.Floor;
    }

    // Очищает клетку после первого посещения (кроме квеста)
    public void MarkAsVisited(Vector2Int tilePos)
    {
        var tile = GetTile(tilePos);
        if (tile == null || tile.type != TileType.Floor) return;

        // Exit — особый случай, никогда не очищается
        if (tile.eventType != EventType.Quest && tile.eventType != EventType.Exit)
        {
            tile.eventType = EventType.Empty;
            tile.visited = true;
        }
    }

    public bool IsVisited(Vector2Int tilePos)
    {
        var tile = GetTile(tilePos);
        return tile != null && tile.visited;
    }

    // ====================== ОТНОСИТЕЛЬНОЕ ДВИЖЕНИЕ ======================
    public bool CanMoveFromTile(Vector2Int tilePos, Direction direction)
    {
        Vector2Int neighbor = GetNeighborTile(tilePos, direction);
        return IsPassableTile(neighbor);
    }

    public Vector2Int GetNeighborTile(Vector2Int tilePos, Direction direction)
    {
        return direction switch
        {
            Direction.North => tilePos + Vector2Int.up,
            Direction.East => tilePos + Vector2Int.right,
            Direction.South => tilePos + Vector2Int.down,
            Direction.West => tilePos + Vector2Int.left,
            _ => tilePos
        };
    }

    public Direction TurnLeft(Direction current) => current switch
    {
        Direction.North => Direction.West,
        Direction.East => Direction.North,
        Direction.South => Direction.East,
        Direction.West => Direction.South,
        _ => current
    };

    public Direction TurnRight(Direction current) => current switch
    {
        Direction.North => Direction.East,
        Direction.East => Direction.South,
        Direction.South => Direction.West,
        Direction.West => Direction.North,
        _ => current
    };

    public Direction TurnBack(Direction current) => current switch
    {
        Direction.North => Direction.South,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        Direction.West => Direction.East,
        _ => current
    };

    // Проверяет, есть ли за стеной пустой тайл (секретный проход)
    // Проверяет наличие секретного прохода ТОЛЬКО в НЕПОСЕЩЁННУЮ клетку
    public bool TryGetSecretPassage(Vector2Int fromTile, out Direction outDirection, out Vector2Int outTargetTile)
    {
        outDirection = Direction.North;
        outTargetTile = fromTile;

        Direction[] dirs = { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (var dir in dirs)
        {
            Vector2Int step1 = GetNeighborTile(fromTile, dir);   // должен быть Wall
            Vector2Int step2 = GetNeighborTile(step1, dir);      // должен быть Floor + Empty + НЕ ПОСЕЩЁННЫЙ

            var tile1 = GetTile(step1);
            var tile2 = GetTile(step2);

            bool isValid = 
                tile1 != null && tile1.type == TileType.Wall &&                     // за текущей клеткой стена
                tile2 != null && tile2.type == TileType.Floor &&                    // за стеной — пол
                tile2.eventType == EventType.Empty &&                               // и он пустой
                !tile2.visited;                                                     // ещё не посещён

            // Для отладки (можно потом убрать или закомментировать)
            //Debug.Log($"[SecretPassage] Проверка {dir} от {fromTile}: step2={step2} | visited={tile2?.visited} → {isValid}");

            if (isValid)
            {
                outDirection = dir;
                outTargetTile = step2;
                return true;
            }
        }
        return false;
    }
}