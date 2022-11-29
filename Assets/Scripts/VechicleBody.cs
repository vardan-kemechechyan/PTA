using System.Collections;
using UnityEngine;

public class VechicleBody : MonoBehaviour
{
    [SerializeField] WheelVehicle vechicle;
    [SerializeField] AudioSource audioSource;
    [SerializeField] Rigidbody rb;

    private void OnCollisionEnter(Collision collision)
    {
        if (GameManager.GetInstance().level.IsOnCollisionGameOverEnabled && collision.relativeVelocity.magnitude > 10.0f)
        {
            if (collision.gameObject.TryGetComponent(out ObjectMaterial objectMaterial))
            {
                audioSource.clip = GameManager.GetInstance().objectCollisionSounds[(int)objectMaterial.type];
                if (audioSource.clip) audioSource.Play();
            }

            if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Ghost"))
                vechicle.Crash();
        }
        else 
        {
            vechicle.NonCrashCollision();
        }
    }
}