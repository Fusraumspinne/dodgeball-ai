using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics.Internal;
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
        if(initialized)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            float angleToTarget = Vector3.SignedAngle(transform.forward, (target.position - transform.position).normalized, Vector3.up);
            angleToTarget /= 180.0f;

            float[] vision = PerformRaycastVision();

            float[] inputs = new float[11];
            inputs[0] = angleToTarget;
            inputs[1] = Mathf.Clamp01(distance / 10.0f);
            for (int i = 0; i < vision.Length; i++)
            {
                if (i + 2 >= 9)
                {
                    break;
                }

                inputs[i + 2] = vision[i];
            }

            Vector2 dangerDir = CheckIncomingBall();
            inputs[9] = dangerDir.x;
            inputs[10] = dangerDir.y;

            float[] output = net.FeedForward(inputs);

            moveInput = Mathf.Clamp(output[0], 0f, 1f);
            turnInput = Mathf.Clamp(output[1], -1f, 1f);

            float distanceFitness = Mathf.Clamp((distance <= 20.0f) ? Mathf.Exp(-distance / 10.0f) : -Mathf.Clamp01((distance - 20.0f) / 10.0f), -2f, 1f);
            //float ballPenalty = Mathf.Clamp(dangerDir.magnitude, 0f, 1f);
            //float dodgeFitness = 1f - Mathf.Clamp01(Mathf.Abs(inputs[9] * moveInput) + Mathf.Abs(inputs[10] * moveInput));

            net.AddFitness(1f - Mathf.Abs(inputs[0]) + distanceFitness);

            UpdateRotation();
            UpdateMove();
        }
    }

    private Vector2 CheckIncomingBall()
    {
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        Vector2 dangerDirection = Vector2.zero;

        foreach (GameObject ball in balls)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) continue;

            Vector3 ballPos = ball.transform.position;
            Vector3 ballVelocity = rb.velocity;
            Vector3 agentPos = transform.position;

            float timeToImpact = Mathf.Abs((ballPos.z - agentPos.z) / (ballVelocity.z + 0.0001f));
            Vector3 futureBallPos = ballPos + ballVelocity * timeToImpact;
            float distanceToAgent = Vector3.Distance(futureBallPos, agentPos);

            if (distanceToAgent < 15f)
            {
                Vector3 dir = (futureBallPos - agentPos).normalized;
                dangerDirection += new Vector2(dir.x, dir.z) * (1f - (distanceToAgent / 15f));
            }
        }

        return dangerDirection;
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
        int layerMask = ~LayerMask.GetMask("Agent", "Player", "Ball");

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