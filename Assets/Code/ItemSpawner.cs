using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private float laneWidth = 1.0f;
    private int lastLane = 1;

    public void SpawnPathmakingItems(int[,] p, int row, float curD, float nxtD, float totalLen) {
        if (splineContainer == null || itemPrefab == null) return;

        List<int> safe = new List<int>();
        for (int i = 0; i < 3; i++) if (p[row, i] == 0) safe.Add(i);
        int target = safe.Count > 0 ? safe[Random.Range(0, safe.Count)] : 1;

        for (int j = 1; j <= 3; j++) {
            float t_l = j / 4f; // 障害物と重ならないように調整
            float d = Mathf.Lerp(curD, nxtD, t_l);
            float laneValue = Mathf.Clamp(Mathf.Lerp((float)lastLane, (float)target, t_l), 0f, 2f);
            
            float t = Mathf.Clamp01(d / totalLen);
            Vector3 pos = (Vector3)splineContainer.EvaluatePosition(t);
            Vector3 tan = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t));
            Vector3 right = Vector3.Cross(tan, Vector3.up).normalized;

            Vector3 sPos = pos + right * ((laneValue - 1.0f) * laneWidth);
            if (Physics.Raycast(sPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f)) sPos.y = hit.point.y + 0.5f;

            Instantiate(itemPrefab, sPos, Quaternion.LookRotation(tan, Vector3.up));
        }
        lastLane = target;
    }
}