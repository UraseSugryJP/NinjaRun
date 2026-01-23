using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    // 生成するアイテムのプレハブ
    public GameObject itemPrefab;

    // 基準となる床のオブジェクト（Inspectorで設定）
    public GameObject ground;

    // コースの設定
    private float centerOffsetX = -20.0f;
    private float laneWidth = 3.0f;

    [Header("生成の設定")]
    public int spawnCount = 10;
    public float startOffset = 5.0f;
    public float zInterval = 3.0f;

    private float groundStartZ;

    void Start()
    {
        // 床オブジェクトが設定されているか確認
        if (ground != null)
        {
            Renderer groundRenderer = ground.GetComponent<Renderer>();

            // 床の開始地点（Z座標の最小値）を取得
            groundStartZ = groundRenderer.bounds.min.z;

            // 床の長さ（Z方向のサイズ）を取得
            float groundLength = groundRenderer.bounds.size.z;

            // 設定された間隔（zInterval）に基づいて、配置可能な個数を自動計算
            if (zInterval > 0)
            {
                float availableLength = groundLength - startOffset;
                // 床の長さに収まる最大個数を計算（マイナスにならないよう制御）
                spawnCount = Mathf.Max(0, Mathf.FloorToInt(availableLength / zInterval));
            }

            // デバッグ用ログ：計算結果の確認
            Debug.Log($"使用する床: {ground.name}, 長さ: {groundLength}, 生成数: {spawnCount}");
        }
        else
        {
            Debug.LogError("エラー: ItemSpawnerの「Ground」に床のオブジェクトが設定されていません。");
            return;
        }

        // 計算された個数分だけアイテムを生成
        for (int i = 0; i < spawnCount; i++)
        {
            // レーンをランダムに決定（-1:左, 0:中央, 1:右）
            int laneIndex = Random.Range(-1, 2);
            SpawnOneItem(i, laneIndex);
        }
    }

    // 指定されたインデックスとレーンにアイテムを1つ生成する
    void SpawnOneItem(int index, int lane)
    {
        float x = (lane * laneWidth) + centerOffsetX;
        float z = groundStartZ + startOffset + (index * zInterval);
        float y = -0.5f;

        Vector3 spawnPos = new Vector3(x, y, z);
        Instantiate(itemPrefab, spawnPos, Quaternion.identity);
    }
}