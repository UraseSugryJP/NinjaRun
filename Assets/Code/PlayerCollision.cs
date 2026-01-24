using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody))]
public class PlayerCollision : MonoBehaviour
{
    [Header("参照（自動取得するので空欄でOK）")]
    [SerializeField] private PlayerMovement movement; 

    [Header("自動リスタートの設定")]
    [SerializeField] private float restartDelay = 1.0f; 

    private const string TAG_OBSTACLE = "Obstacle";
    private const string TAG_DEATHZONE = "DeathZone";

    private bool isDead = false;
    private bool restartTriggered = false;

    void Start()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 座標制御のため Kinematic
            rb.useGravity = false;
        }

        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // ゲームオーバー時の入力監視
        if (isDead && !restartTriggered)
        {
            bool shouldRestart = false;
            if (Keyboard.current != null && (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) shouldRestart = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) shouldRestart = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) shouldRestart = true;

            if (shouldRestart)
            {
                restartTriggered = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    // すべての衝突をこのメソッドに集約
    private void OnTriggerEnter(Collider other) => CheckCollision(other.gameObject);
    private void OnCollisionEnter(Collision collision) => CheckCollision(collision.gameObject);

    private void CheckCollision(GameObject target)
{
    if (isDead || target == null) return;

    bool isDeathZone = SafeCompareTag(target, TAG_DEATHZONE);
    bool isObstacle = SafeCompareTag(target, TAG_OBSTACLE);

    // 1. 【最優先】デスゾーンに触れたら即座に死亡
    if (isDeathZone)
    {
        Debug.Log("DeathZoneに接触！");
        HandleDeath(TAG_DEATHZONE);
        return; // これ以降の判定（高さチェックなど）はしない
    }

    // 2. 障害物(Obstacle)の場合
    if (isObstacle)
    {
        // ローリング中の特例判定
        if (movement != null && movement.IsRolling && target.GetComponentInParent<RollableObstacle>() != null) return;

        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null)
        {
            float obstacleTop = targetCol.bounds.max.y;
            float playerBottom = transform.position.y;

            // シビアな判定（高さの猶予を 0.02f に設定）
            if (playerBottom > obstacleTop - 0.02f)
            {
                // 足元に障害物があるのでセーフ（乗っている状態）
                return; 
            }
        }

        // 高さが足りなければ死亡
        HandleDeath(TAG_OBSTACLE);
    }
}

    private bool SafeCompareTag(GameObject go, string tag)
    {
        if (go == null) return false;
        return go.CompareTag(tag);
    }

    private void HandleDeath(string tag)
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Game Over! 原因: " + tag);

        if (movement != null) movement.enabled = false;
        
        // Splineの停止
        if (transform.parent != null)
        {
            var cart = transform.parent.GetComponent<UnityEngine.Splines.SplineAnimate>();
            if (cart != null) cart.Pause();
        }

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        if (!restartTriggered)
        {
            restartTriggered = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}