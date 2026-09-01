using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

public class Shoot : NetworkBehaviour
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
        if (audioSource == null)
        {
            Debug.LogWarning(gameObject.name + " no tiene un componente AudioSource asignado.");
        }
    }

    void Awake()
    {
        currentAmmo = magazineSize;
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!IsOwner) return; // Solo el dueño puede disparar su propia arma

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

            // Efectos locales, instantaneos para quien dispara
            if (audioSource != null && shootSound != null)
                audioSource.PlayOneShot(shootSound);

            if (muzzleFlash != null)
            {
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                muzzleFlash.Play();
            }

            if (weaponSway != null)
                weaponSway.AddRecoil();

            // La bala real la crea el servidor
            ShootServerRpc(spawnPoint.position, spawnPoint.rotation);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Quaternion rotation)
    {
        GameObject newBullet = Instantiate(bullet, position, rotation);

        NetworkObject netObj = newBullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(rotation * Vector3.forward * shootForce);
        }

        Destroy(newBullet, 3);
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
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

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
}