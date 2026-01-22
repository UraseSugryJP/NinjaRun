using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SplineContainer splineContainer;

    [Header("障害物プレハブ")]
    [SerializeField] private GameObject wallPrefab;           // 1: 壁（避けるしかない）
    [SerializeField] private GameObject jumpObstaclePrefab;   // 2: 低い障害物（ジャンプで越える）
    [SerializeField] private GameObject rollObstaclePrefab;   // 3: 高い障害物（ローリングでくぐる）

    [Header("生成距離設定（メートル単位）")]
    [SerializeField] private float spawnSpacing = 15.0f;      // 1行あたりのZ間隔
    [SerializeField] private float patternGap = 10.0f;        // パターン間の間隔
    [SerializeField] private float initialSpawnOffset = 20.0f;
    [SerializeField] private float spawnAheadRange = 50.0f;

    [Header("レーン/位置")]
    [SerializeField] private float laneWidth = 3.0f;

    [Header("地面検出")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundRayHeight = 10f;
    [SerializeField] private float groundRayMaxDistance = 30f;

    [Header("制限")]
    [SerializeField] private int maxTotalObstacles = 300;

    [Header("重なり防止")]
    [SerializeField] private float minObstacleDistance = 2.0f;

    // ============================================================
    // パターン定義
    // 0: なし（安全）
    // 1: 壁（左右に避けるしかない / ジャンプもロールも不可）
    // 2: 低い障害物（ジャンプで越える）
    // 3: 高い障害物（ローリングでくぐる）
    // ============================================================
    private static readonly int[][,] patterns = new int[][,]
    {
        // パターン1：ジャンプの基本
        new int[,] {
            { 0, 0, 0 },
            { 0, 2, 0 },
            { 0, 0, 0 },
            { 0, 2, 0 },
            { 0, 0, 0 }
        },
        // パターン2：ローリングゲート
        new int[,] {
            { 0, 0, 0 },
            { 1, 3, 1 },
            { 1, 3, 1 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        },
        // パターン3：ワイドバー（強制アクション）
        new int[,] {
            { 0, 0, 0 },
            { 2, 2, 2 },
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 3, 3, 3 }
        },
        // パターン4：ハイ＆ロー（交互）
        new int[,] {
            { 0, 2, 0 },
            { 0, 0, 0 },
            { 0, 3, 0 },
            { 0, 0, 0 },
            { 0, 1, 0 }
        },
        // パターン5：選べる苦痛（分岐）
        new int[,] {
            { 0, 0, 0 },
            { 2, 1, 3 },
            { 2, 1, 3 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        },
        // パターン6：アクション階段
        new int[,] {
            { 2, 0, 0 },
            { 0, 0, 0 },
            { 0, 3, 0 },
            { 0, 0, 0 },
            { 0, 0, 2 }
        },
        // パターン7：インポッシブル・ウォール（ひっかけ）
        new int[,] {
            { 1, 3, 1 },
            { 1, 0, 1 },
            { 0, 0, 0 },
            { 2, 2, 2 },
            { 0, 0, 0 }
        },
        // パターン8：トリッキーロード（全部盛り）
        new int[,] {
            { 2, 0, 1 },
            { 0, 0, 0 },
            { 1, 3, 1 },
            { 0, 0, 0 },
            { 1, 2, 0 }
        },
    };

    // 内部変数
    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private readonly Dictionary<GameObject, float> spawnDistanceMap = new Dictionary<GameObject, float>();

    private float splineLengthApprox = 1f;
    private int lengthSampleCount = 200;
    private float nextSpawnDistance = 0f;
    private float playerSplineDistance = 0f;
    private UnityEngine.Splines.SplineAnimate playerSplineAnimate;

    // パターン生成状態
    private int currentPatternIndex = -1;
    private int currentRowIndex = 0;

    // デバッグ用: 全パターン表示
    private readonly List<GameObject> debugSpawnedObjects = new List<GameObject>();
    [Header("デバッグ")]
    [SerializeField] private float debugSpawnOffsetX = 30f; // レーンからの横オフセット

    void Start()
    {
        if (playerTransform == null)
        {
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            else
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) playerTransform = go.transform;
            }
        }

        if (playerTransform != null)
        {
            playerSplineAnimate = playerTransform.GetComponentInParent<UnityEngine.Splines.SplineAnimate>();
        }

        if (splineContainer == null)
        {
            splineContainer = FindObjectOfType<SplineContainer>();
        }

        if (splineContainer != null)
        {
            splineLengthApprox = ApproximateSplineLength();
            float playerT = FindClosestTOnSpline(playerTransform.position);
            playerSplineDistance = playerT * splineLengthApprox;
            nextSpawnDistance = playerSplineDistance + initialSpawnOffset;

            SelectNextPattern();
            
            // デバッグ: プレハブ割り当て確認
            if (wallPrefab == null) Debug.LogError("[ObstacleSpawner] wallPrefab が未設定です！");
            if (jumpObstaclePrefab == null) Debug.LogError("[ObstacleSpawner] jumpObstaclePrefab が未設定です！");
            if (rollObstaclePrefab == null) Debug.LogError("[ObstacleSpawner] rollObstaclePrefab が未設定です！");
            Debug.Log($"[ObstacleSpawner] 初期化完了: splineLength={splineLengthApprox}, nextSpawn={nextSpawnDistance}");
        }
        else
        {
            Debug.LogWarning("[ObstacleSpawner] SplineContainer が見つかりません。");
        }
    }

    void Update()
    {
        if (playerTransform == null || splineContainer == null) return;

        int recycleLimitPerFrame = 8;
        int recycled = 0;
        while (activeObstacles.Count >= maxTotalObstacles && recycled < recycleLimitPerFrame)
        {
            ReclaimOldestOne();
            recycled++;
        }

        // nullオブジェクトのクリーンアップ
        activeObstacles.RemoveAll(obj => obj == null);

        float playerT;
        if (playerSplineAnimate != null)
        {
            playerT = playerSplineAnimate.NormalizedTime;
        }
        else
        {
            playerT = FindClosestTOnSpline(playerTransform.position);
        }
        playerSplineDistance = playerT * splineLengthApprox;

        float targetAhead = playerSplineDistance + spawnAheadRange;
        int safety = 0;
        while (nextSpawnDistance < targetAhead && activeObstacles.Count < maxTotalObstacles && safety++ < 100)
        {
            SpawnNextRow();
        }

        // デバッグ: Pキーで全パターンを表示
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            SpawnAllPatternsForDebug();
        }
    }

    private void SelectNextPattern()
    {
        currentPatternIndex = Random.Range(0, patterns.Length);
        currentRowIndex = 0;
    }

    private void SpawnNextRow()
    {
        if (currentPatternIndex < 0 || currentPatternIndex >= patterns.Length)
        {
            SelectNextPattern();
        }

        int[,] pattern = patterns[currentPatternIndex];
        int rowCount = pattern.GetLength(0);

        if (currentRowIndex >= rowCount)
        {
            nextSpawnDistance += patternGap;
            SelectNextPattern();
            return;
        }

        SpawnRow(pattern, currentRowIndex, nextSpawnDistance);
        currentRowIndex++;
        nextSpawnDistance += spawnSpacing;
    }

    private void SpawnRow(int[,] pattern, int row, float distance)
    {
        Vector3 basePos;
        Vector3 tangent;
        
        if (distance <= splineLengthApprox)
        {
            // スプライン内: 通常通り評価
            float t = distance / splineLengthApprox;
            basePos = splineContainer.EvaluatePosition(t);
            var tangentRaw = splineContainer.EvaluateTangent(t);
            tangent = new Vector3(tangentRaw.x, tangentRaw.y, tangentRaw.z).normalized;
        }
        else
        {
            // スプライン外: 終端から直線的に延長
            Vector3 endPos = splineContainer.EvaluatePosition(1f);
            var endTangentRaw = splineContainer.EvaluateTangent(1f);
            tangent = new Vector3(endTangentRaw.x, endTangentRaw.y, endTangentRaw.z).normalized;
            float extraDistance = distance - splineLengthApprox;
            basePos = endPos + tangent * extraDistance;
        }
        
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(tangent, up).normalized;
        Quaternion rot = Quaternion.LookRotation(tangent, up);

        for (int lane = 0; lane < 3; lane++)
        {
            int obstacleType = pattern[row, lane];
            if (obstacleType == 0) continue;

            int laneOffset = lane - 1;
            Vector3 spawnPos = basePos + right * (laneOffset * laneWidth);

            Vector3 rayStart = spawnPos + Vector3.up * groundRayHeight;
            RaycastHit hit;
            bool hitGround = Physics.Raycast(rayStart, Vector3.down, out hit, groundRayMaxDistance + groundRayHeight, groundLayer);
            if (hitGround)
            {
                spawnPos.y = hit.point.y;
            }

            bool tooClose = false;
            float minDistSqr = minObstacleDistance * minObstacleDistance;
            foreach (var existing in activeObstacles)
            {
                if (existing == null) continue;
                float sqrDist = (spawnPos - existing.transform.position).sqrMagnitude;
                if (sqrDist < minDistSqr)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            GameObject prefab = GetPrefabByType(obstacleType);
            if (prefab == null) continue;

            SpawnObstacle(prefab, spawnPos, rot, distance, obstacleType);
        }
    }

    private GameObject GetPrefabByType(int type)
    {
        switch (type)
        {
            case 1: return wallPrefab;
            case 2: return jumpObstaclePrefab;
            case 3: return rollObstaclePrefab;
            default: return null;
        }
    }

    private void SpawnObstacle(GameObject prefab, Vector3 position, Quaternion rotation, float distance, int obstacleType)
    {
        GameObject obj = null;
        try
        {
            obj = Instantiate(prefab, position, rotation);
            var spawnedSpawner = obj.GetComponent<ObstacleSpawner>();
            if (spawnedSpawner != null) Destroy(spawnedSpawner);

            SetTagRecursively(obj, "Obstacle");

            // rollObstacle (type 3) には RollableObstacle を追加
            if (obstacleType == 3)
            {
                if (obj.GetComponent<RollableObstacle>() == null)
                {
                    obj.AddComponent<RollableObstacle>();
                }
            }

            var behavior = obj.AddComponent<ObstacleBehavior>();
            behavior.Initialize(playerTransform, this, colliderScale: 0.85f, destroyBehindDistance: 5f, ground: groundLayer);

            activeObstacles.Add(obj);
            spawnDistanceMap[obj] = distance;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ObstacleSpawner] Error: " + ex);
            if (obj != null) Destroy(obj);
        }
    }

    private void ReclaimOldestOne()
    {
        if (activeObstacles.Count == 0) return;

        GameObject oldest = null;
        float oldestDist = float.MaxValue;

        foreach (var obj in activeObstacles)
        {
            if (obj == null) continue;

            float d;
            if (spawnDistanceMap.TryGetValue(obj, out d)) { }
            else
            {
                float t = FindClosestTOnSpline(obj.transform.position);
                d = t * splineLengthApprox;
            }

            if (d < oldestDist)
            {
                oldestDist = d;
                oldest = obj;
            }
        }

        if (oldest != null)
        {
            activeObstacles.Remove(oldest);
            if (spawnDistanceMap.ContainsKey(oldest)) spawnDistanceMap.Remove(oldest);
            try { Destroy(oldest); } catch { }
        }
    }

    private float FindClosestTOnSpline(Vector3 worldPos)
    {
        if (splineContainer == null) return 0f;
        float bestT = 0f;
        float bestDistSqr = float.MaxValue;
        int samples = Mathf.Max(32, lengthSampleCount);
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 p = splineContainer.EvaluatePosition(t);
            float d = (p - worldPos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                bestT = t;
            }
        }
        return bestT;
    }

    private float ApproximateSplineLength()
    {
        if (splineContainer == null) return 1f;
        float len = 0f;
        Vector3 prev = splineContainer.EvaluatePosition(0f);
        int samples = Mathf.Max(32, lengthSampleCount);
        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 p = splineContainer.EvaluatePosition(t);
            len += Vector3.Distance(prev, p);
            prev = p;
        }
        if (len <= 0f) len = 1f;
        return len;
    }

    public void NotifyObstacleDestroyed(GameObject obj)
    {
        if (activeObstacles.Contains(obj)) activeObstacles.Remove(obj);
        if (spawnDistanceMap.ContainsKey(obj)) spawnDistanceMap.Remove(obj);
    }

    private void SetTagRecursively(GameObject go, string tag)
    {
        if (go == null) return;
        try { go.tag = tag; } catch { }
        foreach (Transform child in go.transform) SetTagRecursively(child.gameObject, tag);
    }

    // ============================================================
    // デバッグ: 全パターンを一覧表示
    // ============================================================
    private void SpawnAllPatternsForDebug()
    {
        // 既存のデバッグオブジェクトを削除
        foreach (var obj in debugSpawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        debugSpawnedObjects.Clear();

        // プレイヤーの現在位置を基準にする
        Vector3 basePos = playerTransform.position + Vector3.right * debugSpawnOffsetX;
        float zOffset = 0f;
        float patternSpacingZ = 20f; // パターン間の距離

        for (int patternIdx = 0; patternIdx < patterns.Length; patternIdx++)
        {
            int[,] pattern = patterns[patternIdx];
            int rowCount = pattern.GetLength(0);

            for (int row = 0; row < rowCount; row++)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    int obstacleType = pattern[row, lane];
                    if (obstacleType == 0) continue;

                    GameObject prefab = GetPrefabByType(obstacleType);
                    if (prefab == null) continue;

                    // X: パターン番号ごとにずらす, Y: 地面, Z: 行ごと
                    Vector3 spawnPos = basePos + new Vector3(
                        patternIdx * 15f,  // パターンごとに横にずらす
                        0f,
                        zOffset + row * spawnSpacing + (lane - 1) * 0.1f
                    );

                    // 地面検出
                    Vector3 rayStart = spawnPos + Vector3.up * groundRayHeight;
                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayMaxDistance + groundRayHeight, groundLayer))
                    {
                        spawnPos.y = hit.point.y;
                    }

                    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                    debugSpawnedObjects.Add(obj);
                }
            }

            zOffset += (rowCount + 1) * spawnSpacing + patternSpacingZ;
        }

        Debug.Log($"[ObstacleSpawner] デバッグ: {patterns.Length}個のパターンを生成しました (合計{debugSpawnedObjects.Count}オブジェクト)");
    }
}
