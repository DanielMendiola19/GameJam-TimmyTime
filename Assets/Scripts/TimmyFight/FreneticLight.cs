using UnityEngine;

public class FreneticLight : MonoBehaviour
{
    public Light targetLight;

    [Header("Intensidad")]
    public float minIntensity = 1f;
    public float maxIntensity = 8f;

    [Header("Velocidad de cambio")]
    public float minFlashInterval = 0.05f;
    public float maxFlashInterval = 0.25f;

    [Header("Explosiones aleatorias")]
    public float explosionChance = 0.1f; // 10% de probabilidad por flash
    public float explosionMultiplier = 3f; // Aumento de brillo en explosión
    public float explosionDuration = 0.1f;

    private float timer;
    private float nextFlashTime;
    private bool exploding;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        ScheduleNextFlash();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextFlashTime && !exploding)
        {
            Flash();
            ScheduleNextFlash();
        }
    }

    void Flash()
    {
        // Color aleatorio brillante
        targetLight.color = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.9f, 1f);

        // Intensidad aleatoria
        targetLight.intensity = Random.Range(minIntensity, maxIntensity);

        // A veces ocurre una “explosión”
        if (Random.value < explosionChance)
            StartCoroutine(Explosion());
    }

    System.Collections.IEnumerator Explosion()
    {
        exploding = true;
        float originalIntensity = targetLight.intensity;
        targetLight.intensity = originalIntensity * explosionMultiplier;
        yield return new WaitForSeconds(explosionDuration);
        exploding = false;
    }

    void ScheduleNextFlash()
    {
        timer = 0f;
        nextFlashTime = Random.Range(minFlashInterval, maxFlashInterval);
    }
}
