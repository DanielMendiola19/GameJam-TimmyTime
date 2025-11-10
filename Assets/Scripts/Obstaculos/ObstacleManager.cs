using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("Materiales por tipo")]
    public Material parryMaterial;
    public Material esquivarMaterial;
    public Material quietoMaterial;
    public Material dashMaterial;
    public Material dashAbajoMaterial;
    public Material sprintMaterial;
    public Material movimientoNormalMaterial;

    [Header("UI de muerte")]
    public GameObject muerteUI; // UI que se mostrará al morir

    private void Start()
    {
        InicializarObstaculos();
    }

    private void InicializarObstaculos()
    {
        string[] tags = { "Parry", "Esquivar", "Quieto", "Dash", "DashAbajo", "Sprint", "MovimientoNormal" };

        foreach (string tag in tags)
        {
            GameObject[] obstaculos = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject obj in obstaculos)
            {
                Obstaculo obsScript = obj.GetComponent<Obstaculo>();
                if (obsScript == null)
                    obsScript = obj.AddComponent<Obstaculo>();

                // Asignar UI de muerte
                obsScript.muerteUI = muerteUI;

                // Asignar tipo y material según tag
                switch (tag)
                {
                    case "Parry":
                        obsScript.tipo = Obstaculo.TipoObstaculo.Parry;
                        obsScript.materialObstaculo = parryMaterial;
                        break;
                    case "Esquivar":
                        obsScript.tipo = Obstaculo.TipoObstaculo.Esquivar;
                        obsScript.materialObstaculo = esquivarMaterial;
                        break;
                    case "Quieto":
                        obsScript.tipo = Obstaculo.TipoObstaculo.Quieto;
                        obsScript.materialObstaculo = quietoMaterial;
                        break;
                    case "Dash":
                        obsScript.tipo = Obstaculo.TipoObstaculo.Dash;
                        obsScript.materialObstaculo = dashMaterial;
                        break;
                    case "DashAbajo":
                        obsScript.tipo = Obstaculo.TipoObstaculo.DashAbajo;
                        obsScript.materialObstaculo = dashAbajoMaterial;
                        break;
                    case "Sprint":
                        obsScript.tipo = Obstaculo.TipoObstaculo.Sprint;
                        obsScript.materialObstaculo = sprintMaterial;
                        break;
                    case "MovimientoNormal":
                        obsScript.tipo = Obstaculo.TipoObstaculo.MovimientoNormal;
                        obsScript.materialObstaculo = movimientoNormalMaterial;
                        break;
                }

                // Aplicar material visual inmediatamente
                Renderer rend = obj.GetComponent<Renderer>();
                if (rend != null && obsScript.materialObstaculo != null)
                    rend.material = obsScript.materialObstaculo;
            }
        }
    }
}
