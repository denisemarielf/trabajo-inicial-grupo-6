using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
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

        if (collision.gameObject.CompareTag("Player"))
        {
            // Solo el servidor tiene autoridad para eliminar un objeto de red
            if (IsServer)
            {
                NetworkObject netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(); // Despawn ya destruye el objeto para todos los clientes
                }
            }
        }
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }

    }
}