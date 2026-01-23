using System.Collections;
using System.Collections.Generic; // スラッシュをドットに修正
using UnityEngine;
using UnityEngine.SceneManagement;

public class continueYesManager : MonoBehaviour
{
    public void OnYesButton()
    {
        SceneManager.LoadScene("RunStage1");
    }
}
