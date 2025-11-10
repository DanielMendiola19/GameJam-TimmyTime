// CuttableObject.cs

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CuttableObject : MonoBehaviour, IPointerDownHandler
{
    public bool isGood; // true = good, false = bad
    private Image img;
    private GameManagerFight gm;

    private bool wasClicked = false; // solo relevante para los BadObjects

    private void Awake()
    {
        img = GetComponent<Image>();
        gm = FindObjectOfType<GameManagerFight>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasClicked = true;

        if (isGood)
        {
            // tocaste un GoodObject → quita vida
            img.color = Color.blue;
            gm.HitGoodObject(gameObject);
        }
        else
        {
            // tocaste un BadObject → suma score
            img.color = Color.red;
            gm.HitBadObject(gameObject);
        }
    }

    private void OnDestroy()
    {
        // solo revisar BadObjects que desaparecen sin tocar
        if (!wasClicked && !isGood)
        {
            // fallo → quita vida
            gm.HitGoodObject(gameObject);
        }
    }
}
