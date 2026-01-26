using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathUIController : MonoBehaviour
{
    [SerializeField] private Image deathImage;
    [SerializeField] private float fadeDuration = 0.5f; // 赤くなるまでの時間
    [SerializeField] private float waitTime = 3.0f;    // 赤くなってからリロードまでの待ち時間

    void Awake()
    {
        // 初期状態は透明
        if (deathImage != null)
        {
            Color c = deathImage.color;
            c.a = 0;
            deathImage.color = c;
        }
    }

    public void ShowDeathEffect()
    {
        if (deathImage == null) return;
        StartCoroutine(DeathSequence());
    }

   private IEnumerator DeathSequence()
{
    // 画面を激しく揺らす（簡易版）
    Vector3 originalPos = Camera.main.transform.localPosition;
    float shakeTime = 0.2f;
    while (shakeTime > 0) {
        Camera.main.transform.localPosition = originalPos + Random.insideUnitSphere * 0.5f;
        shakeTime -= Time.unscaledDeltaTime;
        yield return null;
    }
    Camera.main.transform.localPosition = originalPos;

    // 赤幕フェード（unscaledDeltaTimeを使うこと！）
    float elapsed = 0f;
    while (elapsed < fadeDuration) {
        elapsed += Time.unscaledDeltaTime;
        deathImage.color = new Color(1, 0, 0, Mathf.Lerp(0f, 0.5f, elapsed / fadeDuration));
        yield return null;
    }

    yield return new WaitForSecondsRealtime(waitTime);

    // 【重要】リロード前に時間を元に戻す
    Time.timeScale = 1f; 
    UnityEngine.SceneManagement.SceneManager.LoadScene(2);
}
}