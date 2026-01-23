using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public int score = 0;
    private TextMeshProUGUI scoreText;

    void Start()
    {
        // アタッチされているTextMeshProコンポーネントを取得
        scoreText = GetComponent<TextMeshProUGUI>();
    }

    // スコアを加算し、UIを更新する
    public void AddScore(int amount)
    {
        score += amount;

        if (scoreText != null)
        {
            scoreText.text = "score " + score;
        }
    }
}