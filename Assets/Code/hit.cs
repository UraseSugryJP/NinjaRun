using UnityEngine;

public class hit : MonoBehaviour
{
    // トリガー（IsTriggerがONのCollider）に接触した時の処理
    private void OnTriggerEnter(Collider other)
    {
        // 衝突した相手がプレイヤーかどうかをタグで判定
        if (other.CompareTag("Player"))
        {
            // シーン内のScoreManagerを探してスコアを加算
            ScoreManager sm = FindObjectOfType<ScoreManager>();
            if (sm != null)
            {
                sm.AddScore(100);
                Debug.Log("hit!!!!");
            }

            // 衝突後、このオブジェクト（アイテム自身）を削除
            Destroy(gameObject);
        }
    }
}