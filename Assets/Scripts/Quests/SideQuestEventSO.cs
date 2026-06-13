using UnityEngine;

[CreateAssetMenu(fileName = "New SideQuest", menuName = "Quests/Side Quest Event")]
public class SideQuestEventSO : ScriptableObject
{
    [Header("Основная информация")]
    public string questID;                    // Уникальный идентификатор (например: side-slums-normal-01)
    public string locationID;                 // К какой локации привязан (slums, factory и т.д.)
    public Difficulty difficulty;             // Для какого уровня сложности предназначен

    [Header("Нарратив")]
    public string questGiverName;             // Имя квестодателя (для логов и будущего UI)

    [Header("Событие")]
    [TextArea(3, 6)]
    public string description;                // Краткое описание (для дебага)

    // Здесь позже можно будет добавить ссылку на VN-сцену / DialogueAsset
    // public DialogueAsset dialogueAsset;
}