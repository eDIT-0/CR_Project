using UnityEngine;

public class RandomEventSystem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private LocationDataSO locationData;
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private PlayerMovement playerMovement;

    private int wallHoleUsesThisLevel = 0;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRandomEventSystem(this);
    }

    public void SetLocationData(LocationDataSO data)
    {
        locationData = data;
    }

    public void TriggerRandomEvent(Vector2Int currentTile)
    {
        if (locationData == null)
        {
            Debug.LogWarning("RandomEventSystem: LocationDataSO не назначен!");
            return;
        }

        // 1. Выбор списка событий (60/40)
        RandomEventListSO chosenList = locationData.standardRandomEvents;

        bool hasUnique = locationData.uniqueRandomEvents != null &&
                         locationData.uniqueRandomEvents.events.Count > 0;

        if (hasUnique && UnityEngine.Random.value < 0.6f)
            chosenList = locationData.uniqueRandomEvents;

        if (chosenList == null || chosenList.events.Count == 0)
        {
            Debug.LogWarning("RandomEventSystem: Нет доступных ивентов.");
            return;
        }

        // 2. Проверка секретного прохода
        bool hasSecretPassage = false;
        Direction secretDir = Direction.North;
        Vector2Int secretTarget = currentTile;

        if (dungeonGenerator != null)
            hasSecretPassage = dungeonGenerator.TryGetSecretPassage(currentTile, out secretDir, out secretTarget);

        // 3. Приоритет "Дыра в стене"
        if (hasSecretPassage && wallHoleUsesThisLevel == 0 && UnityEngine.Random.value < 0.7f)
        {
            wallHoleUsesThisLevel++;
            Debug.Log("Сработал уникальный ивент: Дыра в стене!");
            HandleWallHole(secretDir, secretTarget);
            return;
        }

        // 4. Обычный рандом
        int totalWeight = 0;
        foreach (var ev in chosenList.events)
            totalWeight += ev.weight;

        if (totalWeight == 0) return;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int current = 0;

        foreach (var ev in chosenList.events)
        {
            current += ev.weight;
            if (roll < current)
            {
                string listType = (chosenList == locationData.uniqueRandomEvents) ? "УНИКАЛЬНЫЙ" : "СТАНДАРТНЫЙ";
                Debug.Log($"{listType} ИВЕНТ ({locationData.locationName}): <b>{ev.title}</b>\n{ev.description}");
                return;
            }
        }
    }

    private void HandleWallHole(Direction direction, Vector2Int targetTile)
    {
        Debug.Log($"Дыра в стене! Попытка пройти ({direction})...");

        if (UnityEngine.Random.value < 0.5f)
        {
            Debug.Log("Успешно! Вы пролезли через дыру.");
            if (playerMovement != null)
                playerMovement.TeleportToSecretTile(targetTile, direction);
        }
        else
        {
            Debug.Log("Не удалось пролезть... слишком узко.");
        }
    }
}