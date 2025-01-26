using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
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

    private bool initialized = false;
    private Transform target;

    private NeuralNetwork net;
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        if (initialized)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            // Calculate normalized inputs
            float angleToTarget = Vector3.SignedAngle(transform.forward, (target.position - transform.position).normalized, Vector3.up) / 180.0f;
            float normalizedDistance = Mathf.Clamp01(distance / 10.0f); // Normalize distance to range [0, 1]

            // Raycasting for 3x3 vision grid
            float[] vision = PerformRaycastVision();

            // Combine inputs into neural network input array
            float[] inputs = new float[9];
            inputs[0] = angleToTarget;
            inputs[1] = normalizedDistance;
            for (int i = 0; i < vision.Length; i++)
            {
                inputs[i + 2] = vision[i];
            }

            // Get outputs from neural network
            float[] output = net.FeedForward(inputs);
            moveInput = output[0]; // Move forward/backward
            turnInput = output[1]; // Rotate left/right

            // Update fitness based on proximity to target
            net.AddFitness(1.0f / (distance + 1.0f)); // Closer = higher fitness

            // Apply movement and rotation
            UpdateRotation();
            UpdateMove();
        }
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
    }

    private float[] PerformRaycastVision()
    {
        float[] vision = new float[7];
        float[] angles = { -90, -60, -30, 0, 30, 60, 90 };

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angles[i], 0) * transform.forward;
            vision[i] = CastRay(direction);
        }

        return vision;
    }

    private float CastRay(Vector3 direction)
    {
        RaycastHit hit;
        float maxDistance = 10f;
        int layerMask = ~LayerMask.GetMask("Agent");

        if (Physics.Raycast(transform.position, direction, out hit, maxDistance, layerMask))
        {
            Debug.DrawRay(transform.position, direction * hit.distance, Color.red); 
            return hit.distance; 
        }

        Debug.DrawRay(transform.position, direction * maxDistance, Color.green); 
        return maxDistance;
    }

    public void Init(NeuralNetwork net, Transform target)
    {
        this.target = target;
        this.net = net;
        initialized = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}