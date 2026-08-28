using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    public GameObject[] weapons;
    public AudioSource audioSource;
    public AudioClip switchSound;
    private Shoot currentWeaponShoot;
    public AmmoUI ammoUI;


    void Start()
    {
        SelectWeapon(-1);
    }

    public void OnSelectWeapon1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SelectWeapon(-1); 
        }
    }

    public void OnSelectWeapon2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SelectWeapon(0); 
        }
    }
    public void OnSelectWeapon3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SelectWeapon(1);
        }
    }
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (currentWeaponShoot != null)
        {
            currentWeaponShoot.OnShoot(context);
        }
    }
    private void SelectWeapon(int index)
    {
      
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == index);
            
        }
        currentWeaponShoot = (index >= 0 && index < weapons.Length && weapons[index] != null)
            ? weapons[index].GetComponent<Shoot>()
            : null;

        if (ammoUI != null)
        {
            ammoUI.SetWeapon(currentWeaponShoot);
        }
        PlaySwitchSound();
    }
    public void OnReload(InputAction.CallbackContext context)
    {
        if (currentWeaponShoot != null)
        {
            currentWeaponShoot.OnReload(context);
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
