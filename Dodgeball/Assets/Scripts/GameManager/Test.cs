using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField][Range(0.0f, 0.5f)] float mouseSmoothTime = 0.03f;
    [SerializeField] float mouseSensitivity = 3.5f;
    [SerializeField] float speed = 6.0f;
    [SerializeField][Range(0.0f, 0.5f)] float moveSmoothTime = 0.3f;
    [SerializeField] float gravity = -30f;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask ground;

    public float jumpHeight = 6f;
    float velocityY;
    bool isGrounded;

    float cameraCap;
    Vector2 currentMouseDelta;
    Vector2 currentMouseDeltaVelocity;

    CharacterController controller;
    Vector2 currentDir;
    Vector2 currentDirVelocity;

    public float moveInput; 
    public float turnInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        UpdateRotation();
        UpdateMove();
    }

    void UpdateRotation()
    {
        float targetTurn = turnInput * mouseSensitivity;

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, new Vector2(targetTurn, 0), ref currentMouseDeltaVelocity, mouseSmoothTime);

        cameraCap -= currentMouseDelta.y;
        cameraCap = Mathf.Clamp(cameraCap, -90.0f, 90.0f);

        transform.Rotate(Vector3.up * currentMouseDelta.x);
    }

    void UpdateMove()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, ground);

        Vector2 targetDir = new Vector2(0, moveInput);
        targetDir.Normalize();

        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, moveSmoothTime);

        velocityY += gravity * Time.deltaTime;

        Vector3 velocity = (transform.forward * currentDir.y) * speed + Vector3.up * velocityY;

        controller.Move(velocity * Time.deltaTime);

        /*
        if (isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }

        if (isGrounded && Mathf.Abs(moveInput) > 0.1f)
        {
            velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        */
    }
}
