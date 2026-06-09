using UnityEngine;

public class LevelTestLoader : MonoBehaviour
{
    [Header("Выбери локацию для теста")]
    [SerializeField] private LocationDataSO locationToLoad;

    private void Start()
    {
        if (GameManager.Instance != null && locationToLoad != null)
        {
            GameManager.Instance.StartLevel(locationToLoad);
        }
        else
        {
            Debug.LogError("LevelTestLoader: GameManager или LocationDataSO не назначены!");
        }
    }
}