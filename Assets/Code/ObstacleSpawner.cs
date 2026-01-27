using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct ObstacleCategory {
        public string name; public int id; public List<GameObject> prefabs; public bool isRollable;
    }

    [Header("Settings")]
    [SerializeField] private List<ObstacleCategory> obstacleCatalog = new List<ObstacleCategory>();
    [SerializeField] private float laneWidth = 1.0f;
    [SerializeField] private float minSpacing = 4.0f;
    [SerializeField] private float maxSpacing = 8.0f;
    [SerializeField] private float spawnAheadRange = 50.0f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private LayerMask groundLayer = ~0;

    private List<GameObject> activeObstacles = new List<GameObject>();
    private float nextSpawnDist = 5f; // 0から開始して即座に湧かせる
    private float totalLen = 0f;
    private int curPattern = 0;
    private int curRow = 0;

    // 中難易度パターン
    private static readonly int[][,] patterns = new int[][,] {
        new int[,] { {0,1,0}, {1,0,1}, {0,1,0} },
        new int[,] { {2,2,2}, {0,0,0}, {3,0,3} }, // ID 3 は端のみ
        new int[,] { {4,0,0}, {0,0,4}, {2,2,2} }, // ID 4 巨大壁
        new int[,] { {1,2,1}, {3,0,3}, {1,0,1} },
        new int[,] { {0,1,1}, {1,1,0}, {0,1,1} },
        new int[,] { {3,1,3}, {2,0,2}, {3,1,3} },
        new int[,] { {4,0,0}, {0,0,0}, {0,0,4} } 
    };

    void Start() {
        if (splineContainer == null) splineContainer = FindObjectOfType<SplineContainer>();
        if (playerTransform == null) playerTransform = GameObject.FindWithTag("Player")?.transform;
        
        totalLen = CalculateSplineLength();
        curPattern = UnityEngine.Random.Range(0, patterns.Length);
        
        Debug.Log($"Spawner Started. Length: {totalLen}");
    }

    void Update() {
        if (playerTransform == null || splineContainer == null) return;
        
        activeObstacles.RemoveAll(o => o == null);

        // プレイヤーの現在位置（T値）を取得して距離に換算
        float playerT = GetClosestT(playerTransform.position);
        float playerDist = playerT * totalLen;

        // プレイヤーの先50mまで生成し続ける
        while (nextSpawnDist < playerDist + spawnAheadRange && activeObstacles.Count < 50) {
            SpawnNext();
        }
    }

    void SpawnNext() {
        int[,] p = patterns[curPattern];
        
        if (curRow >= p.GetLength(0)) {
            curRow = 0;
            curPattern = UnityEngine.Random.Range(0, patterns.Length);
            nextSpawnDist += 5f; 
            return;
        }

        float gap = UnityEngine.Random.Range(minSpacing, maxSpacing);
        if (itemSpawner) itemSpawner.SpawnPathmakingItems(p, curRow, nextSpawnDist, nextSpawnDist + gap, totalLen);
        
        SpawnRow(p, curRow, nextSpawnDist);

        nextSpawnDist += gap;
        curRow++;
    }

    void SpawnRow(int[,] pattern, int row, float dist) {
    // 【修正】走行距離をスプラインの全長で割り、0.0～1.0の範囲でループさせる
    float loopDist = dist % totalLen;
    float t = loopDist / totalLen;

    Vector3 pos = (Vector3)splineContainer.EvaluatePosition(t);
    Vector3 tan = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t));
    Vector3 right = Vector3.Cross(tan, Vector3.up).normalized;
    Quaternion splineRot = Quaternion.LookRotation(tan, Vector3.up);

    for (int i = 0; i < 3; i++) {
        int id = pattern[row, i];
        if (id == 0) continue;
        if (id == 3 && i == 1) continue; 

        var cat = obstacleCatalog.Find(c => c.id == id);
        if (cat.prefabs == null || cat.prefabs.Count == 0) continue;

        GameObject prefab = cat.prefabs[UnityEngine.Random.Range(0, cat.prefabs.Count)];
        Quaternion sRot = splineRot * prefab.transform.rotation;

        Vector3 sPos;
        if (id == 4) { 
            float offset = (i == 2) ? 0.5f * laneWidth : -0.5f * laneWidth;
            sPos = pos + right * offset;
        } else {
            sPos = pos + right * ((i - 1) * laneWidth);
            if (id == 3 && i == 2) {
                sRot = sRot * Quaternion.Euler(0, 180f, 0);
            }
        }

        if (Physics.Raycast(sPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer)) {
            sPos.y = hit.point.y;
        }

        GameObject obj = Instantiate(prefab, sPos, sRot);
        if (cat.isRollable) obj.AddComponent<RollableObstacle>();
        
        // 【重要】ObstacleBehavior が「プレイヤーの後ろに回ったら自動で消える」機能を持っていれば
        // activeObstacles.RemoveAll(o => o == null) と相まって、リストも肥大化せずループできます
        obj.AddComponent<ObstacleBehavior>().Initialize(playerTransform, this, 0.85f, 5f, groundLayer);
        activeObstacles.Add(obj);
    }
}

    float CalculateSplineLength() {
        float l = 0; Vector3 p = (Vector3)splineContainer.EvaluatePosition(0);
        for(int i=1; i<=100; i++) {
            Vector3 n = (Vector3)splineContainer.EvaluatePosition(i/100f);
            l += Vector3.Distance(p, n); p = n;
        }
        return l > 0 ? l : 1000f;
    }

    float GetClosestT(Vector3 wp) {
        float bt = 0; float bd = float.MaxValue;
        for(int i=0; i<=50; i++) {
            float t = i/50f;
            float d = Vector3.SqrMagnitude((Vector3)splineContainer.EvaluatePosition(t) - wp);
            if (d < bd) { bd = d; bt = t; }
        }
        return bt;
    }

    public void NotifyObstacleDestroyed(GameObject obj) {
        if (activeObstacles.Contains(obj)) activeObstacles.Remove(obj);
    }
}