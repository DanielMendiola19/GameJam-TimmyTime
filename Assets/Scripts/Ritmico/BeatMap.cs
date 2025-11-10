// Archivo: BeatMap.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBeatMap", menuName = "RhythmGame/BeatMap")]
public class BeatMap : ScriptableObject
{
    public List<NoteData> notes = new List<NoteData>();
}
