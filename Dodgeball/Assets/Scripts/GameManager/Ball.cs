using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private bool training;

    [SerializeField] private Collider colliderBall;
    [SerializeField] private Rigidbody rb;

    private void Start()
    {
        if (training)
        {
            colliderBall.isTrigger = true;
            rb.useGravity = false;
        }

        StartCoroutine(DestroyBall());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            Destroy(other.gameObject);
            if (!training)
            {
                Destroy(gameObject);
            }
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    IEnumerator DestroyBall()
    {
        yield return new WaitForSeconds(10);
        Destroy(gameObject);
    }
}
