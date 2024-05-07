using UnityEngine;
using System.Collections;

namespace MobFarm {

	public class MF_DemoGun : MonoBehaviour {

		public enum GravityUsage { Default, Gravity, NoGravity }

		[Tooltip( "The turret that holds this gun. If blank, one will be searched up parent hierarchy." )]
		public MF_EasyTurret turretScript;
		[Header( "Projectile Settings:" )]
		[Tooltip( "Shot prefab to spawn for each shot." )]
		public MF_DemoProjectile shotPrefab;
		public float damage;
		[Tooltip( "(meters/sec)" )]
		public float shotSpeed;
		[Tooltip( "(meters)" )]
		public float maxRange;
        [Tooltip( "will be computed from shot speed and max range. (seconds)" )]
        float shotDuration;
        [Tooltip( "Default: Will use gravity only if shot rigidbody uses gravity.\nGravity: Change all shots to use gravity.\nNo Gravity: Change all shots to not use gravity." )]
		public GravityUsage gravity;

		[Header( "Gun Settings:" )]
		[Tooltip( "(deg radius)\nAdds random inaccuracy to shots.\n0 = perfect accuracy." )]
		public float inaccuracy; // in degrees
		[Tooltip( "(seconds)\nThe minimum time between shots.\n0 = every frame." )]
		public float cycleTime; // min time between shots
		[Tooltip( "Audio to play for each shot." )]
		public AudioSource shotSound;
		[Tooltip( "the Transform where a shot will be produced." )]
		public Transform exit;

		[Header( "Targeting:" )]
		[Tooltip( "Drag and drop target." )]
		public Transform target;
		[Tooltip( "Angle tolerance (deg.) gun must be pointing towards aim location to fire." )]
		public float aimTolerance = 1f;

		Rigidbody targetRigidbody;
		float delay;
		bool usingGravity;
		bool error;

		void OnValidate () {
			// compute shotDuration from speed and range
			if ( shotSpeed > 0 && maxRange > 0 ) {
				shotDuration = maxRange / shotSpeed;
			}

			if ( Application.isPlaying == true ) {
				if ( !turretScript ) { turretScript = GetComponentInParent<MF_EasyTurret>(); }
				if ( turretScript ) { turretScript.shotSpeed = shotSpeed; } // shot speed might have been changed

				usingGravity = false; // remains false if forceing no gravity or if projectile doesn't use it
				if ( shotPrefab.GetComponent<Rigidbody>() ) { // verify rigidbody
					if ( gravity == GravityUsage.Gravity ) { // force gravity
						usingGravity = true;
					} else {
						if ( shotPrefab.GetComponent<Rigidbody>().useGravity == true && gravity == GravityUsage.Default ) { // use gravity if projectile does
							usingGravity = true;
						}
					}
				}

				CheckErrors();
			}
		}

		void Awake () {
			if ( error == true ) { return; }

			if ( turretScript ) {
				turretScript.shotSpeed = shotSpeed;
				if ( exit ) { turretScript.weaponExit = exit; } // assign weapon exit
			}

			if ( turretScript ) { // set any initial target specified in editor for demo purposes
				turretScript.shotSpeed = shotSpeed;
				if ( target ) {
					SetTarget( target );
				} else if ( turretScript.editorTarget ) {
					target = turretScript.editorTarget;
					targetRigidbody = target.root.GetComponent<Rigidbody>();
				}
			}
		}

		void Update () {
			if ( turretScript ) {
				// check if target changes
				if ( turretScript.GetTarget() != target ) {
					SetTarget( target );
				}

				if ( target ) {
					if ( ReadyCheck() == true ) { // weapon is ready to fire
						if ( turretScript.AimCheck( .01f, aimTolerance ) == true ) { // aimed properly
							if ( RangeCheck( turretScript.targetAimLocation ) == true ) { // target within range

								// could add a line-of-sight check here

								DoFire();
							}
						}
					}
				}
			}
		}

		void SetTarget ( Transform targ ) {
			targetRigidbody = targ ? targ.root.GetComponent<Rigidbody>() : null;
			if ( turretScript ) {
				turretScript.SetTarget( targ, targetRigidbody );
				turretScript.shotSpeed = shotSpeed;
			}
		}

		// use to determine if weapon is ready to fire. Seperate function to allow other scripts to check ready status.
		public bool ReadyCheck () {
			if ( gameObject.activeInHierarchy == false ) { return false; }
			return ( Time.time >= delay );
		}

		// use this only if already checked if weapon is ready to fire
		public void DoFire () {
			if ( error == true ) { return; }
			if ( gameObject.activeInHierarchy == false ) { return; }

			// fire weapon
			// create shot

			// ideally change this to use object pooling
			GameObject myShot = (GameObject)Instantiate( shotPrefab.gameObject, exit.position, exit.transform.rotation );

			if ( myShot != null ) {
				// find inaccuracy
				Vector2 errorV2 = Random.insideUnitCircle * inaccuracy;
				myShot.transform.rotation *= Quaternion.Euler( errorV2.x, errorV2.y, 0 );
				// shot settings
				Rigidbody rb = myShot.GetComponent<Rigidbody>();
				rb.velocity = turretScript.myVelocity + ( myShot.transform.forward * shotSpeed );
				rb.useGravity = usingGravity;
				MF_DemoProjectile shotScript = myShot.GetComponent<MF_DemoProjectile>();
				shotScript.duration = shotDuration;
				shotScript.damage = damage;

				// audio
				if ( shotSound ) {
					shotSound.Play();
				}

				delay = Time.time + cycleTime; // find next delay
			}
		}

		public bool RangeCheck ( Transform trans ) {
			return trans == null ? false : RangeCheck( trans.position );
		}
		public bool RangeCheck ( Vector3 position ) {
			if ( gameObject.activeInHierarchy == false ) { return false; }

			float gravity = -Physics.gravity.y;
			float sqRange = Vector3.Project( exit.position - position, Vector3.up ).sqrMagnitude;
			if ( usingGravity == true ) { // ballistic range 
				float h = position.y - exit.position.y;
				float cos = shotSpeed * Mathf.Cos( 45f );
				float sin = shotSpeed * Mathf.Sin( 45f );
				float f1 = ( cos ) / gravity;
				float f2 = ( sin ) + Mathf.Sqrt( ( sin * sin ) + ( 2 * gravity * h ) );
				float ballRange = f1 * f2;
				if ( sqRange <= ballRange * ballRange ) {
                    return true;
                }
            } else { // standard range
				if ( sqRange <= ( maxRange * maxRange ) * 1.05f ) {
					return true;
				}
			}
			return false;
		}

		public bool CheckErrors () {
			error = false;

			if ( turretScript == null ) { Debug.Log( this + " " + transform.root.name + ": Turret object hasn't been defined and one could not be found." ); error = true; }
			if ( shotPrefab == null ) { Debug.Log( this + " " + transform.root.name + ": Weapon shot prefab hasn't been defined." ); error = true; }
			if ( exit == null ) { Debug.Log( this + " " + transform.root.name + ": Weapon exit hasn't been defined." ); error = true; }
			if ( shotSpeed <= 0f ) { Debug.Log( this + " " + transform.root.name + ": Weapon shot speed must be > 0." ); error = true; }
			if ( maxRange <= 0f ) { Debug.Log( this + " " + transform.root.name + ": Weapon maxRange must be > 0." ); error = true; }

			return error;
		}
	}
}

