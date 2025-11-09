using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public RectTransform spawnArea; // panel donde aparecerán
    public GameObject[] goodObjects;
    public GameObject[] badObjects;

    public float spawnInterval = 1f;      
    public float objectLifetime = 1.5f;   
    public int maxSpawnPerBeat = 2;       
    [Range(0f, 1f)]
    public float goodProbability = 0.6f;  
    public float minDistanceBetweenObjects = 100f; // distancia mínima entre objetos

    private bool canSpawn = true;

    public void SpawnOnBeat()
    {
        if (!canSpawn) return;

        canSpawn = false;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        int spawnCount = Random.Range(1, maxSpawnPerBeat + 1);
        List<Vector2> positions = new List<Vector2>(); // guarda las posiciones generadas

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab;
            if (Random.value < goodProbability)
                prefab = goodObjects[Random.Range(0, goodObjects.Length)];
            else
                prefab = badObjects[Random.Range(0, badObjects.Length)];

            GameObject obj = Instantiate(prefab, spawnArea);
            RectTransform rt = obj.GetComponent<RectTransform>();

            // Calcula límites considerando tamaño del prefab
            float halfWidth = rt.rect.width / 2;
            float halfHeight = rt.rect.height / 2;

            Vector2 spawnPos;
            int tries = 0;
            do
            {
                float x = Random.Range(-spawnArea.rect.width / 2 + halfWidth, spawnArea.rect.width / 2 - halfWidth);
                float y = Random.Range(-spawnArea.rect.height / 2 + halfHeight, spawnArea.rect.height / 2 - halfHeight);
                spawnPos = new Vector2(x, y);
                tries++;
            }
            while (!IsPositionValid(spawnPos, positions) && tries < 20);

            positions.Add(spawnPos);
            rt.anchoredPosition = spawnPos;

            Destroy(obj, objectLifetime);
        }

        yield return new WaitForSeconds(spawnInterval);
        canSpawn = true;
    }

    private bool IsPositionValid(Vector2 pos, List<Vector2> existingPositions)
    {
        foreach (var p in existingPositions)
        {
            if (Vector2.Distance(pos, p) < minDistanceBetweenObjects)
                return false;
        }
        return true;
    }
}
