using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MovementTarget : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    void Start()
    {
        SetNewTarget();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewTarget();
        }
    }

    void SetNewTarget()
    {
        Vector3 randomPosition = GetRandomNavMeshPosition();
        agent.SetDestination(randomPosition);
    }

    Vector3 GetRandomNavMeshPosition()
    {
        NavMeshHit hit;
        Vector3 potentialPosition;

        do
        {
            float randomX = Random.Range(-80, 80);
            float randomZ = Random.Range(-40f, 40f);
            potentialPosition = new Vector3(randomX, transform.position.y, randomZ);
        }
        while (!NavMesh.SamplePosition(potentialPosition, out hit, 10f, NavMesh.AllAreas));

        return hit.position;
    }
}
