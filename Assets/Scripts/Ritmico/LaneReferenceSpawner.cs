using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneReferenceSpawner : MonoBehaviour
{
    [Header("Referencias de lanes")]
    public Transform[] lanePoints;
    public Transform[] targetPoints;

    /// <summary>
    /// Retorna el número de lanes disponibles
    /// </summary>
    public int LaneCount
    {
        get { return lanePoints != null ? lanePoints.Length : 0; }
    }

    /// <summary>
    /// Retorna un índice de lane válido aleatorio
    /// </summary>
    public int GetRandomLane(System.Random prng = null)
    {
        if (lanePoints == null || lanePoints.Length == 0)
        {
            Debug.LogWarning("No hay lanes asignadas en LaneReferenceSpawner.");
            return 0;
        }

        if (prng == null)
            return Random.Range(0, lanePoints.Length);
        else
            return prng.Next(0, lanePoints.Length);
    }

    /// <summary>
    /// Obtiene la posición de la lane por índice
    /// </summary>
    public Vector3 GetLanePosition(int laneIndex)
    {
        if (lanePoints == null || lanePoints.Length == 0) return Vector3.zero;
        if (laneIndex < 0 || laneIndex >= lanePoints.Length) return Vector3.zero;
        return lanePoints[laneIndex].position;
    }

    /// <summary>
    /// Calcula la distancia de una lane (para fallTime)
    /// </summary>
    public float GetLaneDistance(int laneIndex)
    {
        if (lanePoints == null || targetPoints == null) return 0f;
        if (laneIndex < 0 || laneIndex >= lanePoints.Length || laneIndex >= targetPoints.Length) return 0f;
        return Vector3.Distance(lanePoints[laneIndex].position, targetPoints[laneIndex].position);
    }
}