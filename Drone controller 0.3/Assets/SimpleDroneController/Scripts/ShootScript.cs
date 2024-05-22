using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootScript : MonoBehaviour
{
    [SerializeField] GameObject Bullet;
    [SerializeField] Transform Gun;
    [SerializeField] float reloadTime;
    [SerializeField] float bulletForce;

    float tempReload;

    private void Start()
    {
        tempReload = reloadTime;
    }

    private void Update()
    {
        tempReload -= Time.deltaTime;
        if (tempReload < 0 && Input.GetMouseButtonDown(0))
        {
            Shoot();
            tempReload = reloadTime;
        }
    }

    private void Shoot()
    {
        var b = Instantiate(Bullet, Gun.position, Gun.rotation);
        b.GetComponent<Rigidbody>().AddForce(Gun.transform.forward * bulletForce);
        StartCoroutine(TimerToDestroy(b));
    }

    private IEnumerator TimerToDestroy(GameObject o)
    {
        yield return new WaitForSeconds(5f);
        Destroy(o);
    }
}
