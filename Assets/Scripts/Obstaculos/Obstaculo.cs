using UnityEngine.SceneManagement;
using UnityEngine;

public class Obstaculo : MonoBehaviour
{
    public enum TipoObstaculo
    {
        Parry, Esquivar, Quieto, Dash, DashAbajo, Sprint, MovimientoNormal
    }

    [Header("Configuración")]
    public TipoObstaculo tipo;
    public Material materialObstaculo;

    [Header("UI")]
    public GameObject muerteUI; // Asignar el GameObject de la UI de muerte en el Inspector

    private float reaccionTiempo = 0.2f;
    private float reaccionCounter = 0f;

    private Collider triggerCollider;
    private Collider pisoCollider;

    private void Awake()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            if (col.isTrigger) triggerCollider = col;
            else pisoCollider = col;
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        if (GetComponent<Renderer>() != null && materialObstaculo != null)
            GetComponent<Renderer>().material = materialObstaculo;

        if (muerteUI != null)
            muerteUI.SetActive(false); // asegurarse que esté desactivada al inicio
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerController_2 jugador = other.GetComponent<PlayerController_2>();
        if (jugador == null) return;

        bool fallo = false;

        if (tipo == TipoObstaculo.DashAbajo)
        {
            pisoCollider.enabled = !jugador.isDownDashing;
            if (jugador.isDashing || jugador.isParrying)
                fallo = true;
        }
        else
        {
            switch (tipo)
            {
                case TipoObstaculo.Parry:
                    if (!jugador.isParrying) fallo = true;
                    break;
                case TipoObstaculo.Esquivar:
                    fallo = true;
                    break;
                case TipoObstaculo.Quieto:
                    if (jugador.inputMove.x != 0) fallo = true;
                    break;
                case TipoObstaculo.Dash:
                    if (!jugador.isDashing) fallo = true;
                    break;
                case TipoObstaculo.Sprint:
                    if (!(Input.GetKey(jugador.sprintKey) && jugador.inputMove.x != 0)) fallo = true;
                    break;
                case TipoObstaculo.MovimientoNormal:
                    if (jugador.inputMove.x == 0) reaccionCounter += Time.deltaTime;
                    else reaccionCounter = 0f;
                    if (reaccionCounter >= reaccionTiempo) fallo = true;
                    break;
            }
        }

        if (fallo) Muerte(jugador);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController_2 jugador = other.GetComponent<PlayerController_2>();
        if (jugador == null) return;

        if (tipo == TipoObstaculo.DashAbajo)
        {
            pisoCollider.enabled = true;
        }
    }

    private void Muerte(PlayerController_2 jugador)
    {
        // Desactivar jugador
        jugador.gameObject.SetActive(false);

        // Activar UI de muerte antes de pausar el tiempo
        if (muerteUI != null)
            muerteUI.SetActive(true);

        // Pausar el juego
        Time.timeScale = 0f;

        // Iniciar la espera para reinicio
        StartCoroutine(EsperarReinicio());
    }


    private System.Collections.IEnumerator EsperarReinicio()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (muerteUI != null)
                    muerteUI.SetActive(false); // desactivar UI al reiniciar

                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield break;
            }
            yield return null;
        }
    }
}
