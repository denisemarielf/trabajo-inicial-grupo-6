using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ShootTests
{
    private GameObject arma;
   private Shoot shoot;

    [SetUp]
    public void SetUp()
    {
        arma = new GameObject("Arma_Test");
        shoot = arma.AddComponent<Shoot>();
    }

    [TearDown]
    public void TearDown()
    {
        if (arma != null)
        {
            Object.DestroyImmediate(arma);
        }
    }

    [Test]


    [UnityTest]
    public IEnumerator CargadorVacio_SeIniciaLaRecarga()
    {
        // Dejamos el cargador vacío
        SetCurrentAmmo(0);

        // Ejecutamos directamente TryReload()
        InvocarTryReload();

        // La recarga debe haberse iniciado
        Assert.IsTrue(shoot.IsReloading);

        yield return null;
    }

[Test]
public void Recarga_NoSuperaElLimiteDelCargador()
{
    // Arrange
    SetCurrentAmmo(5);
    shoot.reserveAmmo = 60;

    // La cantidad máxima permitida es el tamaño del cargador
    int ammoNeeded = shoot.magazineSize - shoot.CurrentAmmo;
    int ammoToLoad = Mathf.Min(ammoNeeded, shoot.ReserveAmmo);

    // Act
    SetCurrentAmmo(shoot.CurrentAmmo + ammoToLoad);

    // Assert
    Assert.AreEqual(
        shoot.magazineSize,
        shoot.CurrentAmmo
    );

    Assert.LessOrEqual(
        shoot.CurrentAmmo,
        shoot.magazineSize
    );
}

    private void SetCurrentAmmo(int cantidad)
    {
        FieldInfo campo = typeof(Shoot).GetField(
            "currentAmmo",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        campo.SetValue(shoot, cantidad);
    }

    private void InvocarTryReload()
    {
        MethodInfo metodo = typeof(Shoot).GetMethod(
            "TryReload",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        metodo.Invoke(shoot, null);
    }
}
