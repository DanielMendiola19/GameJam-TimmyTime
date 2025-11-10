using UnityEngine;
using System.Collections.Generic;

public class NoteHitDetector : MonoBehaviour
{
    [HideInInspector]
    public KeyCode[] laneKeys;

    [Header("Referencias")]
    public NoteResultManager resultManager;

    [Header("Efectos de Partículas")]
    public GameObject electricSparkPrefab; // Prefab de partículas

    [Header("Tomacorrientes (Visuales)")]
    public Renderer[] outletRenderers; // Los tomacorrientes visuales (uno por lane)
    public Material outletNormalMaterial;
    public Material outletPressedMaterial;
    public float flashDuration = 0.1f;

    [Header("Game Systems")]
    public HealthSystem healthSystem;

    [HideInInspector]
    public bool canProcessInput = true;


    // Estadísticas públicas
    public int CurrentScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }
    public int PerfectHits { get; private set; }
    public int GreatHits { get; private set; }
    public int FailHits { get; private set; }
    public int MissHits { get; private set; }
    public int TotalHits { get; private set; }
    public float Accuracy { get; private set; }

    private float[] keyPressTime;
    private bool[] keyPressedThisFrame;

    private Dictionary<int, List<Note>> notesInPerfectZone = new Dictionary<int, List<Note>>();
    private Dictionary<int, List<Note>> notesInGreatZone = new Dictionary<int, List<Note>>();

    void Start()
    {
        canProcessInput = true;

        // Obtener las keys actuales del SettingsManager
        laneKeys = new KeyCode[4];
        laneKeys[0] = SettingsManager.Instance.GetKey("Carril 1");
        laneKeys[1] = SettingsManager.Instance.GetKey("Carril 2");
        laneKeys[2] = SettingsManager.Instance.GetKey("Carril 3");
        laneKeys[3] = SettingsManager.Instance.GetKey("Carril 4");

        for (int i = 0; i < laneKeys.Length; i++)
        {
            notesInPerfectZone[i] = new List<Note>();
            notesInGreatZone[i] = new List<Note>();
        }

        keyPressTime = new float[laneKeys.Length];
        keyPressedThisFrame = new bool[laneKeys.Length];

        for (int i = 0; i < laneKeys.Length; i++)
        {
            keyPressTime[i] = -1f;
            keyPressedThisFrame[i] = false;
        }

        ResetOutletMaterials();

        if (resultManager != null)
            resultManager.UpdateAccuracyText(100f);
    }

    void Update()
    {
        if (!canProcessInput) return;

        for (int i = 0; i < laneKeys.Length; i++)
            keyPressedThisFrame[i] = false;

        for (int lane = 0; lane < laneKeys.Length; lane++)
        {
            if (Input.GetKeyDown(laneKeys[lane]))
            {
                keyPressedThisFrame[lane] = true;
                keyPressTime[lane] = Time.time;
            }
        }

        ProcessAllPressedKeys();
        UpdateOutletVisuals();
    }

    private void ProcessAllPressedKeys()
    {
        for (int lane = 0; lane < laneKeys.Length; lane++)
        {
            if (!keyPressedThisFrame[lane]) continue;

            bool hitAnyNote = false;
            int notesHitThisPress = 0;

            // PERFECT
            if (notesInPerfectZone[lane].Count > 0)
            {
                foreach (Note note in new List<Note>(notesInPerfectZone[lane]))
                {
                    if (!note.wasHit && notesHitThisPress == 0)
                    {
                        note.MarkAsHit();
                        HitNote("PERFECT", lane, 100);
                        PerfectHits++;
                        TotalHits++;
                        notesInPerfectZone[lane].Remove(note);
                        hitAnyNote = true;
                        notesHitThisPress++;
                        break;
                    }
                }
            }

            // GREAT
            if (!hitAnyNote && notesInGreatZone[lane].Count > 0)
            {
                foreach (Note note in new List<Note>(notesInGreatZone[lane]))
                {
                    if (!note.wasHit && notesHitThisPress == 0)
                    {
                        note.MarkAsHit();
                        HitNote("GREAT", lane, 50);
                        GreatHits++;
                        TotalHits++;
                        notesInGreatZone[lane].Remove(note);
                        hitAnyNote = true;
                        notesHitThisPress++;
                        break;
                    }
                }
            }

            // FAIL
            if (!hitAnyNote)
            {
                FailNote(lane);
            }
        }
    }

    // Zonas
    public void RegisterNoteInPerfectZone(Note note, int lane)
    {
        if (!notesInPerfectZone[lane].Contains(note))
            notesInPerfectZone[lane].Add(note);
    }

    public void UnregisterNoteFromPerfectZone(Note note, int lane)
    {
        notesInPerfectZone[lane].Remove(note);
    }

    public void RegisterNoteInGreatZone(Note note, int lane)
    {
        if (!notesInGreatZone[lane].Contains(note))
            notesInGreatZone[lane].Add(note);
    }

    public void UnregisterNoteFromGreatZone(Note note, int lane)
    {
        notesInGreatZone[lane].Remove(note);
    }

    // Resultados
    private void HitNote(string hitType, int lane, int points)
    {
        CurrentScore += points;
        CurrentCombo++;
        MaxCombo = Mathf.Max(MaxCombo, CurrentCombo);
        UpdateAccuracy();

        healthSystem?.OnNoteHit(hitType);
        resultManager?.OnNoteHit(hitType, CurrentScore, CurrentCombo);
    }

    private void FailNote(int lane)
    {
        CurrentCombo = 0;
        FailHits++;
        TotalHits++;
        UpdateAccuracy();

        healthSystem?.OnNoteFail("FAIL");
        resultManager?.OnNoteFail("FAIL", CurrentScore, CurrentCombo);
    }

    public void MissNote(int lane)
    {
        CurrentCombo = 0;
        MissHits++;
        TotalHits++;
        UpdateAccuracy();

        healthSystem?.OnNoteFail("MISS");
        resultManager?.OnNoteFail("MISS", CurrentScore, CurrentCombo);
    }

    private void UpdateAccuracy()
    {
        if (TotalHits == 0)
        {
            Accuracy = 100f;
        }
        else
        {
            int totalNotes = PerfectHits + GreatHits + FailHits + MissHits;
            float totalScore = (PerfectHits * 1f) + (GreatHits * 0.3f) - (FailHits * 1f) - (MissHits * 1f);
            Accuracy = Mathf.Max(0f, (totalScore / totalNotes) * 100f);
        }
        resultManager?.UpdateAccuracyText(Accuracy);
    }

    // 🔌 ACTUALIZACIÓN VISUAL DE TOMACORRIENTES 🔌
    private void UpdateOutletVisuals()
    {
        for (int lane = 0; lane < outletRenderers.Length; lane++)
        {
            if (outletRenderers[lane] == null) continue;

            bool pressed = keyPressedThisFrame[lane] ||
                           (keyPressTime[lane] > 0 && Time.time - keyPressTime[lane] < flashDuration);

            outletRenderers[lane].material = pressed ? outletPressedMaterial : outletNormalMaterial;

            if (!pressed && keyPressTime[lane] > 0 && Time.time - keyPressTime[lane] >= flashDuration)
                keyPressTime[lane] = -1f;
        }
    }

    private void ResetOutletMaterials()
    {
        foreach (Renderer r in outletRenderers)
            if (r != null) r.material = outletNormalMaterial;
    }

    public void SpawnSparkEffect(Vector3 position)
    {
        if (electricSparkPrefab != null)
        {
            GameObject spark = Instantiate(electricSparkPrefab, position, Quaternion.identity);
            Destroy(spark, 1f); // Se destruye automáticamente después de 1s
        }
    }

}
