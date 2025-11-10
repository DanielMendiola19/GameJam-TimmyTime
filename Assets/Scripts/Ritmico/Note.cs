using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public Transform targetPoint;
    public bool wasHit = false;
    public int lane;

    protected virtual void Update()
    {
        if (targetPoint != null && !wasHit)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NoteTouchPerfect"))
        {
            // La nota entró al área de PERFECT
            NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
            if (detector != null && !wasHit)
            {
                detector.RegisterNoteInPerfectZone(this, lane);
            }
        }
        else if (other.CompareTag("NoteTouchGreat"))
        {
            // La nota entró al área de GREAT
            NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
            if (detector != null && !wasHit)
            {
                detector.RegisterNoteInGreatZone(this, lane);
            }
        }
        else if (other.CompareTag("NoteDestroyer"))
        {
            if (!wasHit)
            {
                // Nota llegó al final sin ser presionada - MISS
                NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
                if (detector != null)
                {
                    detector.MissNote(lane);
                }
            }
            Destroy(gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NoteTouchPerfect"))
        {
            // La nota salió del área de PERFECT
            NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
            if (detector != null)
            {
                detector.UnregisterNoteFromPerfectZone(this, lane);
            }
        }
        else if (other.CompareTag("NoteTouchGreat"))
        {
            // La nota salió del área de GREAT
            NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
            if (detector != null)
            {
                detector.UnregisterNoteFromGreatZone(this, lane);
            }
        }
    }

    public void MarkAsHit()
    {
        wasHit = true;

        // Remover de ambas zonas por seguridad
        NoteHitDetector detector = FindObjectOfType<NoteHitDetector>();
        if (detector != null)
        {
            detector.UnregisterNoteFromPerfectZone(this, lane);
            detector.UnregisterNoteFromGreatZone(this, lane);

            // Spawn partículas en la posición de la nota
            detector.SpawnSparkEffect(transform.position);
        }

        // Opcional: cambiar color antes de destruir
        if (GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = Color.green;

        Destroy(gameObject, 0.1f);
    }

}