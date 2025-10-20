using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // Tu jugador
    private Vector3 offset;        // Distancia inicial entre c�mara y jugador
    private Quaternion initialRotation; // Rotaci�n fija que quieres mantener

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        initialRotation = transform.rotation; // Guarda la rotaci�n inicial
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Solo seguir la posici�n, sin cambiar rotaci�n
            transform.position = target.position + offset;
            transform.rotation = initialRotation;
        }
    }
}
