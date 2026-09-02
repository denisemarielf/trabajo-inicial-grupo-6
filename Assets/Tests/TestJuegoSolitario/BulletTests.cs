
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

public class BulletTests
{
    private List<GameObject> objetosDePrueba =
        new List<GameObject>();

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject objeto in objetosDePrueba)
        {
            if (objeto != null)
            {
                Object.DestroyImmediate(objeto);
            }
        }

        objetosDePrueba.Clear();
    }

    [UnityTest]
    public IEnumerator Bala_AlColisionar_SeDetiene()
    {
        // Arrange

        // Creamos la bala
        GameObject bala =
            new GameObject("Bullet");

        objetosDePrueba.Add(bala);

        Rigidbody rbBala =
            bala.AddComponent<Rigidbody>();

        bala.AddComponent<SphereCollider>();

        bala.AddComponent<Bullet>();

        // Colocamos la bala
        bala.transform.position =
            Vector3.zero;

        // Le damos velocidad hacia adelante
        rbBala.linearVelocity =
            Vector3.forward * 10f;

        // Creamos el objetivo
        GameObject objetivo =
            new GameObject("Objetivo");

        objetosDePrueba.Add(objetivo);

        objetivo.transform.position =
            new Vector3(0, 0, 0.5f);

        objetivo.AddComponent<BoxCollider>();

        // Esperamos a que Unity ejecute Awake()
        yield return null;

        // Act
        // Esperamos a que Unity procese la física
        yield return new WaitForFixedUpdate();

        // Assert
        Assert.IsTrue(
            rbBala.isKinematic
        );

        Assert.AreEqual(
            Vector3.zero,
            rbBala.linearVelocity
        );

        Assert.AreEqual(
            Vector3.zero,
            rbBala.angularVelocity
        );
    }
}
