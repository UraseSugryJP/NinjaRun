using UnityEngine;
using UnityEngine.InputSystem; // ★これが新しい入力システムの証

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("3レーン設定")]
    [SerializeField] private float laneWidth = 3.0f; // 1レーンの幅
    [SerializeField] private float laneChangeSpeed = 20f; 
    
    private int currentLane = 0; // -1:左, 0:中, 1:右
    private float targetX = 0f;

    [Header("ジャンプ・重力設定")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30f;

    [Header("接地判定設定")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 10f;

    [Header("ローリング設定")]
    private float rollHeight = 0.5f;
    private float rollCenterY = 0.25f;

    // --- 内部変数 ---
    private CapsuleCollider col;
    private float verticalVelocity;
    private float defaultHeight;
    private float defaultCenterY;
    
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
        // 1. 入力の判定（ここが新しくなった！）
        HandleInput();

        // 2. 移動処理（ここは変わらず）
        float nextX = MoveToLane();
        float groundY = GetGroundHeight();
        float nextY = HandleGravityAndJump(groundY);
        HandleRolling();

        transform.localPosition = new Vector3(nextX, nextY, 0f);
    }

    // --- ▼▼▼ 新しい入力システムでの判定ロジック ▼▼▼ ---
    private void HandleInput()
    {
        // --- 右への移動 (Right Arrow, Dキー, ゲームパッド十字キー右) ---
        bool rightInput = false;
        
        // キーボード
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                rightInput = true;
        }
        // ゲームパッド
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.wasPressedThisFrame)
                rightInput = true;
        }

        if (rightInput && currentLane < 1) currentLane++;


        // --- 左への移動 (Left Arrow, Aキー, ゲームパッド十字キー左) ---
        bool leftInput = false;

        // キーボード
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                leftInput = true;
        }
        // ゲームパッド
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame)
                leftInput = true;
        }

        if (leftInput && currentLane > -1) currentLane--;


        // 目標座標の更新
        targetX = currentLane * laneWidth;
    }

    // --- ジャンプ入力のチェック関数 ---
    private bool CheckJumpInput()
    {
        // キーボード: Space, 上矢印, W
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || 
                Keyboard.current.upArrowKey.wasPressedThisFrame || 
                Keyboard.current.wKey.wasPressedThisFrame)
                return true;
        }

        // ゲームパッド: 南ボタン(Aボタン/×ボタン)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    // --- ローリング入力のチェック関数 ---
    private bool CheckRollInput()
    {
        // キーボード: 下矢印, S
        if (Keyboard.current != null)
        {
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                return true;
        }

        // ゲームパッド: 東ボタン(Bボタン/○ボタン) または 十字キー下
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.isPressed || Gamepad.current.dpad.down.isPressed)
                return true;
        }

        return false;
    }
    // --- ▲▲▲ 新しい入力システムここまで ▲▲▲ ---


    // --- 以下、物理・移動ロジック（変更なし） ---

    private float MoveToLane()
    {
        Vector3 localPos = transform.localPosition;
        return Mathf.Lerp(localPos.x, targetX, Time.deltaTime * laneChangeSpeed);
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

    private float HandleGravityAndJump(float groundHeight)
    {
        Vector3 localPos = transform.localPosition;
        float nextY = localPos.y;

        if (localPos.y <= groundHeight + 0.05f && verticalVelocity <= 0)
        {
            IsGrounded = true;
            verticalVelocity = 0;
            nextY = groundHeight;

            // ジャンプ判定に関数を使用
            if (CheckJumpInput() && !IsRolling)
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

        nextY += verticalVelocity * Time.deltaTime;
        return nextY;
    }

    private void HandleRolling()
    {
        if (col == null) return;

        // ローリング判定に関数を使用
        if (CheckRollInput() && IsGrounded)
        {
            IsRolling = true;
            col.height = rollHeight;
            col.center = new Vector3(col.center.x, rollCenterY, col.center.z);
        }
        else
        {
            IsRolling = false;
            col.height = defaultHeight;
            col.center = new Vector3(col.center.x, defaultCenterY, col.center.z);
        }
    }
}