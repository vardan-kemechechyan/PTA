using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] bool crash;

    Vector3 startPosition;
    Quaternion startRotation;

    [SerializeField] Rigidbody rb;

    public void Setup()
    {
        if (rb) 
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;

            startPosition = transform.position;
            startRotation = transform.rotation;
        }
    }

    public void ResetObstacle()
    {
        if (rb) 
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;

            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(rb)
            rb.constraints = RigidbodyConstraints.None;

        if (crash && collision.collider.CompareTag("Player") && GameManager.GetInstance().level.IsOnCollisionGameOverEnabled)
        {
            GameManager.GetInstance().Crash();
		}
    }
}