using UnityEngine;

public class NoteTouch : MonoBehaviour
{
    public int lane;
    private Note currentNote; // Nota dentro del collider

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null)
        {
            currentNote = note;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note != null && note == currentNote)
        {
            currentNote = null;
        }
    }

    public bool HasNoteInside() => currentNote != null;

    public Note GetNoteInside() => currentNote;
}
