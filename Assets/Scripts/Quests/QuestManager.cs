using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Активные сайд-квесты")]
    [SerializeField] private List<SideQuestEventSO> activeSideQuests = new List<SideQuestEventSO>();

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

    /// Активирует сайд-квест (вызывается из хаба при взятии задания)
    public void ActivateSideQuest(SideQuestEventSO quest)
    {
        if (quest == null) return;

        // Проверяем, нет ли уже активного квеста для этой же локации + сложности
        if (HasActiveSideQuest(quest.locationID, quest.difficulty))
        {
            Debug.LogWarning($"Сайд-квест для локации {quest.locationID} ({quest.difficulty}) уже активен.");
            return;
        }

        activeSideQuests.Add(quest);
        Debug.Log($"Сайд-квест активирован: {quest.questID}");
    }

    /// Возвращает активный сайд-квест для указанной локации и сложности (если есть)
    public SideQuestEventSO GetActiveSideQuest(string locationID, Difficulty difficulty)
    {
        foreach (var quest in activeSideQuests)
        {
            if (quest.locationID == locationID && quest.difficulty == difficulty)
            {
                return quest;
            }
        }
        return null;
    }

    /// Проверяет, есть ли активный сайд-квест для данной локации и сложности
    public bool HasActiveSideQuest(string locationID, Difficulty difficulty)
    {
        return GetActiveSideQuest(locationID, difficulty) != null;
    }

    /// Завершает сайд-квест (вызывается после прохождения события)
    public void CompleteSideQuest(SideQuestEventSO quest)
    {
        if (quest == null) return;

        if (activeSideQuests.Remove(quest))
        {
            Debug.Log($"Сайд-квест завершён: {quest.questID}");
            // Здесь позже можно будет добавить в список выполненных, если понадобится
        }
    }

    // Для дебага
    public List<SideQuestEventSO> GetAllActiveSideQuests()
    {
        return new List<SideQuestEventSO>(activeSideQuests);
    }
}