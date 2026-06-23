using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Настройки генерации столбов")]
    public float distanceBetweenPillars = 15f;
    public List<Vector3> CalculateSpawnPositions(int playerCount)
    {
        List<Vector3> positions = new List<Vector3>();
        float startX = -((playerCount - 1) * distanceBetweenPillars) / 2f;
        for (int i = 0; i < playerCount; i++)
        {
            float posX = startX + (i * distanceBetweenPillars);
            positions.Add(new Vector3(posX, 0f, 0f));
        }
        return positions;
    }
}