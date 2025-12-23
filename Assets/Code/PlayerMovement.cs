using UnityEngine;
using UnityEngine.InputSystem; // ★これが必要

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("左右移動設定")]
    [SerializeField] private float sideSpeed = 10f;
    [SerializeField] private float limitX = 4.5f;

    [Header("ジャンプ・重力設定")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 25f;

    [Header("接地判定設定")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 10f;

    // --- 内部変数 ---
    private CapsuleCollider col;
    private float verticalVelocity;
    private float defaultHeight;
    private float defaultCenterY;
    private float currentRollHeight = 0.5f; // ローリング時の高さ
    private float currentRollCenterY = 0.25f;

    public bool IsGrounded { get; private set; }
    public bool IsRolling { get; private set; }

    void Start()
    {
        col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            defaultHeight = col.height;
            defaultCenterY = col.center.y;
        }
    }

    void Update()
    {
        Debug.Log("Update動いてます: " + transform.position);
        // 入力値の取得（ここが変わった！）
        float h = GetMoveInput();
        bool jumpInput = GetJumpInput();
        bool rollInput = GetRollInput();

        // 1. 横移動
        HandleSideMovement(h);
        
        // 2. 接地判定
        float groundY = GetGroundHeight();
        
        // 3. ジャンプ・重力
        HandleGravityAndJump(groundY, jumpInput, rollInput);
        
        // 4. ローリング
        HandleRolling(rollInput);
    }

    // --- ▼▼▼ 新しい入力システム対応部分 ▼▼▼ ---

    private float GetMoveInput()
    {
        float h = 0f;

        // キーボード (矢印 or A/D)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) h = -1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) h = 1f;
        }

        // ゲームパッド (左スティック)
        if (Gamepad.current != null)
        {
            // スティックが倒れていれば上書き
            if (Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > 0.1f)
            {
                h = Gamepad.current.leftStick.x.ReadValue();
            }
        }
        return h;
    }

    private bool GetJumpInput()
    {
        // キーボードの上 or スペース
        bool keyJump = Keyboard.current != null && 
                      (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        
        // パッドの南ボタン(A/×)
        bool padJump = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        return keyJump || padJump;
    }

    private bool GetRollInput()
    {
        // キーボードの下
        bool keyRoll = Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
        
        // パッドの東ボタン(B/○) または 左スティック下
        bool padRoll = Gamepad.current != null && 
                      (Gamepad.current.buttonEast.isPressed || Gamepad.current.leftStick.y.ReadValue() < -0.5f);

        return keyRoll || padRoll;
    }

    // --- ▲▲▲ ここまで ▲▲▲ ---

    private void HandleSideMovement(float h)
    {
        Vector3 localPos = transform.localPosition;
        localPos.x += h * sideSpeed * Time.deltaTime;
        localPos.x = Mathf.Clamp(localPos.x, -limitX, limitX);
        transform.localPosition = localPos;
    }

    private float GetGroundHeight()
    {
        Vector3 rayOrigin = transform.position + (transform.up * 1.0f);
        Ray ray = new Ray(rayOrigin, -transform.up);

        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, groundLayer))
        {
            return transform.parent.InverseTransformPoint(hit.point).y;
        }
        return -999f;
    }

    private void HandleGravityAndJump(float groundHeight, bool jumpInput, bool rollInput)
    {
        Vector3 localPos = transform.localPosition;

        if (localPos.y <= groundHeight + 0.05f && verticalVelocity <= 0)
        {
            IsGrounded = true;
            verticalVelocity = 0;
            localPos.y = groundHeight;

            // ジャンプ処理
            if (jumpInput && !IsRolling)
            {
                verticalVelocity = jumpForce;
                IsGrounded = false;
            }
        }
        else
        {
            IsGrounded = false;
            verticalVelocity -= gravity * Time.deltaTime;
        }

        localPos.y += verticalVelocity * Time.deltaTime;
        transform.localPosition = localPos;
    }

    private void HandleRolling(bool rollInput)
    {
        if (col == null) return;

        if (rollInput && IsGrounded)
        {
            IsRolling = true;
            col.height = currentRollHeight;
            col.center = new Vector3(col.center.x, currentRollCenterY, col.center.z);
        }
        else
        {
            IsRolling = false;
            col.height = defaultHeight;
            col.center = new Vector3(col.center.x, defaultCenterY, col.center.z);
        }
    }
}