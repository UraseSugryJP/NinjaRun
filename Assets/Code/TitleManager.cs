using System.Collections;
using System.Collections.Generic; // �X���b�V�����h�b�g�ɏC��
using UnityEngine;
using UnityEngine.SceneManagement;

// �N���X���̌��Ɂu: MonoBehaviour�v��ǉ�
public class TitleManager : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("RunSceneA");
    }
}