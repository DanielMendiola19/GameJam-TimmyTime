using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // Tu jugador
    private Vector3 offset;        // Distancia inicial entre cámara y jugador
    private Quaternion initialRotation; // Rotación fija que quieres mantener

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        initialRotation = transform.rotation; // Guarda la rotación inicial
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Solo seguir la posición, sin cambiar rotación
            transform.position = target.position + offset;
            transform.rotation = initialRotation;
        }
    }
}
