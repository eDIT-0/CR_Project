using UnityEngine;

[CreateAssetMenu(fileName = "New Location", menuName = "Roguelike/Location Data")]
public class LocationDataSO : ScriptableObject
{
    [Header("Общая информация")]
    public string locationName = "Новая локация";
    [TextArea] public string description = "Описание локации";

    [Header("Идентификатор локации")]
    public string locationID;          //например: "slums", "factory", "dungeon"

    [Header("Сложность")]
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Генерация лабиринта")]
    public int roomCountX = 8;
    public int roomCountY = 8;
    public int minQuestDistance = 5;

    [Header("Шансы ивентов (0–1)")]
    [Range(0f, 1f)] public float emptyChance = 0.4f;
    [Range(0f, 1f)] public float enemyChance = 0.25f;
    [Range(0f, 1f)] public float randomEventChance = 0.2f;
    [Range(0f, 1f)] public float rewardChance = 0.1f;
    [Range(0f, 1f)] public float merchantChance = 0.15f;

    [Header("Случайные ивенты")]
    public RandomEventListSO standardRandomEvents;   // общие ивенты
    public RandomEventListSO uniqueRandomEvents;     // уникальные для этой локации (опционально)

    [Header("Дополнительно")]
    public int minRewardCount = 2;
    public bool allowWallHoles = true;               // можно отключить дыры в стене на некоторых локациях
}