using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public BeatMap beatMap;
    public GameObject notePrefab;
    public Transform[] lanePoints;
    public Transform[] targetPoints;
    public AudioSource song;

    private int nextNoteIndex = 0;
    private HashSet<string> spawnedNotes = new HashSet<string>();

    [HideInInspector]
    public bool canSpawn = true;


    void Update()
    {
        if (!canSpawn) return;

        if (beatMap == null || song == null) return;
        float songTime = song.time;

        while (nextNoteIndex < beatMap.notes.Count && songTime >= beatMap.notes[nextNoteIndex].time)
        {
            var nd = beatMap.notes[nextNoteIndex];
            string noteKey = $"{nd.time:F6}_{nd.lane}";

            if (!spawnedNotes.Contains(noteKey))
            {
                SpawnNote(nd);
                spawnedNotes.Add(noteKey);
            }
            nextNoteIndex++;
        }
    }

    void SpawnNote(NoteData data)
    {
        if (data.lane < 0 || data.lane >= lanePoints.Length)
        {
            Debug.LogWarning("Lane inválida: " + data.lane);
            return;
        }

        GameObject note = Instantiate(notePrefab, lanePoints[data.lane].position, Quaternion.identity);
        Note noteComp = note.GetComponent<Note>();
        noteComp.targetPoint = targetPoints[data.lane];
        noteComp.lane = data.lane; // Asignar la lane directamente a la nota

        float distance = Vector3.Distance(lanePoints[data.lane].position, targetPoints[data.lane].position);
        float timeToTarget = Mathf.Max(0.001f, data.time - song.time);
        if (timeToTarget > 0.05f) noteComp.speed = distance / timeToTarget;
    }

    public void ClearAllNotes()
    {
        Note[] activeNotes = FindObjectsOfType<Note>();
        foreach (Note note in activeNotes)
        {
            Destroy(note.gameObject);
        }

        spawnedNotes.Clear();
        nextNoteIndex = 0;
    }
}