using System.Collections;
using System.Collections.Generic; // スラッシュをドットに修正
using UnityEngine;
using UnityEngine.SceneManagement;

// クラス名の後ろに「: MonoBehaviour」を追加
public class TitleManager : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("RunStage1");
    }
}