using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    public Transform spawnPoint;

    public GameObject bullet;
    public float shootForce = 1500f;
    public float shootRate = 0.5f;

    private float shootRateTime = 0;
    private AudioSource audioSource;
    public AudioClip shootSound;
    public ParticleSystem muzzleFlash;
    public WeaponSway weaponSway;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed && Time.time >= shootRateTime)
        {
            shootRateTime = Time.time + shootRate;


            GameObject newBullet = Instantiate(
                bullet,
                spawnPoint.position,
                spawnPoint.rotation
            );
            audioSource.PlayOneShot(shootSound);
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
            weaponSway.AddRecoil();

            Rigidbody rb = newBullet.GetComponent<Rigidbody>();

            Destroy(newBullet, 3);
            if (rb != null)
            {
                rb.AddForce(spawnPoint.forward * shootForce);
            }
        }
    }
}