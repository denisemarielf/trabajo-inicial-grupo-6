using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Shoot : MonoBehaviour
{
    public Transform spawnPoint;

    public GameObject bullet;
    public float shootForce = 1500f;
    public float shootRate = 0.5f;


    public int magazineSize = 12;
    public int reserveAmmo = 60;
    public float reloadTime = 1.5f;
    private int currentAmmo;
    private bool isReloading = false;

    private float shootRateTime = 0;
    private AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    public ParticleSystem muzzleFlash;
    public WeaponSway weaponSway;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        currentAmmo = magazineSize; 
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed && Time.time >= shootRateTime)
        {
            shootRateTime = Time.time + shootRate;

            if (currentAmmo <= 0)
            {
                PlayEmptySound();
                TryReload();
                return;
            }
            currentAmmo--;

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
    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed) TryReload();
    }
    private void TryReload()
    {
        if (isReloading) return;
        if (currentAmmo >= magazineSize) return; 
        if (reserveAmmo <= 0) return; 

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlayReloadSound();

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
    }

    private void PlayReloadSound()
    {
        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);
    }

    private void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }

    // UI de munición 
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
}