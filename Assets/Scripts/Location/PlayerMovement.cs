using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private DungeonGenerator generator;
    [SerializeField] private MazeRenderer mazeRenderer;
    [SerializeField] private RandomEventSystem eventSystem;

    [Header("Настройки")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float cellSize = 1f;

    private Vector2Int currentTile = new Vector2Int(1, 1);        // стартовая комната
    private Direction currentDirection = Direction.East;          // начальный взгляд — вправо (можно поменять)

    private bool isMoving = false;

    private void Start()
    {
        TeleportToTile(currentTile);
        SetInitialDirection();   
    }

    private void SetInitialDirection()
    {
        if (generator == null)
        {
            currentDirection = Direction.East;
            return;
        }

        // Приоритет направлений (можно поменять)
        Direction[] priority = { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (var dir in priority)
        {
            Vector2Int neighbor = generator.GetNeighborTile(currentTile, dir);
            if (generator.IsPassableTile(neighbor))
            {
                currentDirection = dir;
                Debug.Log($"Игрок стартовал лицом на {currentDirection} (к открытой клетке)");
                return;
            }
        }

        // На всякий случай (если вдруг все направления закрыты)
        currentDirection = Direction.East;
        Debug.LogWarning("Нет открытого направления от стартовой клетки!");
    }

    private void Update()
    {
        if (isMoving) return;

        // === ТЕСТОВЫЕ КЛАВИШИ (потом заменим на UI-кнопки) ===
        if (Keyboard.current.wKey.wasPressedThisFrame)      // W — ВПЕРЁД
            TryMoveRelative(RelativeAction.Forward);
        else if (Keyboard.current.sKey.wasPressedThisFrame) // S — НАЗАД
            TryMoveRelative(RelativeAction.Back);
        else if (Keyboard.current.aKey.wasPressedThisFrame) // A — ВЛЕВО
            TryMoveRelative(RelativeAction.Left);
        else if (Keyboard.current.dKey.wasPressedThisFrame) // D — ВПРАВО
            TryMoveRelative(RelativeAction.Right);
    }

    private enum RelativeAction { Forward, Back, Left, Right }

    private void TryMoveRelative(RelativeAction action)
    {
        Vector2Int targetTile = currentTile;
        Direction newDirection = currentDirection;

        switch (action)
        {
            case RelativeAction.Forward:
                targetTile = generator.GetNeighborTile(currentTile, currentDirection);
                break;

            case RelativeAction.Back:
                newDirection = generator.TurnBack(currentDirection);
                targetTile = generator.GetNeighborTile(currentTile, newDirection);
                break;

            case RelativeAction.Left:
                newDirection = generator.TurnLeft(currentDirection);
                targetTile = generator.GetNeighborTile(currentTile, newDirection);
                break;

            case RelativeAction.Right:
                newDirection = generator.TurnRight(currentDirection);
                targetTile = generator.GetNeighborTile(currentTile, newDirection);
                break;
        }

        // Проверяем, можно ли туда идти
        if (generator.IsPassableTile(targetTile))
        {
            currentDirection = newDirection;   // поворачиваемся
            StartCoroutine(MoveToTile(targetTile));
        }
        else
        {
            Debug.Log("🛑 Стена в этом направлении!");
        }
    }

    private System.Collections.IEnumerator MoveToTile(Vector2Int target)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(target.x * cellSize, target.y * cellSize, 0) + mazeRenderer.transform.position;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed);
            yield return null;
        }

        transform.position = endPos;
        currentTile = target;
        isMoving = false;

        OnEnterNewTile();
    }

    private void TeleportToTile(Vector2Int tile)
    {
        Vector3 worldPos = new Vector3(tile.x * cellSize, tile.y * cellSize, 0) + mazeRenderer.transform.position;
        transform.position = worldPos;
        currentTile = tile;
    }

    private void OnEnterNewTile()
    {
        var tile = generator.GetTile(currentTile);
        if (tile == null || tile.type != TileType.Floor) return;

        // === 1. Сначала обрабатываем ивент (если клетка ещё не посещена) ===
        if (!tile.visited)
        {
            Debug.Log($"📍 Игрок вошёл в новую клетку {currentTile} | Смотрит: {currentDirection} | Ивент: {tile.eventType}");

            switch (tile.eventType)
            {
                case EventType.Enemy:
                    Debug.Log("⚔️ Стычка с противником! (здесь будет бой)");
                    break;
                case EventType.RandomEvent:
                    Debug.Log("❓ Запуск случайного события...");
                    if (eventSystem != null)
                        eventSystem.TriggerRandomEvent(currentTile);
                    break;
                case EventType.Reward:
                    Debug.Log("🎁 Получена награда!");
                    break;
                case EventType.Merchant:
                    Debug.Log("🛒 Встречен торговец! (здесь будет магазин)");
                    break;
                case EventType.Quest:
                    Debug.Log("🎉 ЦЕЛЬ ДОСТИГНУТА! (победа на уровне)");
                    break;
                case EventType.Exit:
                    Debug.Log("🚪 Вы вернулись на стартовую клетку. Можно покинуть уровень!");
                    break;
                case EventType.Empty:
                    Debug.Log("🌿 Пустая клетка (передышка)");
                    break;
                case EventType.SideQuest:
                    Debug.Log("Наступил на тайл сайд-квеста!");

                    if (tile.sideQuestData != null)
                    {
                        Debug.Log($"Сработал сайд-квест: {tile.sideQuestData.questID}");
                        // Здесь позже будет запуск VN-сцены / события
                    }

                    // === Очищаем тайл после срабатывания ===
                    tile.eventType = EventType.Empty;
                    tile.sideQuestData = null;
                    break;
            }
        }
        else
        {
            Debug.Log($"👟 Игрок вернулся в уже посещённую клетку {currentTile} | Смотрит: {currentDirection}");
        }

        // === 2. Только ПОСЛЕ обработки ивента очищаем клетку ===
        // (Quest и Exit никогда не исчезают)
        if (tile.eventType != EventType.Quest && tile.eventType != EventType.Exit)
        {
            generator.MarkAsVisited(currentTile);
        }

        // === 3. Обновляем визуализацию (чтобы пустые клетки сразу посерели) ===
        if (mazeRenderer != null)
            mazeRenderer.Refresh();
    }

    // Специально для секретного прохода
    public void TeleportToSecretTile(Vector2Int targetTile, Direction facingDirection)
    {
        Vector3 worldPos = new Vector3(targetTile.x * cellSize, targetTile.y * cellSize, 0) + mazeRenderer.transform.position;
        transform.position = worldPos;
        currentTile = targetTile;
        currentDirection = facingDirection;     // ← смотрим в сторону прохода

        OnEnterNewTile();   // сразу обработаем ивент на новом тайле
    }
}