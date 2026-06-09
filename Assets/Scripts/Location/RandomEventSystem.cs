using UnityEngine;

public class RandomEventSystem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private LocationDataSO locationData;   // ← теперь берём из локации

    public void SetLocationData(LocationDataSO data)
    {
        locationData = data;
    }

    private int wallHoleUsesThisLevel = 0;
    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRandomEventSystem(this);   // ← добавили
    }

    public void TriggerRandomEvent(Vector2Int currentTile)
    {
        if (locationData == null)
        {
            Debug.LogWarning("RandomEventSystem: LocationDataSO не назначен!");
            return;
        }

        // 1. Определяем, какой список использовать (с шансом 60/40)
        RandomEventListSO chosenList = locationData.standardRandomEvents; // по умолчанию — стандартные

        bool hasUnique = locationData.uniqueRandomEvents != null &&
                         locationData.uniqueRandomEvents.events.Count > 0;

        if (hasUnique && UnityEngine.Random.value < 0.6f)   // 60% шанс на уникальные
        {
            chosenList = locationData.uniqueRandomEvents;
        }

        if (chosenList == null || chosenList.events.Count == 0)
        {
            Debug.LogWarning("RandomEventSystem: Нет доступных ивентов в выбранном списке.");
            return;
        }

        // 2. Проверяем секретный проход (остаётся без изменений)
        bool hasSecretPassage = false;
        Direction secretDir = Direction.North;
        Vector2Int secretTarget = currentTile;

        var generator = FindObjectOfType<DungeonGenerator>();
        if (generator != null)
            hasSecretPassage = generator.TryGetSecretPassage(currentTile, out secretDir, out secretTarget);

        // 3. Приоритет "Дыра в стене"
        if (hasSecretPassage && wallHoleUsesThisLevel == 0 && UnityEngine.Random.value < 0.7f)
        {
            wallHoleUsesThisLevel++;
            Debug.Log("🕳️ Сработал уникальный ивент: Дыра в стене!");
            HandleWallHole(secretDir, secretTarget);
            return;
        }

        // 4. Обычный рандом из выбранного списка
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
                Debug.Log($"❓ {listType} ИВЕНТ ({locationData.locationName}): <b>{ev.title}</b>\n{ev.description}");
                return;
            }
        }
    }

    private void HandleWallHole(Direction direction, Vector2Int targetTile)
    {
        
        Debug.Log($"🕳️ Дыра в стене! Попытка пройти ({direction})...");

        if (UnityEngine.Random.value < 0.5f)
        {
            Debug.Log("✅ Успешно! Вы пролезли через дыру.");
            var player = FindObjectOfType<PlayerMovement>();
            if (player != null)
                player.TeleportToSecretTile(targetTile, direction);
        }
        else
        {
            Debug.Log("❌ Не удалось пролезть... слишком узко.");
        }
    }
}