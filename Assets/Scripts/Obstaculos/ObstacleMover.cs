using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    [Header("Velocidad de movimiento")]
    public float velocidad = 8f; // unidades por segundo
    public Vector3 direccion = Vector3.left; // hacia donde se mueve

    private void Update()
    {
        // Mueve todo el padre
        transform.position += direccion.normalized * velocidad * Time.deltaTime;
    }
}
