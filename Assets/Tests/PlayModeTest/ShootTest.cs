

using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.InputSystem.Utilities;
using System.Collections;
using System.Collections.Generic;

public class ShootTests : InputTestFixture
{
    private List<GameObject> objetosDePrueba = new List<GameObject>();
    private Mouse mouse;
    private Keyboard keyboard;

    private Shoot CrearArmaDePrueba()
    {
        // Arma
        GameObject arma = new GameObject("Arma");
        objetosDePrueba.Add(arma);

        Shoot shoot = arma.AddComponent<Shoot>();

        // AudioSource
        arma.AddComponent<AudioSource>();

        // AudioClip
        shoot.shootSound = AudioClip.Create(
            "ShootSound",
            44100,
            1,
            44100,
            false
        );

        // SpawnPoint
        GameObject spawnObject = new GameObject("SpawnPoint");
        objetosDePrueba.Add(spawnObject);

        spawnObject.transform.position = Vector3.zero;
        spawnObject.transform.forward = Vector3.forward;

        shoot.spawnPoint = spawnObject.transform;

        // Prefab de bala
        GameObject balaPrefab = new GameObject("Bullet");
        objetosDePrueba.Add(balaPrefab);

        balaPrefab.AddComponent<Rigidbody>();

        shoot.bullet = balaPrefab;

        // Muzzle Flash
        GameObject muzzleObject = new GameObject("MuzzleFlash");
        objetosDePrueba.Add(muzzleObject);

        ParticleSystem muzzleFlash =
            muzzleObject.AddComponent<ParticleSystem>();

        shoot.muzzleFlash = muzzleFlash;

        // WeaponSway
        GameObject swayObject = new GameObject("WeaponSway");
        objetosDePrueba.Add(swayObject);

        WeaponSway weaponSway =
            swayObject.AddComponent<WeaponSway>();

        shoot.weaponSway = weaponSway;

        return shoot;
    }

    private InputAction CrearAccionDeDisparo(Shoot shoot)
    {
        InputAction disparo = new InputAction(
            "Disparo",
            InputActionType.Button
        );

        disparo.AddBinding("<Mouse>/leftButton");
        disparo.performed += shoot.OnShoot;
        disparo.Enable();

        return disparo;
    }

    
    private IEnumerator VaciarCargador(
        Shoot shoot,
        InputAction disparo
    )
    {
        while (shoot.CurrentAmmo > 0)
        {
            // Clic izquierdo
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { buttons = 1 }
            );

            InputSystem.Update();

            // Soltar clic
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { buttons = 0 }
            );

            InputSystem.Update();

            // Esperamos el tiempo mínimo entre disparos
            yield return new WaitForSeconds(shoot.shootRate);
        }
    }
private InputAction CrearAccionDeRecarga(Shoot shoot)
{
    InputAction recarga = new InputAction(
        "Recarga",
        InputActionType.Button
    );

    recarga.AddBinding("<Keyboard>/r");
    recarga.performed += shoot.OnReload;
    recarga.Enable();

    return recarga;
}

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
    GameObject[] balas = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

    foreach (GameObject objeto in balas)
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

    [Test]
    public void A_SinClic_NoSeDispara()
    {
        // Arrange
        Shoot shoot = CrearArmaDePrueba();

        int municionInicial = shoot.CurrentAmmo;

        // Act
        InputAction.CallbackContext context = default;
        shoot.OnShoot(context);

        // Assert
        Assert.AreEqual(
            municionInicial,
            shoot.CurrentAmmo
        );
       
    }

    
    

    [UnityTest]
    public IEnumerator BSeRealizaUnClic_SeDisparaUnaVezYDisminuyeLaMunicion()
    {
    // Arrange
    Shoot shoot = CrearArmaDePrueba();

    // Esperamos a que Unity ejecute Start()
    yield return null;

    int municionInicial = shoot.CurrentAmmo;

    InputAction disparo = CrearAccionDeDisparo(shoot);

    // Act - Primer clic izquierdo
ClickLeftButton();

    int municionDespuesPrimerDisparo = shoot.CurrentAmmo;

    // Soltamos el botón izquierdo
    SoltarBoton();



    // Assert

    // El primer clic debe disparar y consumir una bala
    Assert.AreEqual(
        municionInicial - 1,
        municionDespuesPrimerDisparo
    );

    // Cleanup
    disparo.performed -= shoot.OnShoot;
    disparo.Disable();
    disparo.Dispose();




    }
    
    [UnityTest]
    public IEnumerator C_SeRealizanDosClicsSeguidos_SeRespetaElTiempoDeDisparo()
{
    // Arrange
    Shoot shoot = CrearArmaDePrueba();

    // Esperamos a que Unity ejecute Start()
    yield return null;

    int municionInicial = shoot.CurrentAmmo;

    InputAction disparo = CrearAccionDeDisparo(shoot);

    // Act - Primer clic izquierdo
ClickLeftButton();

   

    int municionDespuesPrimerDisparo = shoot.CurrentAmmo;

SoltarBoton();

    // Segundo clic izquierdo inmediatamente
ClickLeftButton();

    

    // Assert

    // El primer clic debe disparar y consumir una bala
    Assert.AreEqual(
        municionInicial - 1,
        municionDespuesPrimerDisparo
    );

    // El segundo clic debe ser rechazado por el shootRate
    Assert.AreEqual(
        municionDespuesPrimerDisparo,
        shoot.CurrentAmmo
    );

    // Cleanup
    disparo.performed -= shoot.OnShoot;
    disparo.Disable();
    disparo.Dispose();
}



    [UnityTest]
    public IEnumerator E_CargadorVacio_RecargaCorrectamente()
    {
        // Arrange
        Shoot shoot = CrearArmaDePrueba();

        yield return null;

        InputAction disparo = CrearAccionDeDisparo(shoot);

        // Guardamos la reserva inicial
        int reservaInicial = shoot.ReserveAmmo;

        // Vaciamos el cargador
        yield return VaciarCargador(shoot, disparo);

        // Act - Clic para iniciar la recarga
ClickLeftButton();

SoltarBoton();

        // Esperamos a que termine la recarga
        yield return new WaitForSeconds(shoot.reloadTime);

        // Assert
        Assert.AreEqual(
            shoot.magazineSize,
            shoot.CurrentAmmo
        );

        Assert.AreEqual(
            reservaInicial - shoot.magazineSize,
            shoot.ReserveAmmo
        );

        // Cleanup
        disparo.performed -= shoot.OnShoot;
        disparo.Disable();
        disparo.Dispose();
    }

    [UnityTest]
    public IEnumerator F_SinMunicionDeReserva_NoSeRecarga()
    {
    // Arrange
    Shoot shoot = CrearArmaDePrueba();

    yield return null;

    InputAction disparo = CrearAccionDeDisparo(shoot);

    // Dejamos el cargador vacío
    yield return VaciarCargador(shoot, disparo);

    // Dejamos la reserva en 0
    shoot.reserveAmmo = 0;

    // Act - Intentamos recargar
ClickLeftButton();

SoltarBoton();

    // Assert
    Assert.IsFalse(
        shoot.IsReloading
    );

    // Cleanup
    disparo.performed -= shoot.OnShoot;
    disparo.Disable();
    disparo.Dispose();
}




[UnityTest]
public IEnumerator G_Recarga_NoCargaMasQueLaCapacidadDisponible()
{
    // Arrange
    Shoot shoot = CrearArmaDePrueba();

    yield return null;

    InputAction disparo = CrearAccionDeDisparo(shoot);
    InputAction recarga = CrearAccionDeRecarga(shoot);

    // Act - Primer disparo
    ClickLeftButton();

    SoltarBoton();

    yield return new WaitForSeconds(shoot.shootRate);

    // Segundo disparo
    ClickLeftButton();
    SoltarBoton();

    // Presionamos R para recargar
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState(Key.R)
    );

    InputSystem.Update();

    // Soltamos R
    InputSystem.QueueStateEvent(
        keyboard,
        new KeyboardState()
    );

    InputSystem.Update();

    // Esperamos que termine la recarga
    yield return new WaitForSeconds(shoot.reloadTime);

    // Assert
    Assert.LessOrEqual(
        shoot.CurrentAmmo,
        shoot.magazineSize
    );

    // Cleanup
    disparo.performed -= shoot.OnShoot;
    disparo.Disable();
    disparo.Dispose();

    recarga.performed -= shoot.OnReload;
    recarga.Disable();
    recarga.Dispose();
}

[UnityTest]
public IEnumerator Disparo_BalaApareceEnLaPosicionDelSpawnPoint()
{
    // Arrange
    Shoot shoot = CrearArmaDePrueba();

    yield return null;

    InputAction disparo = CrearAccionDeDisparo(shoot);

    Vector3 posicionSpawnPoint = shoot.spawnPoint.position;

    // Act - Clic izquierdo
    ClickLeftButton();      



    // Buscamos la bala creada
    GameObject bala = GameObject.Find("Bullet(Clone)");

    // Assert
    Assert.IsNotNull(bala);

    Assert.AreEqual(
        posicionSpawnPoint,
        bala.transform.position
    );


        SoltarBoton();
    // Cleanup
    disparo.performed -= shoot.OnShoot;
    disparo.Disable();
    disparo.Dispose();
}

[UnityTest]
public IEnumerator Disparo_BalaSeMueveEnLaDireccionDelDisparo()
    {
        // Arrange
        Shoot shoot = CrearArmaDePrueba();

        yield return null;

        InputAction disparo = CrearAccionDeDisparo(shoot);

        Vector3 direccionDisparo = shoot.spawnPoint.forward;

        // Act - Clic izquierdo
        ClickLeftButton();

        // Esperamos un frame para que la física se actualice
        yield return new WaitForFixedUpdate();

        GameObject bala = GameObject.Find("Bullet(Clone)");

        // Assert
        Assert.IsNotNull(bala);

        Rigidbody rb = bala.GetComponent<Rigidbody>();

        Assert.IsNotNull(rb);

        // La velocidad debe apuntar en la misma dirección que el disparo
        float productoPunto = Vector3.Dot(
            rb.linearVelocity.normalized,
            direccionDisparo.normalized
        );

        Assert.Greater(
            productoPunto,
            0.99f
        );

        // Soltamos el botón
        SoltarBoton();

        // Cleanup
        disparo.performed -= shoot.OnShoot;
        disparo.Disable();
        disparo.Dispose();
    }

    private void SoltarBoton()
    {
        InputSystem.QueueStateEvent(
            mouse,
            new MouseState { buttons = 0 }
        );

        InputSystem.Update();
    }

    private void ClickLeftButton()
    {
        InputSystem.QueueStateEvent(
            mouse,
            new MouseState { buttons = 1 }
        );

        InputSystem.Update();
    }
}
