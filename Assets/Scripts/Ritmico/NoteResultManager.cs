using UnityEngine;
using TMPro;
using System.Collections;
//using UnityEditor.Build.Content;

public class NoteResultManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text hitText;
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text accuracyText;

    [Header("Audio")]
    public AudioSource song;
    [Range(0.1f, 1f)] public float failVolumeMultiplier = 0.3f;
    public float failEffectDuration = 0.4f;
    public float failCooldown = 0.5f;

    private float lastFailTime = -10f;
    private bool isRestoringVolume = false;
    private float originalVolume;

    void Start()
    {
        if (song != null)
            originalVolume = song.volume;
    }

    public int PerfectHits { get; private set; }
    public int GreatHits { get; private set; }
    public int MissHits { get; private set; }
    public int TotalScore { get; private set; }
    public int MaxCombo { get; private set; }

    public void OnNoteHit(string hitType, int score, int combo)
    {
        if (hitText != null) hitText.text = hitType;
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (comboText != null) comboText.text = $"Combo: {combo}x";

        // Actualizar estadísticas
        TotalScore = score;
        MaxCombo = Mathf.Max(MaxCombo, combo);

        if (hitType == "PERFECT")
            PerfectHits++;
        else if (hitType == "GREAT")
            GreatHits++;
    }

    public void OnNoteFail(string hitType, int score, int combo)
    {
        if (hitText != null) hitText.text = hitType;
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (comboText != null) comboText.text = $"Combo: {combo}x";

        if (song != null && !isRestoringVolume && Time.time - lastFailTime > failCooldown)
        {
            StartCoroutine(FailVolumeDip());
            lastFailTime = Time.time;
        }

        // Actualizar estadísticas
        TotalScore = score;
        if (hitType == "MISS")
            MissHits++;
    }

    public void UpdateAccuracyText(float accuracy)
    {
        if (accuracyText != null)
            accuracyText.text = $"{accuracy:F2}%";
    }

    private IEnumerator FailVolumeDip()
    {
        isRestoringVolume = true;
        float targetVolume = Mathf.Max(originalVolume * failVolumeMultiplier, 0.05f);
        float elapsed = 0f;

        while (elapsed < 0.1f)
        {
            song.volume = Mathf.Lerp(originalVolume, targetVolume, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        song.volume = targetVolume;
        yield return new WaitForSeconds(failEffectDuration);

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            song.volume = Mathf.Lerp(targetVolume, originalVolume, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        song.volume = originalVolume;
        isRestoringVolume = false;
    }
}
