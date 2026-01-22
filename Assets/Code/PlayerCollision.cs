using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Rigidbodyがないとエラーになるように強制
[RequireComponent(typeof(Rigidbody))]
public class PlayerCollision : MonoBehaviour
{
    [Header("参照（自動取得するので空欄でOK）")]
    [SerializeField] private PlayerMovement movement; // 移動スクリプト

    [Header("自動リスタートの設定")]
    [SerializeField] private float restartDelay = 1.0f; // 衝突後に何秒で最初からにするか

    // タグ定数（タグ名はUnityエディタと合わせる）
    private const string TAG_OBSTACLE = "Obstacle";
    private const string TAG_DEATHZONE = "DeathZone";

    private bool isDead = false;
    private bool restartTriggered = false;

    void Start()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        // GameOver 後に即時リトライを許可：Rキー / Space / タップ
        if (isDead && !restartTriggered)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.touchCount > 0)
            {
                restartTriggered = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision.gameObject);
    }

    private void CheckCollision(GameObject target)
    {
        if (isDead || target == null) return;

        // ローリング中は RollableObstacle を無視
        if (movement != null && movement.IsRolling)
        {
            var rollable = target.GetComponentInParent<RollableObstacle>();
            if (rollable != null)
            {
                return; // 衝突を無視
            }
        }

        Debug.Log($"[PlayerCollision] Collided with '{target.name}' tag='{SafeGetTag(target)}' pos={target.transform.position}");

        bool isDeathZone = SafeCompareTag(target, TAG_DEATHZONE);
        bool isObstacle = SafeCompareTag(target, TAG_OBSTACLE);

        if (!isObstacle)
        {
            var ob = target.GetComponentInParent<ObstacleBehavior>();
            if (ob != null) isObstacle = true;
        }

        if (isObstacle || isDeathZone)
        {
            HandleDeath(isObstacle ? TAG_OBSTACLE : TAG_DEATHZONE);
        }
    }

    private bool SafeCompareTag(GameObject go, string tag)
    {
        if (go == null) return false;
        try
        {
            return go.CompareTag(tag);
        }
        catch
        {
            return false;
        }
    }

    private string SafeGetTag(GameObject go)
    {
        if (go == null) return "(null)";
        try
        {
            return go.tag;
        }
        catch
        {
            return "(undefined)";
        }
    }

    private void HandleDeath(string tag)
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Game Over! 原因: " + tag);

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (transform.parent != null)
        {
            var cart = transform.parent.GetComponent<UnityEngine.Splines.SplineAnimate>();
            if (cart != null)
            {
                cart.Pause();
            }
        }

        // 遅延リロード（ユーザーが即押しすれば Update 側で即リロードされる）
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