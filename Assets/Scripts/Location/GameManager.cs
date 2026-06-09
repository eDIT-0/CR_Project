using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Текущая локация")]
    [SerializeField] private LocationDataSO currentLocation;

    // Ссылки на генераторы (регистрируются автоматически)
    private DungeonGenerator dungeonGenerator;
    private RandomEventSystem randomEventSystem;

    public LocationDataSO CurrentLocation => currentLocation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ====================== РЕГИСТРАЦИЯ ======================
    public void RegisterDungeonGenerator(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }

    public void RegisterRandomEventSystem(RandomEventSystem system)
    {
        randomEventSystem = system;
    }

    // ====================== ЗАПУСК УРОВНЯ ======================
    public void StartLevel(LocationDataSO location)
    {
        if (location == null)
        {
            Debug.LogError("GameManager: Передана пустая LocationDataSO!");
            return;
        }

        SetCurrentLocation(location);

        // Генерируем лабиринт 
        if (dungeonGenerator != null)
            dungeonGenerator.GenerateDungeon();
        else
            Debug.LogWarning("GameManager: DungeonGenerator не зарегистрирован в сцене!");

        // Передаём локацию в систему ивентов
        if (randomEventSystem != null)
            randomEventSystem.SetLocationData(location);
        else
            Debug.LogWarning("GameManager: RandomEventSystem не зарегистрирован в сцене!");
    }

    private void SetCurrentLocation(LocationDataSO location)
    {
        currentLocation = location;
        Debug.Log($"📍 Загружена локация: <b>{location.locationName}</b>");
    }
}