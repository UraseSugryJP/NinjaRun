using UnityEngine;

// Rigidbodyがないとエラーになるように強制
[RequireComponent(typeof(Rigidbody))]
public class PlayerCollision : MonoBehaviour
{
    [Header("参照（自動取得するので空欄でOK）")]
    [SerializeField] private PlayerMovement movement; // 移動スクリプト
    //[SerializeField] private Animator animator;       // アニメーター

    // タグ定数（タグ名はUnityエディタと合わせる）
    private const string TAG_OBSTACLE = "Obstacle";
    private const string TAG_DEATHZONE = "DeathZone";

    private bool isDead = false;

    void Start()
    {
        // 1. Rigidbodyの安全設定（物理挙動をOFFにする）
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.useGravity = false;
        }

        // 2. 移動スクリプトを自動取得
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }
    }

    // Is Trigger ON の物体（落下ゾーンやアイテム）との接触
    private void OnTriggerEnter(Collider other)
    {
        CheckCollision(other.gameObject);
    }

    // Is Trigger OFF の物体（壁など）との接触
    private void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision.gameObject);
    }

    // 共通の判定処理
    private void CheckCollision(GameObject target)
    {
        if (isDead) return;

        // 障害物 または 落下ゾーン に触れたらアウト
        if (target.CompareTag(TAG_OBSTACLE) || target.CompareTag(TAG_DEATHZONE))
        {
            HandleDeath(target.tag);
        }
    }

    private void HandleDeath(string tag)
    {
        isDead = true;
        Debug.Log("Game Over! 原因: " + tag);

        // 1. プレイヤーの操作・移動を止める
        if (movement != null)
        {
            movement.enabled = false; 
        }

        // 2. 親のカート（コース移動）を止める
        // 親がいるか確認してから取得
        if (transform.parent != null)
        {
            var cart = transform.parent.GetComponent<UnityEngine.Splines.SplineAnimate>();
            if (cart != null)
            {
                cart.Pause();
            }
        }
    }
}