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

    // Se sincroniza sola: cualquiera puede LEERLA, pero solo el dueño puede ESCRIBIRLA
    private NetworkVariable<int> networkWeaponIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        networkWeaponIndex.OnValueChanged += OnWeaponIndexChanged;

        if (IsOwner)
        {
            StartCoroutine(BuscarAmmoUI());
            SelectWeapon(0);
        }
        else
        {
            UpdateWeaponVisuals(networkWeaponIndex.Value);
        }
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
            bool isSelected = (i == index);
            foreach (var renderer in weapons[i].GetComponentsInChildren<Renderer>())
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
        UpdateWeaponVisuals(index); // Respuesta visual inmediata para el dueño

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
            networkWeaponIndex.Value = index; // Avisa a TODOS los demas clientes
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