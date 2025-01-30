using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kanone : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float throwSpeed;

    private void Start()
    {
        StartCoroutine(ThrowBall());
    }

    IEnumerator ThrowBall()
    {
        while (true)
        { 
            if (ballPrefab != null && spawnPoint != null)
            {
                GameObject ball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);

                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = ball.AddComponent<Rigidbody>();
                }

                rb.velocity = spawnPoint.forward * throwSpeed;
            }
            else
            {
                Debug.LogWarning("BallPrefab oder SpawnPoint nicht zugewiesen!");
            }

            yield return new WaitForSeconds(Random.Range(1.5f,3f));
        }
    }
}
