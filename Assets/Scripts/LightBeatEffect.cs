using UnityEngine;

public class LightBeatEffect : MonoBehaviour
{
    public BeatManager beatManager;
    public Light targetLight;
    public float normalIntensity = 2f;
    public float beatIntensity = 6f;
    public float fadeSpeed = 8f;

    // "Explosión" de luz en ciertos momentos
    public float[] explosionBeats; // tiempos (en segundos) donde explota
    private bool lastBeatState;

    // Efecto rayo opcional
    public GameObject lightningPrefab;

    void Update()
    {
        bool onBeat = beatManager.IsOnBeat();

        if (onBeat && !lastBeatState)
        {
            // Efecto normal al ritmo
            targetLight.intensity = beatIntensity;
            targetLight.color = Random.ColorHSV(0f, 1f, 0.8f, 1f, 1f, 1f);

            // Ver si estamos en un momento de explosión
            foreach (float t in explosionBeats)
            {
                if (Mathf.Abs(beatManager.GetSongTime() - t) < 0.1f)
                {
                    StartCoroutine(LightExplosion());
                    break;
                }
            }
        }
        else
        {
            // Suavizar la luz al valor base
            targetLight.intensity = Mathf.Lerp(targetLight.intensity, normalIntensity, Time.deltaTime * fadeSpeed);
        }

        lastBeatState = onBeat;
    }

    private System.Collections.IEnumerator LightExplosion()
    {
        // Explosión: aumento fuerte del brillo + rayo visual
        float peak = 10f;
        targetLight.intensity = peak;

        if (lightningPrefab != null)
        {
            GameObject ray = Instantiate(lightningPrefab, transform.position, Quaternion.identity);
            Destroy(ray, 0.3f);
        }

        yield return new WaitForSeconds(0.2f);
        targetLight.intensity = beatIntensity;
    }
}
