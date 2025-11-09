using UnityEngine;

public class RotateSelf : MonoBehaviour
{
    private Vector3 randomAxis;
    private float randomSpeed;

    [Header("Rango de velocidad de rotación")]
    public float minSpeed = 30f;
    public float maxSpeed = 150f;

    void Start()
    {
        // Eje aleatorio (dirección del giro)
        randomAxis = Random.onUnitSphere; // vector aleatorio (X,Y,Z normalizado)

        // Velocidad aleatoria dentro del rango
        randomSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        // Rotar constantemente en el eje y velocidad generados
        transform.Rotate(randomAxis * randomSpeed * Time.deltaTime, Space.Self);
    }
}
