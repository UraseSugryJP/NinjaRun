using UnityEngine; 
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CapsuleCollider))] [RequireComponent(typeof(AudioSource))] public class PlayerMovement : MonoBehaviour { [Header("3レーン設定")] [SerializeField] private float laneWidth = 3.0f; [SerializeField] private float laneChangeSpeed = 20f; 

private int currentLane = 0; 
private float targetX = 0f; 

[Header("ジャンプ・重力設定")]
[SerializeField] private float jumpForce = 8f; 
[SerializeField] private float gravity = 30f; 

[Header("効果音(SE)設定")]
[SerializeField] private AudioClip jumpClip; 
[SerializeField] private AudioClip landClip; 
[SerializeField] private AudioClip moveClip; 
[SerializeField] private AudioClip rollClip; 

[Header("BGM/足音設定")]
[SerializeField] private AudioClip footstepClip; 
[SerializeField] private AudioSource bgmSource; 
[Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.3f; 

[Header("接地判定設定")]
[SerializeField] private LayerMask groundLayer; 
[SerializeField] private float rayLength = 10f; 

[Header("ローリング設定")]
private float rollHeight = 0.5f; 
private float rollCenterY = 0.25f; 
private float rollDuration = 1.0f; 

private CapsuleCollider col; 
private AudioSource myAudio; 
private float verticalVelocity; 
private float defaultHeight; 
private float defaultCenterY; 
private float rollTimer = 0f; 

public bool IsGrounded { get; private set; } 
private bool wasGrounded; 
public bool IsRolling { get; private set; } 

void Start()
{
    col = GetComponent<CapsuleCollider>(); 
    myAudio = GetComponent<AudioSource>(); 

    if (col != null)
    {
        defaultHeight = col.height; 
        defaultCenterY = col.center.y; 
    }

    if (footstepClip != null)
    {
        myAudio.clip = footstepClip; 
        myAudio.loop = true; 
        myAudio.Play(); 
    }

    if (bgmSource != null)
    {
        bgmSource.loop = true; 
        bgmSource.volume = bgmVolume; 
        bgmSource.Play(); 
    }
}

void Update()
{
    HandleInput(); 
    float nextX = MoveToLane(); 
    float groundY = GetGroundHeight(); 
    float nextY = HandleGravityAndJump(groundY); 
    HandleRolling(); 

    transform.localPosition = new Vector3(nextX, nextY, 0f); 
    UpdateFootsteps(); 
}

private void UpdateFootsteps()
{
    if (IsGrounded && !IsRolling && verticalVelocity <= 0)
    {
        if (!myAudio.isPlaying) myAudio.UnPause(); 
    }
    else
    {
        if (myAudio.isPlaying) myAudio.Pause(); 
    }
}

private void HandleInput()
{
    bool rightInput = false;
    if (Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)) rightInput = true; 
    if (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame) rightInput = true; 

    if (rightInput && currentLane < 1)
    {
        currentLane++; 
        if (moveClip != null) myAudio.PlayOneShot(moveClip); 
    }

    bool leftInput = false;
    if (Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)) leftInput = true; 
    if (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame) leftInput = true; 

    if (leftInput && currentLane > -1)
    {
        currentLane--; 
        if (moveClip != null) myAudio.PlayOneShot(moveClip); 
    }

    targetX = currentLane * laneWidth; 
}

private bool CheckJumpInput()
{
    if (Keyboard.current != null)
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            return true; 
    }
    if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true; 
    return false;
}

private bool CheckRollInput()
{
    if (Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)) return true; 
    if (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame)) return true; 
    return false;
}

private float MoveToLane()
{
    return Mathf.Lerp(transform.localPosition.x, targetX, Time.deltaTime * laneChangeSpeed); 
}

private float GetGroundHeight()
{
    Vector3 rayOrigin = transform.position + (transform.up * 1.0f); 
    Ray ray = new Ray(rayOrigin, -transform.up); 
    int layerMask = groundLayer | LayerMask.GetMask("Obstacle"); 
    if (Physics.Raycast(ray, out RaycastHit hit, rayLength, layerMask))
    {
        return transform.parent.InverseTransformPoint(hit.point).y; 
    }
    return -999f; 
}

private float HandleGravityAndJump(float groundHeight)
{
    Vector3 localPos = transform.localPosition; 
    float nextY = localPos.y; 
    wasGrounded = IsGrounded; 

    if (localPos.y <= groundHeight + 0.05f && verticalVelocity <= 0)
    {
        IsGrounded = true; 
        verticalVelocity = 0; 
        nextY = groundHeight; 

        if (!wasGrounded && landClip != null) myAudio.PlayOneShot(landClip); 

        if (CheckJumpInput() && !IsRolling)
        {
            verticalVelocity = jumpForce; 
            IsGrounded = false; 
            if (jumpClip != null) myAudio.PlayOneShot(jumpClip); 
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

    if (IsRolling)
    {
        rollTimer -= Time.deltaTime; 
        if (rollTimer <= 0f)
        {
            IsRolling = false; 
            col.height = defaultHeight; 
            col.center = new Vector3(col.center.x, defaultCenterY, col.center.z); 
        }
        return;
    }

    if (CheckRollInput() && IsGrounded)
    {
        IsRolling = true; 
        rollTimer = rollDuration; 
        col.height = rollHeight; 
        col.center = new Vector3(col.center.x, rollCenterY, col.center.z); 
        if (rollClip != null) myAudio.PlayOneShot(rollClip); 
    }
}
}