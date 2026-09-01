

using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

public class WeaponSwitcherTests : InputTestFixture
{
    private Mouse mouse;
    private Keyboard keyboard;

    private List<GameObject> objetosDePrueba =
        new List<GameObject>();

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        mouse = InputSystem.AddDevice<Mouse>();
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [TearDown]
    public void Cleanup()
    {
        // Eliminar balas creadas durante el test
        GameObject[] objetos =
            GameObject.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include
            );

        foreach (GameObject objeto in objetos)
        {
            if (objeto.name == "Bullet(Clone)")
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // Eliminar objetos creados para el test
        foreach (GameObject objeto in objetosDePrueba)
        {
            if (objeto != null)
            {
                Object.DestroyImmediate(objeto);
            }
        }

        objetosDePrueba.Clear();

        if (mouse != null)
        {
            InputSystem.RemoveDevice(mouse);
            mouse = null;
        }

        if (keyboard != null)
        {
            InputSystem.RemoveDevice(keyboard);
            keyboard = null;
        }
    }

    private InputAction CrearAccionDeDisparo(
        WeaponSwitcher weaponSwitcher)
    {
        InputAction disparo = new InputAction(
            "Disparo",
            InputActionType.Button
        );

        disparo.AddBinding("<Mouse>/leftButton");
        disparo.performed += weaponSwitcher.OnShoot;
        disparo.Enable();

        return disparo;
    }

    private InputAction CrearAccionArma2(
        WeaponSwitcher weaponSwitcher)
    {
        InputAction seleccionarArma2 = new InputAction(
            "SeleccionarArma2",
            InputActionType.Button
        );

        seleccionarArma2.AddBinding("<Keyboard>/2");
        seleccionarArma2.performed +=
            weaponSwitcher.OnSelectWeapon2;

        seleccionarArma2.Enable();

        return seleccionarArma2;
    }

    private InputAction CrearAccionArma3(
        WeaponSwitcher weaponSwitcher)
    {
        InputAction seleccionarArma3 = new InputAction(
            "SeleccionarArma3",
            InputActionType.Button
        );

        seleccionarArma3.AddBinding("<Keyboard>/3");
        seleccionarArma3.performed +=
            weaponSwitcher.OnSelectWeapon3;

        seleccionarArma3.Enable();

        return seleccionarArma3;
    }

    [UnityTest]
    public IEnumerator NingunArmaEquipada_AlDisparar_NoSeDispara()
    {
        // Arrange
        GameObject switcherObject =
            new GameObject("WeaponSwitcher");

        objetosDePrueba.Add(switcherObject);

        WeaponSwitcher weaponSwitcher =
            switcherObject.AddComponent<WeaponSwitcher>();

        GameObject arma1 = new GameObject("Arma1");
        GameObject arma2 = new GameObject("Arma2");

        objetosDePrueba.Add(arma1);
        objetosDePrueba.Add(arma2);

        weaponSwitcher.weapons = new GameObject[]
        {
            arma1,
            arma2
        };

        yield return null;

        InputAction disparo =
            CrearAccionDeDisparo(weaponSwitcher);

        // Act - Clic izquierdo
        InputSystem.QueueStateEvent(
            mouse,
            new MouseState { buttons = 1 }
        );

        InputSystem.Update();

        // Assert
        Assert.IsNull(
            GameObject.Find("Bullet(Clone)")
        );

        // Soltamos el botón
        InputSystem.QueueStateEvent(
            mouse,
            new MouseState { buttons = 0 }
        );

        InputSystem.Update();

        // Cleanup
        disparo.performed -= weaponSwitcher.OnShoot;
        disparo.Disable();
        disparo.Dispose();
    }

    [UnityTest]
    public IEnumerator Tecla2_SeEquipaLaPrimerArma()
    {
        // Arrange
        GameObject switcherObject =
            new GameObject("WeaponSwitcher");

        objetosDePrueba.Add(switcherObject);

        WeaponSwitcher weaponSwitcher =
            switcherObject.AddComponent<WeaponSwitcher>();

        GameObject arma1 = new GameObject("Arma1");
        GameObject arma2 = new GameObject("Arma2");

        objetosDePrueba.Add(arma1);
        objetosDePrueba.Add(arma2);

        weaponSwitcher.weapons = new GameObject[]
        {
            arma1,
            arma2
        };

        yield return null;

        InputAction seleccionarArma =
            CrearAccionArma2(weaponSwitcher);

        // Act - Presionamos 2
        InputSystem.QueueStateEvent(
            keyboard,
            new KeyboardState(Key.Digit2)
        );

        InputSystem.Update();

        // Assert
        Assert.IsTrue(arma1.activeSelf);
        Assert.IsFalse(arma2.activeSelf);

        // Soltamos 2
        InputSystem.QueueStateEvent(
            keyboard,
            new KeyboardState()
        );

        InputSystem.Update();

        // Cleanup
        seleccionarArma.performed -=
            weaponSwitcher.OnSelectWeapon2;

        seleccionarArma.Disable();
        seleccionarArma.Dispose();
    }

    [UnityTest]
    public IEnumerator Tecla3_SeEquipaLaSegundaArma()
    {
        // Arrange
        GameObject switcherObject =
            new GameObject("WeaponSwitcher");

        objetosDePrueba.Add(switcherObject);

        WeaponSwitcher weaponSwitcher =
            switcherObject.AddComponent<WeaponSwitcher>();

        GameObject arma1 = new GameObject("Arma1");
        GameObject arma2 = new GameObject("Arma2");

        objetosDePrueba.Add(arma1);
        objetosDePrueba.Add(arma2);

        weaponSwitcher.weapons = new GameObject[]
        {
            arma1,
            arma2
        };

        yield return null;

        InputAction seleccionarArma =
            CrearAccionArma3(weaponSwitcher);

        // Act - Presionamos 3
        InputSystem.QueueStateEvent(
            keyboard,
            new KeyboardState(Key.Digit3)
        );

        InputSystem.Update();

        // Assert
        Assert.IsFalse(arma1.activeSelf);
        Assert.IsTrue(arma2.activeSelf);

        // Soltamos 3
        InputSystem.QueueStateEvent(
            keyboard,
            new KeyboardState()
        );

        InputSystem.Update();

        // Cleanup
        seleccionarArma.performed -=
            weaponSwitcher.OnSelectWeapon3;

        seleccionarArma.Disable();
        seleccionarArma.Dispose();
    }

    
    [UnityTest]
    public IEnumerator CambiarDeArma_LaAnteriorSeDesactiva()
    {
    // Arrange
    GameObject switcherObject =
        new GameObject("WeaponSwitcher");

    objetosDePrueba.Add(switcherObject);

    WeaponSwitcher weaponSwitcher =
        switcherObject.AddComponent<WeaponSwitcher>();

    GameObject arma1 =
        new GameObject("Arma1");

    GameObject arma2 =
        new GameObject("Arma2");

    objetosDePrueba.Add(arma1);
    objetosDePrueba.Add(arma2);

    weaponSwitcher.weapons = new GameObject[]
    {
        arma1,
        arma2
    };

    yield return null;

    InputAction seleccionarArma2 =
        CrearAccionArma2(weaponSwitcher);

    InputAction seleccionarArma3 =
        CrearAccionArma3(weaponSwitcher);

    // Act - Equipamos la primera arma con 2
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState(Key.Digit2)
    );

    InputSystem.Update();

    // Comprobamos que la primera está equipada
    Assert.IsTrue(arma1.activeSelf);
    Assert.IsFalse(arma2.activeSelf);

    // Soltamos 2
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState()
    );

    InputSystem.Update();

    // Act - Cambiamos a la segunda arma con 3
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState(Key.Digit3)
    );

    InputSystem.Update();

    // Assert
    Assert.IsFalse(arma1.activeSelf);
    Assert.IsTrue(arma2.activeSelf);

    // Soltamos 3
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState()
    );

    InputSystem.Update();

    // Cleanup
    seleccionarArma2.performed -=
        weaponSwitcher.OnSelectWeapon2;

    seleccionarArma2.Disable();
    seleccionarArma2.Dispose();

    seleccionarArma3.performed -=
        weaponSwitcher.OnSelectWeapon3;

    seleccionarArma3.Disable();
    seleccionarArma3.Dispose();
}

}

