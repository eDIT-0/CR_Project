using UnityEngine;

[CreateAssetMenu(fileName = "New Random Event", menuName = "Roguelike/Random Event")]
public class RandomEventSO : ScriptableObject
{
    public string title;
    [TextArea(3, 6)] public string description;
    [Range(1, 100)] public int weight = 30;

    // Позже сюда можно будет добавить UnityEvent, ссылки на эффекты и т.д.
}