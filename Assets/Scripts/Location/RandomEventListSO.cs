using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Random Event List", menuName = "Roguelike/Random Event List")]
public class RandomEventListSO : ScriptableObject
{
    [Serializable]
    public class EventEntry
    {
        public string title = "Новый ивент";
        [TextArea(3, 6)] public string description = "Описание события...";
        [Range(1, 100)] public int weight = 30;
    }

    public List<EventEntry> events = new List<EventEntry>();
}

