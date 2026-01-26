using System.Collections;
using System.Collections.Generic; // �X���b�V�����h�b�g�ɏC��
using UnityEngine;
using UnityEngine.SceneManagement;

public class continueYesManager : MonoBehaviour
{
    public void OnYesButton()
    {
        SceneManager.LoadScene("RunSceneA");
    }
}
