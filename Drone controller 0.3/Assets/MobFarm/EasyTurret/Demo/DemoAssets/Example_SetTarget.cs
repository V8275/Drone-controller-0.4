using UnityEngine;

namespace MobFarm {

    public class Example_SetTarget : MonoBehaviour {

        public Transform target;
        public MF_EasyTurret turret;

        void Update () {

            if ( turret ) {
                Rigidbody targetRigidbody = target ? target.GetComponent<Rigidbody>() : null;

                turret.SetTarget( target, targetRigidbody );
            }
        }
    }
}
