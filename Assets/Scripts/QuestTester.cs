using UnityEngine;

public class QuestTester : MonoBehaviour
{
    [Header("Тестовый квест")]
    public SideQuestEventSO testQuest;   // Перетащи сюда ассет квеста

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager не найден в сцене!");
            return;
        }

        if (testQuest == null)
        {
            Debug.LogWarning("Не назначен тестовый квест в инспекторе!");
            return;
        }

        // Активируем квест
        QuestManager.Instance.ActivateSideQuest(testQuest);

        // Проверяем, что он активировался
        bool isActive = QuestManager.Instance.HasActiveSideQuest(testQuest.locationID, testQuest.difficulty);
        Debug.Log($"Квест активен? {isActive}");

        // Пробуем получить его
        var activeQuest = QuestManager.Instance.GetActiveSideQuest(testQuest.locationID, testQuest.difficulty);
        if (activeQuest != null)
        {
            Debug.Log($"Получили активный квест: {activeQuest.questID}");
        }
    }

    // Для теста через контекстное меню в инспекторе
    [ContextMenu("Завершить тестовый квест")]
    private void CompleteTestQuest()
    {
        if (testQuest != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteSideQuest(testQuest);
            Debug.Log("Квест завершён через контекстное меню");
        }
    }
}