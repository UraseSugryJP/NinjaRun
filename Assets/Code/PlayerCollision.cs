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
    [SerializeField] private DeathUIController deathUI;
    
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

    if (isDeathZone)
    {
        HandleDeath(TAG_DEATHZONE);
        return;
    }

    if (isObstacle)
    {
        // ローリング中の特例判定
        if (movement != null && movement.IsRolling && target.GetComponentInParent<RollableObstacle>() != null) return;

        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null)
        {
            // --- 修正箇所：回転に対応した高さ判定 ---
            
            // 障害物の中心とワールドサイズを取得
            Vector3 obsCenter = targetCol.bounds.center;
            float obsHeight = targetCol.bounds.size.y;
            float obstacleTop = obsCenter.y + (obsHeight * 0.5f);
            
            // プレイヤーの足元の位置
            float playerBottom = transform.position.y;

            // 判定の「遊び（許容範囲）」を少し広げる (0.02f -> 0.1f程度)
            // 回転している場合、bounds.size.y が実際より大きくなることがあるため
            float threshold = 0.1f; 

            if (playerBottom > obstacleTop - threshold)
            {
                // 足元が障害物の天面付近、あるいはそれより上なら「乗った」とみなす
                return; 
            }
        }

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

    // 1. スプライン移動を即座に破棄（これで無限走行が止まる）
    var animator = GetComponent<UnityEngine.Splines.SplineAnimate>();
    if (animator != null) Destroy(animator); 

    // 2. 物理挙動を爆発させる
    Rigidbody rb = GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.isKinematic = false; // 物理を有効化
        rb.useGravity = true;
        // 少し後ろに弾き飛ばされる衝撃を加える
        rb.AddForce(-transform.forward * 5f + Vector3.up * 3f, ForceMode.Impulse);
    }

    // 3. デス演出呼び出し
    if (deathUI != null) deathUI.ShowDeathEffect();
    
    // タイムスケールを少し遅くして「何が起きたか」見せる
    Time.timeScale = 0.5f; 
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