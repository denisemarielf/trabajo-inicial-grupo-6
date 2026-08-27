using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody rb;
    private bool hasHit = false;
    public ParticleSystem sparksImpact;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; 
        hasHit = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
  
        rb.isKinematic = true;
        ContactPoint contact = collision.GetContact(0);
        if (sparksImpact != null)
        {
            ParticleSystem sparks = Instantiate(
                sparksImpact,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            sparks.Play();
            Destroy(sparks.gameObject, sparks.main.duration + sparks.main.startLifetime.constantMax);
        }



    }
}
