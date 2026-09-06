using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    public enum PowerUpType { Health, Speed , Ammo, Damage }

    [Header("Config")]
    public PowerUpType type;
    public float amount = 25f;       // vida a curar, o multiplicador de velocidad
    public float duration = 8f;      // solo para efectos temporales como Speed
    public float respawnTime = 15f;
    public AudioClip pickupSound;

    private NetworkVariable<bool> isAvailable = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Renderer[] renderers;
    private Collider col;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();
        col.isTrigger = true; // igual que la bala: nunca empuja físicamente a nadie
    }

    public override void OnNetworkSpawn()
    {
        isAvailable.OnValueChanged += (oldVal, newVal) => UpdateVisuals(newVal);
        UpdateVisuals(isAvailable.Value);
    }

    private void UpdateVisuals(bool available)
    {
        foreach (var r in renderers) r.enabled = available;
        col.enabled = available;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!isAvailable.Value) return;
        if (!other.CompareTag("Player")) return;

        ApplyEffect(other.gameObject);
        PlayPickupSoundClientRpc();

        isAvailable.Value = false;
        StartCoroutine(RespawnRoutine());
    }

    [ClientRpc]
    private void PlayPickupSoundClientRpc()
    {
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    }
    private void ApplyEffect(GameObject player)
    {
        switch (type)
        {
            case PowerUpType.Health:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                health?.Heal(amount);
                break;

            case PowerUpType.Speed:
                PlayerMovementCC movement = player.GetComponent<PlayerMovementCC>();
                movement?.ApplySpeedBoost(amount, duration);
                break;
            case PowerUpType.Ammo:
                WeaponSwitcher weaponSwitcher = player.GetComponentInChildren<WeaponSwitcher>();
                weaponSwitcher?.GrantAmmo((int)amount);
                break;
            case PowerUpType.Damage:
                WeaponSwitcher damageSwitcher = player.GetComponentInChildren<WeaponSwitcher>();
                damageSwitcher?.ApplyDamageBoost(amount, duration);
                break;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        isAvailable.Value = true;
    }
}