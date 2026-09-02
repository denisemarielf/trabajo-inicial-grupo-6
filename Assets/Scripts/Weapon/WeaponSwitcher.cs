using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class WeaponSwitcher : NetworkBehaviour
{
    public GameObject[] weapons;
    public AudioSource audioSource;
    public AudioClip switchSound;
    private Shoot currentWeaponShoot;
    public AmmoUI ammoUI;

    private NetworkVariable<int> networkWeaponIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        // Aseguramos que TODOS los GameObjects de arma esten activos siempre,
        // sin importar en que estado haya quedado guardado el prefab.
        foreach (var weapon in weapons)
        {
            if (weapon != null) weapon.SetActive(true);
        }

        networkWeaponIndex.OnValueChanged += OnWeaponIndexChanged;

        if (IsOwner)
        {
            StartCoroutine(BuscarAmmoUI());
            SelectWeapon(0);
        }
        else
        {
            // Aplicamos el valor actual, y ademas reforzamos con un frame de gracia
            // por si el valor todavia no termino de sincronizarse al momento del spawn.
            UpdateWeaponVisuals(networkWeaponIndex.Value);
            StartCoroutine(ReforzarVisualTrasSpawn());
        }
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
    }

    public override void OnNetworkDespawn()
    {
        networkWeaponIndex.OnValueChanged -= OnWeaponIndexChanged;
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
        if (context.performed) SelectWeapon(-1);
    }

    public void OnSelectWeapon2(InputAction.CallbackContext context)
    {
        if (context.performed) SelectWeapon(0);
    }

    public void OnSelectWeapon3(InputAction.CallbackContext context)
    {
        if (context.performed) SelectWeapon(1);
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (currentWeaponShoot != null)
        {
            currentWeaponShoot.OnShoot(context);
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (currentWeaponShoot != null)
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

    private void PlaySwitchSound()
    {
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }
}