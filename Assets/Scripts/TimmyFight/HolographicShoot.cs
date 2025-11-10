using UnityEngine;

public class HolographicShoot : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 500f;
    public LineRenderer laserLine;
    public float laserDuration = 0.05f;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (laserLine)
            laserLine.enabled = false;
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            Shoot();
    }

    void Shoot()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Vector3 endPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, range))
            endPoint = hit.point;
        else
            endPoint = ray.GetPoint(range);

        // Dibujar láser
        if (laserLine)
        {
            laserLine.SetPosition(0, playerCamera.transform.position);
            laserLine.SetPosition(1, endPoint);
            laserLine.enabled = true;
            Invoke(nameof(DisableLaser), laserDuration);
        }
    }

    void DisableLaser()
    {
        if (laserLine)
            laserLine.enabled = false;
    }
}
