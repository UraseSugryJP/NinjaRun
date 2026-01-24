using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Mathematics;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct ObstacleCategory
    {
        public string name;
        public int id;           // pattern配列で使用するID
        public List<GameObject> prefabs; // このIDで出現しうるプレハブのリスト
        public bool isRollable;  
    }

    [Header("障害物カタログ")]
    [SerializeField] private List<ObstacleCategory> obstacleCatalog = new List<ObstacleCategory>();

    [Header("参照")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("動的間隔設定")]
    [SerializeField] private float minSpawnSpacing = 2.0f; 
    [SerializeField] private float maxSpawnSpacing = 5.0f; 
    [SerializeField] private float patternGap = 4.0f;      
    [SerializeField] private float initialSpawnOffset = 10.0f;
    [SerializeField] private float spawnAheadRange = 50.0f;

    [Header("コース設定")]
    [SerializeField] private float laneWidth = 1.0f;

    private LayerMask groundLayer = ~0;
    private float groundRayHeight = 10f;
    private float groundRayMaxDistance = 30f;
    private int maxTotalObstacles = 300;

    private static readonly int[][,] patterns = new int[][,]
    {
        new int[,] { { 0, 1, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 0, 1, 1 }, { 1, 1, 0 }, { 1, 0, 1 } },
        new int[,] { { 2, 1, 1 }, { 1, 2, 1 }, { 1, 1, 2 }, { 1, 2, 1 }, { 2, 1, 1 }, { 1, 1, 0 } },
        new int[,] { { 3, 3, 3 }, { 2, 2, 2 }, { 0, 1, 1 }, { 3, 3, 3 }, { 1, 1, 0 }, { 2, 2, 2 } },
        new int[,] { { 0, 1, 1 }, { 2, 0, 1 }, { 1, 3, 0 }, { 1, 0, 2 }, { 1, 1, 0 }, { 0, 1, 1 } },
        new int[,] { { 0, 1, 0 }, { 2, 1, 2 }, { 1, 0, 1 }, { 3, 0, 3 }, { 1, 0, 1 }, { 0, 1, 0 }, { 2, 1, 2 } },
        new int[,] { { 1, 1, 0 }, { 1, 0, 2 }, { 0, 1, 1 }, { 3, 1, 1 }, { 1, 0, 1 }, { 1, 1, 0 }, { 0, 2, 1 } },
        new int[,] { { 2, 1, 3 }, { 1, 0, 1 }, { 3, 1, 2 }, { 1, 0, 1 }, { 2, 1, 3 }, { 0, 2, 0 } },
        new int[,] { { 0, 1, 1 }, { 1, 0, 1 }, { 1, 1, 0 }, { 0, 1, 1 }, { 1, 0, 1 }, { 1, 1, 0 } },
        new int[,] { { 1, 0, 1 }, { 0, 2, 0 }, { 1, 0, 1 }, { 0, 3, 0 }, { 1, 0, 1 }, { 0, 1, 0 }, { 1, 0, 1 } },
        new int[,] { { 2, 1, 0 }, { 1, 3, 1 }, { 0, 1, 2 }, { 1, 2, 1 }, { 3, 1, 0 }, { 1, 0, 1 }, { 0, 1, 3 } },
        new int[,] { { 2, 2, 2 }, { 1, 0, 1 }, { 3, 3, 3 }, { 0, 1, 1 }, { 2, 2, 2 }, { 1, 1, 0 }, { 3, 3, 3 } },
        new int[,] { { 1, 2, 1 }, { 0, 1, 1 }, { 1, 3, 1 }, { 1, 1, 0 }, { 1, 2, 1 }, { 1, 0, 1 } },
    };

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private float splineLengthApprox = 1f;
    private float nextSpawnDistance = 0f;
    private SplineAnimate playerSplineAnimate;
    private int currentPatternIndex = -1;
    private int currentRowIndex = 0;

    void Start()
    {
        if (playerTransform == null) playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (playerTransform != null) playerSplineAnimate = playerTransform.GetComponentInParent<SplineAnimate>();
        if (splineContainer == null) splineContainer = FindObjectOfType<SplineContainer>();
        if (itemSpawner == null) itemSpawner = GetComponent<ItemSpawner>();

        if (splineContainer != null)
        {
            splineLengthApprox = ApproximateSplineLength();
            float playerT = FindClosestTOnSpline(playerTransform.position);
            nextSpawnDistance = (playerT * splineLengthApprox) + initialSpawnOffset;
            SelectNextPattern();
        }
    }

    void Update()
    {
        if (playerTransform == null || splineContainer == null) return;
        activeObstacles.RemoveAll(obj => obj == null);

        float playerT = (playerSplineAnimate != null) ? playerSplineAnimate.NormalizedTime : FindClosestTOnSpline(playerTransform.position);
        float playerDist = playerT * splineLengthApprox;

        float targetAhead = playerDist + spawnAheadRange;
        int safety = 0;
        while (nextSpawnDistance < targetAhead && activeObstacles.Count < maxTotalObstacles && safety++ < 100)
        {
            SpawnNextRow();
        }
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) SpawnAllPatternsForDebug();
    }

    private void SelectNextPattern()
    {
        currentPatternIndex = UnityEngine.Random.Range(0, patterns.Length);
        currentRowIndex = 0;
    }

    private void SpawnNextRow()
    {
        if (currentPatternIndex < 0) SelectNextPattern();
        int[,] pattern = patterns[currentPatternIndex];
        int rowCount = pattern.GetLength(0);

        float currentGap = UnityEngine.Random.Range(minSpawnSpacing, maxSpawnSpacing);

        if (currentRowIndex >= rowCount)
        {
            float totalGap = currentGap + patternGap;
            if (itemSpawner != null) itemSpawner.SpawnPathmakingItems(pattern, rowCount - 1, nextSpawnDistance, nextSpawnDistance + totalGap, splineLengthApprox);
            nextSpawnDistance += totalGap;
            SelectNextPattern();
            return;
        }

        if (itemSpawner != null) itemSpawner.SpawnPathmakingItems(pattern, currentRowIndex, nextSpawnDistance, nextSpawnDistance + currentGap, splineLengthApprox);
        SpawnRow(pattern, currentRowIndex, nextSpawnDistance);
        currentRowIndex++;
        nextSpawnDistance += currentGap;
    }

    private void SpawnRow(int[,] pattern, int row, float distance)
    {
        float t = Mathf.Clamp01(distance / splineLengthApprox);
        Vector3 basePos = (Vector3)splineContainer.EvaluatePosition(t); 
        Vector3 tangent = Vector3.Normalize((Vector3)splineContainer.EvaluateTangent(t)); 
        Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
        Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);

        for (int lane = 0; lane < 3; lane++)
        {
            int typeID = pattern[row, lane];
            if (typeID == 0) continue;

            ObstacleCategory cat = GetCategoryByID(typeID);
            
            // プレハブリストが空でないか確認し、ランダムに1つ選択
            if (cat.prefabs != null && cat.prefabs.Count > 0)
            {
                GameObject selectedPrefab = cat.prefabs[UnityEngine.Random.Range(0, cat.prefabs.Count)];
                
                Vector3 spawnPos = basePos + right * ((lane - 1) * laneWidth);
                if (Physics.Raycast(spawnPos + Vector3.up * groundRayHeight, Vector3.down, out RaycastHit hit, groundRayMaxDistance + groundRayHeight, groundLayer))
                {
                    spawnPos.y = hit.point.y;
                }
                SpawnObstacle(cat, selectedPrefab, spawnPos, rot);
            }
        }
    }

    private ObstacleCategory GetCategoryByID(int id)
    {
        foreach (var cat in obstacleCatalog)
        {
            if (cat.id == id) return cat;
        }
        return default;
    }

    private void SpawnObstacle(ObstacleCategory cat, GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject obj = Instantiate(prefab, pos, rot);
        if (cat.isRollable && obj.GetComponent<RollableObstacle>() == null) obj.AddComponent<RollableObstacle>();
        var behavior = obj.AddComponent<ObstacleBehavior>();
        behavior.Initialize(playerTransform, this, 0.85f, 5f, groundLayer);
        activeObstacles.Add(obj);
    }

    // (ユーティリティメソッド群：FindClosestTOnSplineなどは不変)
    private float FindClosestTOnSpline(Vector3 worldPos)
    {
        float bestT = 0f; float bestDistSqr = float.MaxValue;
        for (int i = 0; i <= 100; i++)
        {
            float t = i / 100f;
            Vector3 p = (Vector3)splineContainer.EvaluatePosition(t);
            float d = (p - worldPos).sqrMagnitude;
            if (d < bestDistSqr) { bestDistSqr = d; bestT = t; }
        }
        return bestT;
    }

    private float ApproximateSplineLength()
    {
        float len = 0f; 
        Vector3 prev = (Vector3)splineContainer.EvaluatePosition(0f);
        for (int i = 1; i <= 100; i++)
        {
            Vector3 p = (Vector3)splineContainer.EvaluatePosition(i / 100f);
            len += Vector3.Distance(prev, p); prev = p;
        }
        return len > 0 ? len : 1f;
    }

    public void NotifyObstacleDestroyed(GameObject obj) { activeObstacles.Remove(obj); }

    private void SpawnAllPatternsForDebug()
    {
        float zOffset = 0f; float debugSpacing = minSpawnSpacing;
        Vector3 basePos = (playerTransform != null) ? playerTransform.position + Vector3.right * 30f : Vector3.right * 30f;
        for (int pIdx = 0; pIdx < patterns.Length; pIdx++)
        {
            int[,] p = patterns[pIdx];
            for (int r = 0; r < p.GetLength(0); r++)
            {
                for (int l = 0; l < 3; l++)
                {
                    int typeID = p[r, l];
                    if (typeID == 0) continue;
                    ObstacleCategory cat = GetCategoryByID(typeID);
                    if (cat.prefabs != null && cat.prefabs.Count > 0)
                    {
                        Vector3 pos = basePos + new Vector3(pIdx * 15f, 0, zOffset + r * debugSpacing);
                        Instantiate(cat.prefabs[0], pos, Quaternion.identity); // デバッグ時は最初の1つ
                    }
                }
            }
            zOffset += (p.GetLength(0) + 1) * debugSpacing + 20f;
        }
    }
}