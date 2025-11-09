using UnityEngine;

public class ConstantFlicker : MonoBehaviour
{
    public Light targetLight;

    [Header("Intensidad y ritmo")]
    public float minIntensity = 0f;     // oscuridad total
    public float maxIntensity = 10f;    // brillo máximo
    public float minInterval = 0.05f;   // velocidad mínima entre cambios
    public float maxInterval = 0.25f;   // velocidad máxima entre cambios

    [Header("Colores")]
    public bool useVividColors = true;  // colores intensos
    public bool allowDarkFlashes = true; // permite que a veces se apague

    private float timer = 0f;
    private float nextChange = 0.1f;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        ScheduleNextChange();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextChange)
        {
            Flash();
            ScheduleNextChange();
        }
    }

    void Flash()
    {
        // Decide si entra un “apagón” rápido
        bool blackout = allowDarkFlashes && Random.value < 0.15f;

        if (blackout)
        {
            targetLight.intensity = 0f;
            return;
        }

        // Cambiar color y brillo de forma frenética
        targetLight.color = useVividColors
            ? Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f) // colores vivos
            : Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f); // colores suaves

        targetLight.intensity = Random.Range(minIntensity, maxIntensity);
    }

    void ScheduleNextChange()
    {
        timer = 0f;
        nextChange = Random.Range(minInterval, maxInterval);
    }
    }
