using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomNoteSpawner : MonoBehaviour
{
    [Header("Asignar manual")]
    public CustomLevelLoader loader; // reference al CustomLevelLoader
    public AudioSource tap;
    public LaneReferenceSpawner laneRef;
    public GameObject notePrefab;

    [Header("Count In")]
    public float tapInterval = 0.5f;
    public TMP_Text countText;

    [Header("Line Renderer Settings")]
    public Color connectionColor = Color.yellow;
    public float lineWidth = 0.1f;
    public float timeTolerance = 0.01f;

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
        if (loader == null || loader.runtimeBeatMap == null || loader.songSource == null || tap == null || laneRef == null)
        {
            Debug.LogError("CustomNoteSpawner: Falta asignar referencias en inspector o nivel no cargado!");
            return;
        }

        StartCountIn();
    }

    // ==================== LINE RENDERER PARA NOTAS MÚLTIPLES ====================
    private void CreateConnectionLines(float noteTime, List<GameObject> notes)
    {
        if (notes.Count < 2) return;

        notes.Sort((a, b) => a.GetComponent<Note>().lane.CompareTo(b.GetComponent<Note>().lane));

        GameObject lineObject = new GameObject($"ConnectionLine_{noteTime}");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = connectionColor;
        lineRenderer.endColor = connectionColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = notes.Count;

        for (int i = 0; i < notes.Count; i++)
            lineRenderer.SetPosition(i, notes[i].transform.position);

        activeConnections[noteTime] = lineRenderer;
    }

    private void UpdateConnectionLines(float noteTime)
    {
        if (!activeConnections.ContainsKey(noteTime)) return;

        LineRenderer lineRenderer = activeConnections[noteTime];
        List<GameObject> notes = notesByTime[noteTime];

        notes.RemoveAll(note => note == null);

        if (notes.Count >= 2)
        {
            notes.Sort((a, b) => a.GetComponent<Note>().lane.CompareTo(b.GetComponent<Note>().lane));
            lineRenderer.positionCount = notes.Count;
            for (int i = 0; i < notes.Count; i++)
                if (notes[i] != null) lineRenderer.SetPosition(i, notes[i].transform.position);
        }
        else
        {
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
        foreach (float groupedTime in notesByTime.Keys)
            if (Mathf.Abs(exactTime - groupedTime) <= timeTolerance)
                return groupedTime;

        return exactTime;
    }

    // ==================== SPAWN DE NOTAS ====================
    void SpawnNote(NoteData data)
    {
        int lane = data.lane;
        Vector3 pos = laneRef.GetLanePosition(lane);
        GameObject note = Instantiate(notePrefab, pos, Quaternion.identity);

        Note n = note.GetComponent<Note>();
        n.lane = lane;
        n.targetPoint = laneRef.targetPoints[lane];

        float dist = laneRef.GetLaneDistance(lane);
        float timeToTarget = Mathf.Max(0.001f, data.time - loader.songSource.time);
        if (timeToTarget > 0.05f)
            n.speed = dist / timeToTarget;

        RegisterNoteForConnections(data.time, note);
    }

    private void RegisterNoteForConnections(float noteTime, GameObject note)
    {
        float groupedTime = GetGroupedTime(noteTime);

        if (!notesByTime.ContainsKey(groupedTime))
            notesByTime[groupedTime] = new List<GameObject>();

        notesByTime[groupedTime].Add(note);

        if (notesByTime[groupedTime].Count >= 2)
            CreateConnectionLines(groupedTime, notesByTime[groupedTime]);
    }

    // ==================== UPDATE ====================
    void Update()
    {
        if (!canSpawn || isPaused || gameOverFlag || loader.runtimeBeatMap == null || loader.songSource == null)
            return;

        float songTime = loader.songSource.time;

        while (nextNoteIndex < loader.runtimeBeatMap.notes.Count && songTime >= loader.runtimeBeatMap.notes[nextNoteIndex].time)
        {
            NoteData nd = loader.runtimeBeatMap.notes[nextNoteIndex];
            string key = $"{nd.time:F6}_{nd.lane}";

            if (!spawnedNotes.Contains(key))
            {
                SpawnNote(nd);
                spawnedNotes.Add(key);
            }
            nextNoteIndex++;
        }

        UpdateAllConnectionLines();
    }

    private void UpdateAllConnectionLines()
    {
        List<float> timesToUpdate = new List<float>(activeConnections.Keys);

        foreach (float time in timesToUpdate)
            UpdateConnectionLines(time);
    }

    void OnDestroy()
    {
        foreach (var connection in activeConnections.Values)
            if (connection != null) Destroy(connection.gameObject);

        activeConnections.Clear();
        notesByTime.Clear();
    }

    // ==================== MÉTODOS ====================
    public void PauseEverything()
    {
        isPaused = true;
        canSpawn = false;

        if (loader.songSource != null && loader.songSource.isPlaying) loader.songSource.Pause();
        if (tap != null && tap.isPlaying) tap.Pause();

        if (countText != null) countText.gameObject.SetActive(false);

        Debug.Log("CustomNoteSpawner: TODO PAUSADO");
    }

    public void ResumeEverything()
    {
        isPaused = false;

        if (loader.songSource != null && !loader.songSource.isPlaying)
            loader.songSource.UnPause();

        if (isCounting && countText != null)
            countText.gameObject.SetActive(true);

        if (!isCounting && !gameOverFlag)
            canSpawn = true;

        Debug.Log("▶️ CustomNoteSpawner: TODO REANUDADO");
    }

    public void StopEverything()
    {
        isPaused = true;
        gameOverFlag = true;
        canSpawn = false;

        if (loader.songSource != null) loader.songSource.Stop();
        if (tap != null) tap.Stop();

        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }

        if (countText != null) countText.gameObject.SetActive(false);

        foreach (var connection in activeConnections.Values)
            if (connection != null) Destroy(connection.gameObject);

        activeConnections.Clear();
        notesByTime.Clear();

        Debug.Log("CustomNoteSpawner: TODO DETENIDO");
    }

    public void StartCountIn()
    {
        if (countCoroutine != null) StopCoroutine(countCoroutine);

        countCoroutine = StartCoroutine(CountCoroutine());
    }

    private IEnumerator CountCoroutine()
    {
        isCounting = true;
        currentCountValue = 3;
        remainingTapTime = tapInterval;
        gameOverFlag = false;
        isPaused = false;

        Debug.Log("🔊 Iniciando countdown CustomLevel...");

        while (currentCountValue > 0)
        {
            if (gameOverFlag || isPaused)
            {
                if (gameOverFlag) yield break;

                while (isPaused && !gameOverFlag) yield return null;

                if (gameOverFlag) yield break;
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(true);
                countText.text = currentCountValue.ToString();
            }

            if (tap != null) tap.Play();

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
            if (countText != null) countText.gameObject.SetActive(false);

            if (loader.songSource != null)
            {
                loader.songSource.time = 0f;
                loader.songSource.Play();
            }

            canSpawn = true;
            Debug.Log("Countdown completado - Iniciando Custom Gameplay");
        }

        isCounting = false;
    }
}
