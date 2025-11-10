using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInactivity : MonoBehaviour
{
    [Header("Tiempo hasta escena por inactividad (segundos)")]
    public float inactivityTime = 60f; // 1 minuto por ejemplo

    private float timer = 0f;

    void Update()
    {
        // Si hay input, reiniciamos timer
        if (Input.anyKey || Input.mousePresent && (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0))
        {
            timer = 0f;
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= inactivityTime)
            {
                // Redirigir a escena
                SceneManager.LoadScene("InicioCinematica");
            }
        }
    }
}
