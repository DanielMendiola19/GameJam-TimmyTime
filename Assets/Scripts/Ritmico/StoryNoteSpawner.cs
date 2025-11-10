using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StoryNoteSpawner : MonoBehaviour
{
    [Header("Asignar manual")]
    public BeatMap beatMap;
    public AudioSource song;
    public AudioSource tap;
    public LaneReferenceSpawner laneRef;
    public GameObject notePrefab;

    [Header("Count In")]
    public float tapInterval = 0.5f;
    public TMP_Text countText;

    [Header("Line Renderer Settings")]
    public Color connectionColor = Color.yellow;
    public float lineWidth = 0.1f;
    public float timeTolerance = 0.01f; // Tolerancia para agrupar notas

    [HideInInspector]
    public bool canSpawn = false;

    private int nextNoteIndex = 0;
    private HashSet<string> spawnedNotes = new HashSet<string>();
    private Dictionary<float, List<GameObject>> notesByTime = new Dictionary<float, List<GameObject>>();
    private Dictionary<float, LineRenderer> activeConnections = new Dictionary<float, LineRenderer>();

    private Coroutine countCoroutine;
    private int currentCountValue = 3;
    private float remainingTapTime = 0f;
    private bool isCounting = false;
    private bool isPaused = false;
    private bool gameOverFlag = false;

    void Start()
    {
        if (beatMap == null || song == null || tap == null || laneRef == null)
        {
            Debug.LogError("StoryNoteSpawner: Falta asignar referencias en inspector.");
            return;
        }

        StartCountIn();
    }

    // ==================== LINE RENDERER PARA NOTAS MÚLTIPLES ====================
    private void CreateConnectionLines(float noteTime, List<GameObject> notes)
    {
        // Solo crear líneas para 2 o más notas
        if (notes.Count < 2) return;

        // Ordenar notas por lane
        notes.Sort((a, b) => a.GetComponent<Note>().lane.CompareTo(b.GetComponent<Note>().lane));

        // Crear GameObject para el LineRenderer
        GameObject lineObject = new GameObject($"ConnectionLine_{noteTime}");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Configurar LineRenderer
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = connectionColor;
        lineRenderer.endColor = connectionColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = notes.Count;

        // Establecer puntos
        for (int i = 0; i < notes.Count; i++)
        {
            lineRenderer.SetPosition(i, notes[i].transform.position);
        }

        // Guardar referencia
        activeConnections[noteTime] = lineRenderer;

        Debug.Log($"📏 Línea creada para {notes.Count} notas en tiempo {noteTime}");
    }

    private void UpdateConnectionLines(float noteTime)
    {
        if (!activeConnections.ContainsKey(noteTime)) return;

        LineRenderer lineRenderer = activeConnections[noteTime];
        List<GameObject> notes = notesByTime[noteTime];

        // Remover notas destruidas
        notes.RemoveAll(note => note == null);

        // Actualizar posiciones o eliminar si quedan menos de 2 notas
        if (notes.Count >= 2)
        {
            // Ordenar por lane
            notes.Sort((a, b) => a.GetComponent<Note>().lane.CompareTo(b.GetComponent<Note>().lane));

            lineRenderer.positionCount = notes.Count;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] != null)
                {
                    lineRenderer.SetPosition(i, notes[i].transform.position);
                }
            }
        }
        else
        {
            // Eliminar línea si quedan menos de 2 notas
            Destroy(activeConnections[noteTime].gameObject);
            activeConnections.Remove(noteTime);
            notesByTime.Remove(noteTime);
        }
    }

    private void CleanUpConnectionLines(float noteTime)
    {
        if (activeConnections.ContainsKey(noteTime))
        {
            Destroy(activeConnections[noteTime].gameObject);
            activeConnections.Remove(noteTime);
        }
        notesByTime.Remove(noteTime);
    }

    private float GetGroupedTime(float exactTime)
    {
        // Agrupar tiempos similares (dentro de la tolerancia)
        foreach (float groupedTime in notesByTime.Keys)
        {
            if (Mathf.Abs(exactTime - groupedTime) <= timeTolerance)
            {
                return groupedTime;
            }
        }
        return exactTime;
    }

    // ==================== SPAWN DE NOTAS MODIFICADO ====================
    void SpawnNote(NoteData data)
    {
        int lane = data.lane;
        Vector3 pos = laneRef.GetLanePosition(lane);
        GameObject note = Instantiate(notePrefab, pos, Quaternion.identity);

        Note n = note.GetComponent<Note>();
        n.lane = lane;
        n.targetPoint = laneRef.targetPoints[lane];

        float dist = laneRef.GetLaneDistance(lane);
        float timeToTarget = Mathf.Max(0.001f, data.time - song.time);
        if (timeToTarget > 0.05f)
            n.speed = dist / timeToTarget;

        // Registrar nota para posibles conexiones
        RegisterNoteForConnections(data.time, note);
    }

    private void RegisterNoteForConnections(float noteTime, GameObject note)
    {
        float groupedTime = GetGroupedTime(noteTime);

        if (!notesByTime.ContainsKey(groupedTime))
        {
            notesByTime[groupedTime] = new List<GameObject>();
        }

        notesByTime[groupedTime].Add(note);

        // Crear líneas si hay 2 o más notas en este tiempo
        if (notesByTime[groupedTime].Count >= 2)
        {
            CreateConnectionLines(groupedTime, notesByTime[groupedTime]);
        }
    }

    // ==================== UPDATE MODIFICADO ====================
    void Update()
    {
        // No spawnear si está pausado, en game over, o no puede spawnear
        if (!canSpawn || isPaused || gameOverFlag || beatMap == null || song == null)
            return;

        float songTime = song.time;

        while (nextNoteIndex < beatMap.notes.Count && songTime >= beatMap.notes[nextNoteIndex].time)
        {
            NoteData nd = beatMap.notes[nextNoteIndex];
            string key = $"{nd.time:F6}_{nd.lane}";

            if (!spawnedNotes.Contains(key))
            {
                SpawnNote(nd);
                spawnedNotes.Add(key);
            }
            nextNoteIndex++;
        }

        // Actualizar posiciones de las líneas
        UpdateAllConnectionLines();
    }

    private void UpdateAllConnectionLines()
    {
        // Crear copia de las keys para evitar modificación durante iteración
        List<float> timesToUpdate = new List<float>(activeConnections.Keys);

        foreach (float time in timesToUpdate)
        {
            UpdateConnectionLines(time);
        }
    }

    // ==================== CLEANUP AL DESTRUIR ====================
    void OnDestroy()
    {
        // Limpiar todas las líneas
        foreach (var connection in activeConnections.Values)
        {
            if (connection != null)
                Destroy(connection.gameObject);
        }
        activeConnections.Clear();
        notesByTime.Clear();
    }

    // ==================== MÉTODOS EXISTENTES (sin cambios) ====================
    public void PauseEverything()
    {
        isPaused = true;
        canSpawn = false;

        if (song != null && song.isPlaying)
            song.Pause();
        if (tap != null && tap.isPlaying)
            tap.Pause();

        if (countText != null)
            countText.gameObject.SetActive(false);

        Debug.Log("StoryNoteSpawner: TODO PAUSADO");
    }

    public void ResumeEverything()
    {
        isPaused = false;

        if (song != null && !song.isPlaying)
            song.UnPause();

        if (isCounting && countText != null)
            countText.gameObject.SetActive(true);

        // 🔑 Restaurar spawneo si el juego ya empezó
        if (!isCounting && !gameOverFlag)
            canSpawn = true;

        Debug.Log("▶️ StoryNoteSpawner: TODO REANUDADO");
    }

    public void StopEverything()
    {
        isPaused = true;
        gameOverFlag = true;
        canSpawn = false;

        if (song != null)
            song.Stop();
        if (tap != null)
            tap.Stop();

        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }

        if (countText != null)
            countText.gameObject.SetActive(false);

        // Limpiar líneas
        foreach (var connection in activeConnections.Values)
        {
            if (connection != null)
                Destroy(connection.gameObject);
        }
        activeConnections.Clear();
        notesByTime.Clear();

        Debug.Log("🛑 StoryNoteSpawner: TODO DETENIDO");
    }

    public void StartCountIn()
    {
        if (countCoroutine != null)
            StopCoroutine(countCoroutine);

        countCoroutine = StartCoroutine(CountCoroutine());
    }

    private IEnumerator CountCoroutine()
    {
        isCounting = true;
        currentCountValue = 3;
        remainingTapTime = tapInterval;
        gameOverFlag = false;
        isPaused = false;

        Debug.Log("🔊 Iniciando countdown...");

        while (currentCountValue > 0)
        {
            if (gameOverFlag || isPaused)
            {
                if (gameOverFlag)
                {
                    Debug.Log("Countdown cancelado por Game Over");
                    yield break;
                }

                while (isPaused && !gameOverFlag)
                {
                    yield return null;
                }

                if (gameOverFlag) yield break;
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(true);
                countText.text = currentCountValue.ToString();
            }

            if (tap != null)
                tap.Play();

            Debug.Log($"🔊 Count: {currentCountValue}");

            float timer = remainingTapTime;
            remainingTapTime = tapInterval;

            while (timer > 0f)
            {
                if (gameOverFlag) yield break;

                if (!isPaused)
                    timer -= Time.deltaTime;

                yield return null;
            }

            currentCountValue--;
        }

        if (!gameOverFlag && !isPaused)
        {
            if (countText != null)
                countText.gameObject.SetActive(false);

            if (song != null)
            {
                song.time = 0f;
                song.Play();
            }

            canSpawn = true;
            Debug.Log("🎵 Countdown completado - Iniciando gameplay");
        }

        isCounting = false;
    }

    // ==================== MÉTODOS DE LEGACY ====================
    public void PauseCount()
    {
        PauseEverything();
    }

    public void ResumeCount()
    {
        ResumeEverything();
    }

    public void GameOverStopCount()
    {
        StopEverything();
    }
}