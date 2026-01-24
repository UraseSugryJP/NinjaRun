using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private GameObject itemPrefab;

    [Header("動線設定")]
    [SerializeField] private int itemsPerSegment = 3; 
    [SerializeField] private float itemYOffset = 0.5f;
    [SerializeField] private float laneWidth = 1.0f; 

    private int lastSafeLane = 1; 

    public void SpawnPathmakingItems(int[,] pattern, int currentRow, float currentDist, float nextDist, float splineLength)
    {
        if (splineContainer == null || itemPrefab == null) return;

        List<int> safeLanes = new List<int>();
        for (int l = 0; l < 3; l++)
        {
            if (pattern[currentRow, l] == 0) safeLanes.Add(l);
        }

        int targetLane = safeLanes.Count > 0 ? safeLanes[UnityEngine.Random.Range(0, safeLanes.Count)] : 1;

        for (int i = 0; i < itemsPerSegment; i++)
        {
            float t_lerp = (float)i / itemsPerSegment;
            float lerpedDist = Mathf.Lerp(currentDist, nextDist, t_lerp);
            float lerpedLane = Mathf.Lerp(lastSafeLane, targetLane, t_lerp);
            
            PlaceItemAt(lerpedDist, lerpedLane, splineLength);
        }

        lastSafeLane = targetLane;
    }

    private void PlaceItemAt(float distance, float laneValue, float splineLength)
    {
        float t = Mathf.Clamp01(distance / splineLength);
        Vector3 basePos = (Vector3)splineContainer.EvaluatePosition(t);
        Vector3 tangent = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t));
        Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;

        float laneOffset = laneValue - 1f; 
        Vector3 spawnPos = basePos + (right * (laneOffset * laneWidth));

        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos.y = hit.point.y + itemYOffset;
        }

        Instantiate(itemPrefab, spawnPos, Quaternion.LookRotation(tangent));
    }
}