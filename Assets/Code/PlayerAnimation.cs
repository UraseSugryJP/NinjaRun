using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // コルーチンを使うために必要

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    
    // アニメーション中かどうかを管理するフラグ（連打防止用）
    private bool isActioning = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || isActioning) return; // 何かアクション中なら入力を受け付けない

        // ジャンプ（上キー）
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ActionRoutine("isJumping", 0.8f)); // 0.8秒間ジャンプ状態
        }

        // ローリング（下キー）
        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ActionRoutine("isRolling", 0.6f)); // 0.6秒間ローリング状態
        }
    }

    // 時間でフラグを管理する共通コルーチン
    IEnumerator ActionRoutine(string paramName, float duration)
    {
        isActioning = true;            // アクション開始（二重入力禁止）
        anim.SetBool(paramName, true);  // アニメーターのBoolをtrueにする

        yield return new WaitForSeconds(duration); // 指定した時間だけ待機

        anim.SetBool(paramName, false); // 時間が来たらfalseに戻す
        isActioning = false;           // アクション終了（入力を解禁）
    }
}
