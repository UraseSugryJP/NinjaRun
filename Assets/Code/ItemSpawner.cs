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
    [SerializeField] private float laneWidth = 1.0f; // ObstacleSpawnerと一致させる

    private int lastSafeLane = 1; // 0:左, 1:中, 2:右

    public void SpawnPathmakingItems(int[,] pattern, int currentRow, float currentDist, float nextDist, float splineLength)
    {
        if (splineContainer == null || itemPrefab == null) return;

        // 1. 安全なレーン（0, 1, 2）を特定
        List<int> safeLanes = new List<int>();
        for (int l = 0; l < 3; l++)
        {
            if (pattern[currentRow, l] == 0) safeLanes.Add(l);
        }

        // 2. 次のターゲットレーンを決定
        int targetLane = safeLanes.Count > 0 ? safeLanes[Random.Range(0, safeLanes.Count)] : 1;

        // 3. アイテムを「行の間」に配置
        // i = 1 から始めることで、障害物の真上にアイテムが重なるのを防ぐ
        for (int i = 1; i <= itemsPerSegment; i++)
        {
            // 0.0〜1.0 の割合を計算
            float t_lerp = (float)i / (float)(itemsPerSegment + 1);
            
            float lerpedDist = Mathf.Lerp(currentDist, nextDist, t_lerp);
            
            // --- ここがロジックの肝 ---
            // レーン番号を 0.0(左) 〜 2.0(右) の間で補間し、絶対に 0〜2 の間に収める
            float lerpedLaneValue = Mathf.Lerp((float)lastSafeLane, (float)targetLane, t_lerp);
            lerpedLaneValue = Mathf.Clamp(lerpedLaneValue, 0f, 2f); 
            
            PlaceItemAt(lerpedDist, lerpedLaneValue, splineLength);
        }

        lastSafeLane = targetLane;
    }

    private void PlaceItemAt(float distance, float laneValue, float splineLength)
    {
        float t = Mathf.Clamp01(distance / splineLength);
        
        // ObstacleSpawner.cs と同一の座標計算
        Vector3 basePos = (Vector3)splineContainer.EvaluatePosition(t); 
        Vector3 tangent = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t)); 
        Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;

        // laneValue 1.0(中央) の時に、(1.0 - 1.0) * laneWidth = 0 となりスプライン直下になる
        // laneValue 0.0(左端) -> -1.0 * laneWidth
        // laneValue 2.0(右端) ->  1.0 * laneWidth
        float laneOffset = laneValue - 1.0f; 
        Vector3 spawnPos = basePos + (right * (laneOffset * laneWidth));

        // 接地判定
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
        {
            spawnPos.y = hit.point.y + itemYOffset;
        }

        Instantiate(itemPrefab, spawnPos, Quaternion.LookRotation(tangent, Vector3.up));
    }
}