using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RealtimeBeatDetector : MonoBehaviour
{
    public Spawner spawner;
    public float sensitivity = 1.4f;
    public int sampleSize = 1024;
    public float cooldown = 0.18f;

    private float[] spectrum;
    private float lastBeatTime = 0f;

    void Start()
    {
        spectrum = new float[sampleSize];
    }

    void Update()
    {
        GetComponent<AudioSource>().GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        float sum = 0f;

        for (int i = 0; i < spectrum.Length; i++)
            sum += spectrum[i];

        float rms = Mathf.Sqrt(sum / spectrum.Length);
        float now = Time.time;

        if (rms * sensitivity > 0.01f && now - lastBeatTime > cooldown)
        {
            lastBeatTime = now;
            spawner.SpawnOnBeat();
        }
    }
}
