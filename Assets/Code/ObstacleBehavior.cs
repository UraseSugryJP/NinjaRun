using UnityEngine;

// 障害物に動的に付与して使用する軽量コンポーネント
public class ObstacleBehavior : MonoBehaviour
{
    private Transform playerTransform;

    private float destroyBehindDistance = 5f;
    private float colliderScale = 0.85f;
    private LayerMask groundLayer;
    private ObstacleSpawner spawner;

    // 動的に作る小さめの当たり判定（トリガー）
    private GameObject hitboxGO;
    private bool showGizmos = true;
    private Vector3 hitboxWorldCenter;
    private Vector3 hitboxWorldSize;

    // 初期化：Spawner から呼び出す
    public void Initialize(Transform player, ObstacleSpawner spawner, float colliderScale, float destroyBehindDistance, LayerMask ground)
    {
        this.playerTransform = player;
        this.spawner = spawner;
        this.colliderScale = Mathf.Clamp(colliderScale, 0.1f, 1.0f);
        this.destroyBehindDistance = Mathf.Max(0.1f, destroyBehindDistance);
        this.groundLayer = ground;

        ShrinkColliders();
        CreateHitbox();
    }

    private void ShrinkColliders()
    {
        // BoxCollider, SphereCollider, CapsuleCollider を縮小
        foreach (var box in GetComponentsInChildren<BoxCollider>())
        {
            box.size = Vector3.Scale(box.size, new Vector3(colliderScale, colliderScale, colliderScale));
        }
        foreach (var sph in GetComponentsInChildren<SphereCollider>())
        {
            sph.radius *= colliderScale;
        }
        foreach (var cap in GetComponentsInChildren<CapsuleCollider>())
        {
            cap.radius *= colliderScale;
            cap.height *= colliderScale;
        }
        // MeshCollider は扱わない（複雑なため）
    }

    private void CreateHitbox()
    {
        // 既に作成済みなら更新のみ
        if (hitboxGO != null)
        {
            Destroy(hitboxGO);
            hitboxGO = null;
        }

        // まず子レンダラーのワールド境界を集める
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool haveBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        if (renderers != null && renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            haveBounds = true;
        }
        else
        {
            // レンダラーがなければコライダーの境界を使う
            Collider[] cols = GetComponentsInChildren<Collider>();
            if (cols != null && cols.Length > 0)
            {
                bounds = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) bounds.Encapsulate(cols[i].bounds);
                haveBounds = true;
            }
        }

        if (!haveBounds)
        {
            bounds = new Bounds(transform.position, Vector3.one);
        }

        // ヒットボックス用の子オブジェクトを作成（トリガー）
        hitboxGO = new GameObject("Hitbox");
        hitboxGO.transform.SetParent(transform, true);
        hitboxGO.transform.position = bounds.center;
        hitboxGO.transform.rotation = transform.rotation;
        hitboxGO.tag = "Obstacle"; // プレイヤー側はタグで判定しているため設定
        hitboxGO.layer = gameObject.layer;

        var bc = hitboxGO.AddComponent<BoxCollider>();
        bc.isTrigger = true;

        // BoxCollider.size はローカルサイズなので、簡単のためワールドサイズをそのまま入れる。
        // 若干小さめのヒットボックスにする（colliderScale を使用）
        Vector3 worldSize = bounds.size;
        if (worldSize.magnitude < 0.001f) worldSize = Vector3.one * 1.0f;
        bc.size = worldSize * colliderScale;

        // 中心は子オブジェクトのローカル原点に合わせる
        bc.center = Vector3.zero;

        // 保存（表示用）
        hitboxWorldCenter = hitboxGO.transform.position;
        hitboxWorldSize = bc.size;

        // 備考: 既存のコライダは縮小済み。ヒット判定はこのトリガーで行う想定。
    }

    void Update()
    {
        if (playerTransform == null) return;

        // trackCenter を player の親とみなす（カーブ対応）
        Transform trackCenter = playerTransform.parent != null ? playerTransform.parent : playerTransform;

        // 前方差分（プレイヤーから障害物の前に移動しているか）
        float forwardDelta = Vector3.Dot(trackCenter.forward, playerTransform.position - transform.position);

        // プレイヤーが十分前に進んだら破棄
        if (forwardDelta > destroyBehindDistance)
        {
            DestroySelf();
        }
    }

    private void DestroySelf()
    {
        // Spawner に通知（あれば）
        if (spawner != null)
        {
            spawner.NotifyObstacleDestroyed(gameObject);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 念のため spawner があれば通知
        if (spawner != null)
        {
            spawner.NotifyObstacleDestroyed(gameObject);
        }
        if (hitboxGO != null)
        {
            // 重複防止
            Destroy(hitboxGO);
            hitboxGO = null;
        }
    }

    // シーン内でヒットボックスの位置とサイズを確認できるようにする
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        if (hitboxGO == null)
        {
            // まだ作られていない場合、自身のレンダラー境界を描画
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(b.center, b.size * colliderScale);
            }
            return;
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(hitboxWorldCenter, hitboxWorldSize);
    }
}
