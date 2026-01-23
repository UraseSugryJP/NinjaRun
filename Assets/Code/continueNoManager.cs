using System.Collections;
using System.Collections.Generic; // スラッシュをドットに修正
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueNoManager : MonoBehaviour
{
    public void OnNoButton()
    {
        SceneManager.LoadScene("StartScene");
    }
}
