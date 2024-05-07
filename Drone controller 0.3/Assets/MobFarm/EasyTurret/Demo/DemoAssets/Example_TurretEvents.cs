using UnityEngine;

namespace MobFarm {

    public class Example_TurretEvents : MonoBehaviour, ITurretEvents { // implement interface here

        public bool aimedAtTarget;
        public bool gainedTarget;
        public bool lostTarget;
        public bool gainedTargetAim;
        public bool lostTargetAim;

        // implement method TurretEvents
        void ITurretEvents.TurretEvent( TurretEventType eventType ) {

            // adding every TurretEventType as an option to display Debug message based on bool state

            if ( eventType == TurretEventType.AimedAtTarget ) {
                if ( aimedAtTarget == true ) { Debug.Log( "Aimed at Target" ); }
            }

            if ( eventType == TurretEventType.GainedTarget ) {
                if ( gainedTarget == true ) { Debug.Log( "Gained Target" ); }
            }

            if ( eventType == TurretEventType.LostTarget ) {
                if ( lostTarget == true ) { Debug.Log( "Lost Target" ); }
            }

            if ( eventType == TurretEventType.GainedTargetAim ) {
                if ( gainedTargetAim == true ) { Debug.Log( "Gained Target Aim" ); }
            }

            if ( eventType == TurretEventType.LostTargetAim ) {
                if ( lostTargetAim == true ) { Debug.Log( "Lost Target Aim" ); }
            }
        }
    }
}
