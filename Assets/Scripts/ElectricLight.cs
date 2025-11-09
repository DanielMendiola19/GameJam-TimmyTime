using System.Collections;
using UnityEngine;

public class ElectricLight : MonoBehaviour
{
    private Light myLight;
    private float timer = 0f;
    private float nextFlash = 0f;

    // 🔁 Variables para la rotación aleatoria
    private Vector3 rotationAxis;
    private float rotationSpeed;

    void Start()
    {
        myLight = GetComponent<Light>();
        if (myLight == null)
        {
            myLight = gameObject.AddComponent<Light>();
            myLight.type = LightType.Point;
        }

        myLight.intensity = 0;
        myLight.color = new Color(0.6f, 0.9f, 1f); // azul eléctrico

        // Inicializamos rotación aleatoria
        SetRandomRotation();

        ScheduleNextFlash();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // ⚡ Parpadeo eléctrico
        if (timer >= nextFlash)
        {
            StartCoroutine(Flash());
            ScheduleNextFlash();
        }

        // 🔁 Rotación continua
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }

    void ScheduleNextFlash()
    {
        timer = 0f;
        nextFlash = Random.Range(0.2f, 1.5f); // intervalo entre chispas
    }

    private IEnumerator Flash()
    {
        myLight.intensity = Random.Range(2f, 5f);
        yield return new WaitForSeconds(Random.Range(0.03f, 0.1f)); // duración del flash
        myLight.intensity = 0;

        // 🌀 Cada vez que chispea, cambiamos dirección y velocidad
        SetRandomRotation();
    }

    // 🎲 Define una dirección y velocidad aleatoria
    void SetRandomRotation()
    {
        rotationAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        rotationSpeed = Random.Range(30f, 120f); // grados por segundo
    }
}
