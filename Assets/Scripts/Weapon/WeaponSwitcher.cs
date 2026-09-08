using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class WeaponSwitcher : NetworkBehaviour
{
    public GameObject[] weapons;
    public AudioSource audioSource;
    public AudioClip switchSound;
    private Shoot currentWeaponShoot;
    public AmmoUI ammoUI;
    private PlayerHealth playerHealth;
    private bool wasDead = false;

    private NetworkVariable<int> networkWeaponIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        // Aseguramos que TODOS los GameObjects de arma esten activos siempre,
        // sin importar en que estado haya quedado guardado el prefab.
        playerHealth = GetComponentInParent<PlayerHealth>();
        foreach (var weapon in weapons)
        {
            if (weapon != null) weapon.SetActive(true);
        }

        // NUEVO: asignamos la layer según sea mi arma o la de otro jugador.
        int targetLayer = IsOwner
            ? LayerMask.NameToLayer("Weapon")
            : LayerMask.NameToLayer("Default");

        foreach (var weapon in weapons)
        {
            if (weapon != null) SetLayerRecursively(weapon, targetLayer);
        }

        networkWeaponIndex.OnValueChanged += OnWeaponIndexChanged;
        if (IsOwner)
        {
            StartCoroutine(BuscarAmmoUI());
            SelectWeapon(0);

            // El jugador persiste entre reinicios de partida, pero la escena
            // (y con ella el AmmoUI viejo) se destruye y se recrea. Cada vez
            // que carga una escena nueva, volvemos a buscar el AmmoUI actual.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            UpdateWeaponVisuals(networkWeaponIndex.Value);
            StartCoroutine(ReforzarVisualTrasSpawn());
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // La escena vieja (y su AmmoUI) ya no existen; la referencia quedó
        // apuntando a un objeto destruido. Buscamos el AmmoUI de la escena nueva.
        ammoUI = null;
        StartCoroutine(BuscarAmmoUI());
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (playerHealth == null) return;

        bool isDead = playerHealth.IsDead();

        // Apenas detecto que acabo de morir, enfundo el arma automáticamente
        // (mismo comportamiento que OnSelectWeapon1 -> SelectWeapon(-1)).
        if (isDead && !wasDead)
        {
            SelectWeapon(-1);
        }

        wasDead = isDead;
    }
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private System.Collections.IEnumerator ReforzarVisualTrasSpawn()
    {
        yield return null; // espera un frame
        yield return null; // y otro mas, para dar margen a la sincronizacion inicial
        UpdateWeaponVisuals(networkWeaponIndex.Value);
    }

    private System.Collections.IEnumerator BuscarAmmoUI()
    {
        while (ammoUI == null)
        {
            ammoUI = FindAnyObjectByType<AmmoUI>();
            if (ammoUI == null) yield return null;
        }

        // Apenas lo encontramos, lo inicializamos con el arma actual
        // (antes esto quedaba en blanco hasta el próximo cambio de arma).
        ammoUI.SetWeapon(currentWeaponShoot);
    }

    public override void OnNetworkDespawn()
    {
        networkWeaponIndex.OnValueChanged -= OnWeaponIndexChanged;

        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnWeaponIndexChanged(int oldIndex, int newIndex)
    {
        UpdateWeaponVisuals(newIndex);
    }

    private void UpdateWeaponVisuals(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null) continue;

            bool isSelected = (i == index);
            // "true" incluye componentes en hijos inactivos, por las dudas
            foreach (var renderer in weapons[i].GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = isSelected;
            }
        }
    }

    public void OnSelectWeapon1(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead()) return;
        if (IsOwner && !EscapeMenu.IsOpen && context.performed)
            SelectWeapon(-1);
    }

    public void OnSelectWeapon2(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead()) return;
        if (IsOwner && !EscapeMenu.IsOpen && context.performed)
            SelectWeapon(0);
    }

    public void OnSelectWeapon3(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead()) return;
        if (IsOwner && !EscapeMenu.IsOpen && context.performed)
            SelectWeapon(1);
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (IsOwner && currentWeaponShoot != null)
        {
            currentWeaponShoot.OnShoot(context);
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (IsOwner && currentWeaponShoot != null)
        {
            currentWeaponShoot.OnReload(context);
        }
    }

    private void SelectWeapon(int index)
    {
        UpdateWeaponVisuals(index);

        currentWeaponShoot = (index >= 0 && index < weapons.Length && weapons[index] != null)
            ? weapons[index].GetComponent<Shoot>()
            : null;

        if (ammoUI != null)
        {
            ammoUI.SetWeapon(currentWeaponShoot);
        }
        PlaySwitchSound();

        if (IsOwner)
        {
            networkWeaponIndex.Value = index;
        }
    }
    public void GrantAmmo(int amount)
    {
        if (!IsServer) return; // esto lo llama el PowerUp, que corre en el servidor
        GrantAmmoClientRpc(amount);
    }

    [ClientRpc]
    private void GrantAmmoClientRpc(int amount)
    {
        if (!IsOwner) return; // solo le importa al dueño de esta arma

        foreach (var weapon in weapons)
        {
            if (weapon == null) continue;

            Shoot shootComponent = weapon.GetComponent<Shoot>();
            shootComponent?.AddReserveAmmo(amount);
        }
    }
    public void ApplyDamageBoost(float multiplier, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(DamageBoostRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator DamageBoostRoutine(float multiplier, float duration)
    {
        Shoot boostedShoot = GetServerCurrentWeaponShoot();
        if (boostedShoot == null) yield break;

        int originalDamage = boostedShoot.damageAmount;
        boostedShoot.damageAmount = Mathf.RoundToInt(originalDamage * multiplier);

        yield return new WaitForSeconds(duration);

        boostedShoot.damageAmount = originalDamage;
    }

    private Shoot GetServerCurrentWeaponShoot()
    {
        int index = networkWeaponIndex.Value;
        if (index < 0 || index >= weapons.Length || weapons[index] == null) return null;
        return weapons[index].GetComponent<Shoot>();
    }

    private void PlaySwitchSound()
    {
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }
}