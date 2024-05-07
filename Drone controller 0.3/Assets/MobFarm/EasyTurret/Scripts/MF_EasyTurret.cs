using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace MobFarm {

    public enum TurretEventType { AimedAtTarget, GainedTargetAim, LostTargetAim, GainedTarget, LostTarget }
    public interface ITurretEvents {
        void TurretEvent ( TurretEventType eventType );
    }

    [HelpURL( "http://mobfarmgames.weebly.com/mf_easyturret.html" )]
	public class MF_EasyTurret : MonoBehaviour {
		
		public enum AimType { Direct, Intercept, Ballistic, BallisticIntercept };
		public enum MotionType { Constant, Smooth }

#pragma warning disable
		[SerializeField] bool showArcs = true; // used in editor to how/hide arc guides on individual turret basis
#pragma warning restore
		[Tooltip( "Target that can be initially set in the editor.\nChanging this during runtime has no effect." )]
		public Transform editorTarget;

		[Tooltip( "Current target shown for reference.\nSet by using:\nSetTarget( Transform )\nSetTarget( Transform, RigidBody )\nSetTarget( Transform, RigidBody2D )\n\n" +
			"If a target RigidBody is not specified, target object root will be searched for one." )]
		[SerializeField] Transform target; // shown in editor for reference
		[Tooltip( "Shows if turret has a mathematical solution to hit target, regardless if it is currently aimed correctly. False result is usually due to insufficient shot speed." )]
		public bool hasSolution;
		[Tooltip( "Direct: Will aim directly at target.\n\nIntercept: Will compute linear intercept to hit a moving target.\n\nBallistic: Will aim in a ballistic arc to hit a stationary target.\n\n" +
					"Ballistic Intercept: Will aim in a ballistic arc to hit a moving target.\n\n" +
					"Intercept and Ballistic Intercept modes will require the shot speed, and velocity of shooter and target. Ballistic mode requires shot speed. " +
					"All ballistic calculations are made with drag equal to 0." )]
		public AimType aimType;
		[Tooltip( "Specify which ballistic arc solution to use, since ballistic aim usually has two possible solutions." )]
		public ArcType ballisticArc;
		[Tooltip( "High arc generally requires more iterations to hit moving targets.\nMore = better accuracy, but more cpu usage.\nIf both shooter and target are stationary, iterations will be ignored." )]
		public int highArcIterations = 3;
		//public int lowArcIterations = 3; // not needed for typical scenarios - possibly needed for situations with low gravity and low shot speeds
		[Tooltip( "Shoud be set to match the shot speed of the weapon used. This may be assigned by the weapon directly." )]
		public float shotSpeed;

		//[Header("Turning Properties:")]
		[Tooltip( "Constant = Turret parts move at specified rotation/elevation rate.\n\n"+
					"Smooth = Turret parts will accelerate at specified rotation/elevation acceleration, and max rates will be at rotation/elevation rates." )]
		public MotionType motionType;
		[Tooltip( "A multiplier to give a cushion to stopping distance. Lower values try to slow down later, but might overshoot. " +
					"Higher values give more precise aim, but might take a little longer to match target." )]
		public float dampen = 1.5f;
		[Tooltip( "Max rotation rate of the rotator part in degrees/second." )]
		public float rotationRate = 50f;
		[Tooltip( "Max rotation rate of the elevator part in degrees/second." )]
		public float elevationRate = 50f;
		[Tooltip( "Acceleration rate of the rotator part in degrees/second/second." )]
		public float rotationAccel = 50f;
		[Tooltip( "Acceleration rate of the elevator part in degrees/second/second." )]
		public float elevationAccel = 50f;
		//[Header("Turn Arc Limit: (0 to 180) Mirrored on both sides.")]
		[Range( 0, 180 )] public float limitLeftRight = 180f;
		//[Header("Elevation Arc Limits: (-90 to 90)")]
		[Range( 0, 90 )] public float limitUp = 90f;
		[Range( -90, 0 )] public float limitDown = 0f;
		//[Header("Rest Angle: (x = elevation, y = rotation)")]
		[Tooltip( "Position when turret has no target." )]
		public Vector2 restAngle;
		[Tooltip( "Time without a target before turret will move to rest position." )]
		public float restDelay;

		//[Header("Parts:")]
		[Tooltip( "Transform that rotates around the y axis. Should be a child of the turret base." )]
		public Transform rotator;
		[Tooltip( "Transform that rotates around the x axis. Should be a child of the rotator part." )]
		public Transform elevator;
		[Tooltip( "The Transform where a shot will be produced. This may be changed dynamicaly in the case of multiple exits - alternately, pick a transform at the center of multiple exits\n" +
					"If left blank the elevator part will be assigned, but this should eventually be assigned by a weapon." )]
		public Transform weaponExit;
		[Tooltip( "Assign a specific transform to be used for velocity. Ideally, one that has a Rigidbody component.\n" +
				 "If blank, will assume this turrets's root transform." )]
		public Transform velocityRoot;
		public AudioObject rotatorSound;
		public AudioObject elevatorSound;

		[Tooltip( "Interface ITurretEvents sends TurretEvent( TurretEventType ) to objects in this list.\n\n" +
				"enum TurretEventType { AimedAtTarget, GainedTargetAim, LostTargetAim, GainedTarget, LostTarget }" )]
		public List<TurretEventTarget> eventTargets;

		[System.Serializable]
		public class TurretEventTarget {
			public GameObject gameObject;
			[HideInInspector] public ITurretEvents eventTarget;

			public TurretEventTarget ( ITurretEvents eventTarget ) {
				new TurretEventTarget( null, eventTarget );
			}
			public TurretEventTarget ( GameObject gameObject, ITurretEvents eventTarget ) { // overload to nicely display object in inspector
				this.gameObject = gameObject;
				this.eventTarget = eventTarget;
			}
		}

		[HideInInspector] public Vector3 myVelocity;
		[HideInInspector] public bool requestAngle;
		[HideInInspector] public Vector2 requestedAngle;
		[HideInInspector] public Vector3 targetAimLocation;
		[HideInInspector] public Rigidbody targetRigidbody;
		[HideInInspector] public NavMeshAgent targetNavMeshAgent;
		[HideInInspector] public Rigidbody2D targetRigidbody2D;
		[HideInInspector] public Vector3 lastTargetPosition;

		// event trackers
		bool aimCheck; // might not need to check 
		bool eventTargetsAllNull; // if all values in eventTargets are null
		bool gainedTarget;
		bool lostTarget;
		bool gainedAim;
		bool lostAim;
		bool aimed;
		bool hadTarget;

		Rigidbody myRigidbody;
		Rigidbody2D myRigidbody2D;
		NavMeshAgent myNavMeshAgent;

		Vector3 lastPlatformPosition;
		float timeSinceTarget;
		Vector3 targetLoc;
		Vector3 rotatorPlaneTarget;
		Vector3 elevatorPlaneTarget;

		float curRotRate = 0f;
		float curEleRate = 0f;
		float lastRotDistRelative = 0f;
		float lastRotRateRelative = 0f;
		float lastEleDistRelative = 0f;
		float lastEleRateRelative = 0f;
		float lastRotRate = 0f;
		float lastEleRate = 0f;
		float lastDeltaTime = 0f;

		float rotDistRelative = 0f;
		float rotRateRelative = 0f;
		float eleDistRelative = 0f;
		float eleRateRelative = 0f;

		float lastRotY;
		float lastEleX;
		// absolute value and averaged rates for audio
		float rotRate;
		float eleRate;
		float rotRate1; // saved rate
		float eleRate1; // saved rate

		bool error;

		[System.Serializable]
		public class AudioObject {
			public AudioSource audioSource;
			[Tooltip( "Minimum pitch multiplier at near 0 turn rate." )] public float pitchMin;
			[Tooltip( "Maximum pitch multiplier at fastest turn rate." )] public float pitchMax;
			[Tooltip( "Minimum volume multiplier at near 0 turn rate." )] public float volumeMin;
			[Tooltip( "Maximum volume multiplier at fastest turn rate." )] public float volumeMax;
			[HideInInspector] public float stopTime;
			[HideInInspector] public float origPitch;
			[HideInInspector] public float origVolume;

			public void Awake () {
				if ( audioSource ) {
					origVolume = audioSource.volume;
					origPitch = audioSource.pitch;
				}
			}
		}

		public void AddEventTarget ( ITurretEvents eventTarget ) {
			AddEventTarget( null, eventTarget );
		}
		public void AddEventTarget ( GameObject gameObject, ITurretEvents eventTarget ) {
			eventTargets.Add( new TurretEventTarget( gameObject, eventTarget ) );
			if ( eventTarget != null ) { eventTargetsAllNull = false; }
		}

		public void RemoveEventTarget ( ITurretEvents eventTarget ) {
			for ( int i = 0; i < eventTargets.Count; i++ ) {
				if ( eventTargets[i].eventTarget == eventTarget ) {
					eventTargets.RemoveAt( i );
				}
			}
			// check if all are null
			eventTargetsAllNull = true;
			for ( int i = 0; i < eventTargets.Count; i++ ) {
				if ( eventTargets[i].eventTarget != null ) {
					eventTargetsAllNull = false; break;
				}
			}
		}

		void Awake () {

			if ( !velocityRoot ) {
				velocityRoot = transform.root;
			}
			myRigidbody = velocityRoot.GetComponent<Rigidbody>();
			myRigidbody2D = velocityRoot.GetComponent<Rigidbody2D>();
			myNavMeshAgent = velocityRoot.GetComponent<NavMeshAgent>();

			eventTargetsAllNull = true;
			for ( int i = 0; i < eventTargets.Count; i++ ) {
				if ( eventTargets[i].gameObject != null ) {
					eventTargetsAllNull = false;
					eventTargets[i].eventTarget = eventTargets[i].gameObject.GetComponent<ITurretEvents>();
				}
			}

			timeSinceTarget = -restDelay;
			// initialize audio
			rotatorSound.Awake();
			elevatorSound.Awake();

			if ( weaponExit == null ) {
				if ( elevator ) { weaponExit = elevator; } // if no weaponExit, assign as elevator, but this should eventually be changed by the weapon
			}

			if ( editorTarget ) { SetTarget( editorTarget ); } // set initial target in editor

			CheckErrors();
		}

		void OnEnable () { // reset for object pool support
			if ( error == true ) { return; }
			timeSinceTarget = -restDelay;
			rotator.transform.localRotation = Quaternion.identity;
			elevator.transform.localRotation = Quaternion.identity;
			// event bools
			gainedTarget = false;
			gainedAim = false;
			lostTarget = false;
			lostAim = false;
			aimed = false;
			hadTarget = false;
		}

		void OnDisable () { // reset for object pool support
			target = null;
			hasSolution = false;
		}

		Vector3 UnitVelocity () {
			Vector3 vel;
			if ( myNavMeshAgent ) {
				vel = myNavMeshAgent.velocity;
			} else if ( myRigidbody && myRigidbody.isKinematic == false ) {
				vel = myRigidbody.velocity;
			} else if ( myRigidbody2D && myRigidbody2D.isKinematic == false ) {
				vel = myRigidbody2D.velocity;
			} else { // no velocity sources found - calculate based on last position
				vel = lastPlatformPosition - transform.position;
				lastPlatformPosition = transform.position;
			}
			return vel;
		}

		public void SetTarget ( Transform target ) {
			SetTarget( target, null, null, null );
		}
		public void SetTarget ( Transform target, Rigidbody targetRigidbody ) {
			SetTarget( target, targetRigidbody, null, null );
		}
		public void SetTarget ( Transform target, Rigidbody2D targetRigidbody2D ) {
			SetTarget( target, null, targetRigidbody2D, null );
		}
		public void SetTarget ( Transform target, NavMeshAgent targetNavMeshAgent ) {
			SetTarget( target, null, null, null );
		}
		void SetTarget ( Transform targ, Rigidbody targRB, Rigidbody2D targRB2D, NavMeshAgent targNMA ) {
			if ( targ && targ.gameObject.activeInHierarchy == false ) { targ = null; }
			if ( target != targ ) {
				// events
				if ( targ ) { // new target
					gainedTarget = true;
					hadTarget = true;
				}
				if ( target || !targ ) { // will remove target
					lostTarget = true;
				}
				target = targ;
			}
			if ( target ) {
				if ( targRB || targRB2D || targNMA ) {
					targetRigidbody = targRB;
					targetRigidbody2D = targRB2D;
					targetNavMeshAgent = targNMA;
				} else { // no velocity source specified, shearch for one
					targetRigidbody = target ? target.GetComponent<Rigidbody>() : null;
					targetRigidbody2D = target ? target.GetComponent<Rigidbody2D>() : null;
					targetNavMeshAgent = target ? target.GetComponent<NavMeshAgent>() : null;
				}
			}
		}

		public void ClearTarget () {
			SetTarget( null, null, null, null );
		}

		public Transform GetTarget () {
			return target;
		}

		void Update () {
			if ( error == true ) { return; }
			if ( target == null || target.gameObject.activeInHierarchy == false ) {
				target = null;
				if ( hadTarget == true ) { lostTarget = true; hadTarget = false; }
				targetAimLocation = Vector3.zero;
				hasSolution = false;
			}
			myVelocity = UnitVelocity();
			targetAimLocation = AimLocation();

			// process aim location
			if ( target && requestAngle == false ) {
				Vector3 _localTarget;
				timeSinceTarget = Time.time;
				// find target's location in rotation plane
				_localTarget = rotator.InverseTransformPoint( targetAimLocation );
				rotatorPlaneTarget = rotator.TransformPoint( new Vector3( _localTarget.x, 0f, _localTarget.z ) );
				// find target's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)

				Vector3 _cross = Vector3.Cross( rotator.up, targetAimLocation - weaponExit.position );
				Vector3 _level = Vector3.Cross( _cross, rotator.up ); // find direction towards target but level with local plane
				float _angle = Vector3.Angle( _level, targetAimLocation - weaponExit.position );
				if ( _localTarget.y < rotator.InverseTransformPoint( weaponExit.position ).y ) { _angle *= -1; } // should angle be negative?
				elevatorPlaneTarget = weaponExit.position + ( Quaternion.AngleAxis( _angle, -rotator.right ) * rotator.forward * 1000f );

			} else { // no target, or requested angle is true
				float specialAngleX = 0f;
				float specialAngleY = 0f;
				bool doSpecialAngle = false;

				if ( requestAngle == true ) { // requested angle
					doSpecialAngle = true;
					specialAngleX = requestedAngle.x; specialAngleY = requestedAngle.y; // set rotation and elevation goals to the requested position
				} else if ( Time.time >= timeSinceTarget + restDelay ) { // no target
					doSpecialAngle = true;
					specialAngleX = restAngle.x; specialAngleY = restAngle.y; // set rotation and elevation goals to the rest position
				}
				if ( doSpecialAngle == true ) { // set rotation and elevation goals to the specified angle
					specialAngleX = Mathf.Clamp( specialAngleX, 0f, 89.94f ); // close to 90 causes Quaternion.RotateTowards() to get confused due to gimbaling

					if ( limitLeftRight < 180f ) { // if there's a rotation limit, special angles close to 180 or -180 causes confusion as to which way to rotate
						if ( specialAngleY > 179.94f && specialAngleY < 180.06f ) {
							specialAngleY = specialAngleY > 180f ? 180.06f : 179.94f;
						} else if ( specialAngleY < -179.94 && specialAngleY > -180.06f ) {
							specialAngleY = specialAngleY < -180f ? -180.06f : -179.94f;
						}
					}
					rotatorPlaneTarget = rotator.position + ( Quaternion.AngleAxis( specialAngleY, transform.up ) * transform.forward * 1000f );
					elevatorPlaneTarget = elevator.position + ( Quaternion.AngleAxis( specialAngleX, -rotator.right ) * rotator.forward * 1000f );
				}
			}

			// turning

			// turn opposite if shortest route is through a gimbal limit
			int directionFactor = 1;
			if ( limitLeftRight < 180 ) { // is there a gimbal limit?
				float _bearing = Vector3.SignedAngle( transform.forward, rotatorPlaneTarget - rotator.position, transform.up );
				float _aimAngle = Vector3.SignedAngle( transform.forward, rotator.forward, transform.up );
				float _curRotSeperation = Vector3.SignedAngle( rotator.forward, rotatorPlaneTarget - weaponExit.position, transform.up );

				// targetAngle and rotatorAnlge on opposite halves && need to turn more than 180
				if ( Mathf.Sign( _bearing ) != Mathf.Sign( _aimAngle ) && Mathf.Sign( _bearing ) != Mathf.Sign( _curRotSeperation ) ) {
					directionFactor = -1;
				}
			}

			// store rotation and elevation for rate computation
			lastRotY = rotator.localEulerAngles.y;
			lastEleX = elevator.localEulerAngles.x;

			Vector3 _localMount = rotator.InverseTransformPoint( weaponExit.position ); // for weapons not aligned with rotator
			Vector3 rv = rotatorPlaneTarget - rotator.TransformPoint( new Vector3( _localMount.x, 0f, _localMount.z ) );
			Vector3 ev = elevatorPlaneTarget - weaponExit.position;

			// store relative tracking rates
			rotDistRelative = Vector3.SignedAngle( rotator.forward, rv, transform.up ) * directionFactor;
			float rotRateRelative_temp = ( lastRotDistRelative - rotDistRelative ) / lastDeltaTime;
			eleDistRelative = Vector3.SignedAngle( elevator.forward, elevatorPlaneTarget - elevator.position, -rotator.right );
			float eleRateRelative_temp = ( lastEleDistRelative - eleDistRelative ) / lastDeltaTime;

			// running average relative rates to smooth spikes.
			rotRateRelative = ( rotRateRelative_temp + lastRotRateRelative ) * .5f;
			eleRateRelative = ( eleRateRelative_temp + lastEleRateRelative ) * .5f;
			// store values for next frame
			lastDeltaTime = Time.deltaTime;
			lastRotDistRelative = rotDistRelative;
			lastRotRateRelative = rotRateRelative_temp;
			lastEleDistRelative = eleDistRelative;
			lastEleRateRelative = eleRateRelative_temp;
			lastRotRate = curRotRate;
			lastEleRate = curEleRate;

			if ( motionType == MotionType.Constant ) {

				Quaternion _rot;

				// apply rotation
				if ( rv != Vector3.zero ) { // prevent LookRotation is 0 error
					_rot = Quaternion.LookRotation( rv, transform.up );
					rotator.rotation = Quaternion.RotateTowards( rotator.rotation, _rot, rotationRate * directionFactor * Time.deltaTime );
				}

				// apply elevation
				if ( ev != Vector3.zero ) { // prevent LookRotation is 0 error
					_rot = Quaternion.LookRotation( ev, rotator.up );
					elevator.rotation = Quaternion.RotateTowards( elevator.rotation, _rot, elevationRate * Time.deltaTime );
				}

			} else { // smooth motion type

				// check rotator snap to target 
				if ( Mathf.Abs( rotRateRelative ) <= rotationAccel * .1f && Mathf.Abs( rotDistRelative ) <= rotationAccel * Time.deltaTime * .1f ) {
					//&& curRotRate + (rotAccel * Time.deltaTime) <= rotationRate && curRotRate + (rotAccel * Time.deltaTime) >= -rotationRate ) {

					if ( rv != Vector3.zero ) { // prevent LookRotation is 0 error
						Quaternion _rot = Quaternion.LookRotation( rv, transform.up );
						rotator.rotation = Quaternion.RotateTowards( rotator.rotation, _rot, rotationRate * Time.deltaTime );
					}

					float d = Vector3.SignedAngle( rotator.forward, rv, transform.up );
					curRotRate = ( ( ( rotDistRelative - d ) / Time.deltaTime ) + lastRotRate ) * .5f; // find new rate, average from last rate to smooth spikes.

					//Debug.Log( "Rate: " + curRotRate + "   RelRate:" + ( rotRateRelative ) + "   Angle: " + rotDistRelative + "  *** SNAP" );

                } else { // no snap

					float dts = ( ( rotRateRelative * rotRateRelative ) / ( 2 * rotationAccel ) ) * dampen;
					float rotStopPoint = rotDistRelative - ( dts * Mathf.Sign( rotRateRelative ) );

					if ( ( rotStopPoint <= 0 && rotRateRelative > 0f ) || ( rotStopPoint <= ( -rotationAccel * Time.deltaTime * .5f ) && rotRateRelative < 0f )
								|| ( rotStopPoint >= 0 && rotRateRelative < 0f ) || rotStopPoint >= ( rotationAccel * Time.deltaTime * .5f ) && rotRateRelative > 0f ) { // apply acceleration
						curRotRate += Mathf.Sign( rotStopPoint ) * rotationAccel * Time.deltaTime;
						//if ( Mathf.Sign( rotStopPoint ) == Mathf.Sign( curRotRate ) ) { Debug.Log("Accelerate"); } else {  Debug.Log("Decelerate"); }
					}
					curRotRate = Mathf.Clamp( curRotRate, -rotationRate, rotationRate );
					rotator.rotation = Quaternion.AngleAxis( curRotRate * Time.deltaTime, transform.up ) * rotator.rotation; // apply rotation

					//Debug.Log( "Rate: "+ curRotRate + "   RelRate:" + (rotRateRelative) + "   Angle: " + rotDistRelative + "   DTS: " + dts + "   StopPoint: " + rotStopPoint );

                }


				// check elevator snap to target
				if ( Mathf.Abs( eleRateRelative ) <= elevationAccel * .2f && Mathf.Abs( eleDistRelative ) <= elevationAccel * Time.deltaTime * .2f ) {
					if ( ev != Vector3.zero ) { // prevent LookRotation is 0 error
						Quaternion _rot = Quaternion.LookRotation( ev, rotator.up );
						elevator.rotation = Quaternion.RotateTowards( elevator.rotation, _rot, elevationRate * Time.deltaTime );
					}

					float d = Vector3.SignedAngle( elevator.forward, ev, rotator.up );
					curEleRate = ( ( ( eleDistRelative - d ) / Time.deltaTime ) + lastEleRate ) * .5f; // find new rate, average from last rate to smooth spikes.

				} else { // no snap

					// distance to stop
					float dts = ( ( eleRateRelative * eleRateRelative ) / ( 2 * elevationAccel ) ) * dampen;
					float eleStopPoint = eleDistRelative - ( dts * Mathf.Sign( eleRateRelative ) );

					if ( ( eleStopPoint <= 0 && eleRateRelative > 0f ) || ( eleStopPoint <= ( -elevationAccel * Time.deltaTime * .5f ) && eleRateRelative < 0f )
								|| ( eleStopPoint >= 0 && eleRateRelative < 0f ) || eleStopPoint >= ( elevationAccel * Time.deltaTime * .5f ) && eleRateRelative > 0f ) { // apply acceleration
						curEleRate += Mathf.Sign( eleStopPoint ) * elevationAccel * Time.deltaTime;
					}
					curEleRate = Mathf.Clamp( curEleRate, -elevationRate, elevationRate );
					elevator.rotation = Quaternion.AngleAxis( curEleRate * Time.deltaTime, -rotator.right ) * elevator.rotation; // apply elevation
				}
			}

			CheckGimbalLimits();

			// prevent nonsence
			rotator.localEulerAngles = new Vector3( 0f, rotator.localEulerAngles.y, 0f );
			elevator.localEulerAngles = new Vector3( elevator.localEulerAngles.x, 0f, 0f );

			// store current turning rates for audio
			rotRate1 = rotRate;
			eleRate1 = eleRate;
			// find new rates
			rotRate = ( lastRotY - rotator.localEulerAngles.y ) / Time.deltaTime;
			eleRate = ( lastEleX - elevator.localEulerAngles.x ) / Time.deltaTime;
			// sanitize rates
			rotRate = Mathf.Clamp( Mathf.Abs( rotRate ), 0f, rotationRate );
			eleRate = Mathf.Clamp( Mathf.Abs( eleRate ), 0f, elevationRate );
			// average with old rate
			rotRate = ( rotRate + rotRate1 ) * .5f;
			eleRate = ( eleRate + eleRate1 ) * .5f;

			// turn sounds	
			if ( rotatorSound.audioSource ) {
				TurnSound( rotatorSound, rotRate, rotationRate );
			}
			if ( elevatorSound.audioSource ) {
				TurnSound( elevatorSound, eleRate, elevationRate );
			}

			// turretEvents
			if ( eventTargetsAllNull == false ) { // at least one valid event target
												  // this stops running aimcheck every frame if not necessary, but aimCheck won't be updated if no eventTargets registered
												  // can lead to extra gainedAim and lostAim if it changed while no eventTargets registered. If this is an issue, just run it all the time.
				aimCheck = AimCheck( .5f ); // cache AimCheck()
			}
			bool gt = false; bool aat = false; bool gta = false; bool lt = false; bool lta = false;

			if ( gainedTarget == true ) { gt = true; gainedTarget = false; }
			if ( aimCheck == true ) {
				aat = true; ;
				if ( aimed == false ) {
					gainedAim = true;
					aimed = true;
				}
			} else {
				if ( aimed == true ) {
					lostAim = true;
					aimed = false;
				}
			}
			if ( gainedAim == true ) { gta = true; gainedAim = false; }
			if ( lostTarget == true ) { lt = true; lostTarget = false; }
			if ( lostAim == true ) { lta = true; lostAim = false; }

			SendEvents( gt, aat, gta, lt, lta );
		}

		void SendEvents ( bool gt, bool aat, bool gta, bool lt, bool lta ) {
			for ( int i = 0; i < eventTargets.Count; i++ ) {
				if ( eventTargets[i].eventTarget != null ) {
					if ( gt ) { eventTargets[i].eventTarget.TurretEvent( TurretEventType.GainedTarget ); }
					if ( aat ) { eventTargets[i].eventTarget.TurretEvent( TurretEventType.AimedAtTarget ); }
					if ( gta ) { eventTargets[i].eventTarget.TurretEvent( TurretEventType.GainedTargetAim ); }
					if ( lt ) { eventTargets[i].eventTarget.TurretEvent( TurretEventType.LostTarget ); }
					if ( lta ) { eventTargets[i].eventTarget.TurretEvent( TurretEventType.LostTargetAim ); }
				}
			}
		}


		Vector3 AimLocation () {
			// intercept and ballistics
			if ( target ) {
				hasSolution = true;
				Vector3 targetVelocity = Vector3.zero;
				targetLoc = target.position;
				Vector3 gravityV3 = Physics.gravity;

				targetAimLocation = targetLoc; // default is direct aim

				if ( aimType != AimType.Direct ) { // will need velocities and shotspeed
					if ( shotSpeed != 0 ) {
						// target velocity
						if ( aimType == AimType.Intercept || aimType == AimType.BallisticIntercept ) {
							if ( targetNavMeshAgent ) {
								targetVelocity = targetNavMeshAgent.velocity;
							} else if ( targetRigidbody && targetRigidbody.isKinematic == false ) { // if target has a rigidbody, use velocity
								targetVelocity = targetRigidbody.velocity;
							} else if ( targetRigidbody2D && targetRigidbody2D.isKinematic == false ) {
								targetVelocity = targetRigidbody2D.velocity;
							} else { // otherwise compute velocity from change in position
								targetVelocity = ( targetLoc - lastTargetPosition ) / Time.deltaTime;
								lastTargetPosition = targetLoc;
							}
						}
					} else { // shot speed = 0
						hasSolution = false;
					}
				}

				if ( hasSolution == true ) {
					if ( ( aimType == AimType.Ballistic || aimType == AimType.BallisticIntercept ) && ( gravityV3.x + gravityV3.y + gravityV3.z ) != 0 ) { // ballistic aim
                        // find initial aim angle
                        float? _ballAim = MFball.BallisticAimAngle( targetLoc, weaponExit.position, shotSpeed, ballisticArc );
						if ( _ballAim == null ) {
							hasSolution = false;
						} else {
							if ( aimType == AimType.BallisticIntercept ) { // ballistic + intercept, iterate for better ballistic accuracy
								if ( !Mathf.Approximately( myVelocity.sqrMagnitude, 0f ) || !Mathf.Approximately( targetVelocity.sqrMagnitude, 0f ) ) { // don't need if shooter and target are not moving 
									int bi = 0;
									int biMax = 1;
									if ( ballisticArc == ArcType.High ) {
										biMax = highArcIterations;
									}

                                    // add this if more iterations are needed for low arc - unlikely to need except in cases with low gravity and low shot speeds

                                    //if ( ballisticArc == ArcType.Low ) {
                                    //    biMax = 3; // enter a value here
                                    //}

                                    while ( hasSolution == true && bi++ < biMax ) {
										_ballAim = MFball.BallisticIteration( weaponExit.position, shotSpeed, (float)_ballAim, ballisticArc, target.position, targetVelocity, myVelocity, targetLoc, out targetLoc );
										if ( _ballAim == null ) { hasSolution = false; } // no solution
									}
								}
							}
							if ( hasSolution == true ) { // solution can fail in balistic iteration
								Vector3 _cross = -Vector3.Cross( ( targetLoc - weaponExit.position ), -gravityV3 );
								Quaternion _eleAngleDir = Quaternion.AngleAxis( (float)_ballAim * Mathf.Rad2Deg, -_cross );
								// targetAimLocation must be at correct distance for later calculations
								targetAimLocation = weaponExit.position + ( _eleAngleDir * Vector3.Cross( _cross, -gravityV3 ) ).normalized * Vector3.Distance( weaponExit.position, targetLoc );
							}
						}
						if ( hasSolution == false ) { // no solution for ballistic aim, aim 45° opposite gravity
							Vector3 _cross = -Vector3.Cross( ( targetLoc - weaponExit.position ), -gravityV3 );
							Quaternion _eleAngleDir = Quaternion.AngleAxis( 45f, -_cross );
							targetAimLocation = weaponExit.position + ( _eleAngleDir * Vector3.Cross( _cross, -gravityV3 ) ).normalized * Vector3.Distance( weaponExit.position, targetLoc );
						}

					} else if ( aimType != AimType.Direct ) { // intercept or ballistic with no gravity
															  // point at linear intercept position
						Vector3? _interceptAim = MFcompute.Intercept( weaponExit.position, myVelocity, shotSpeed, targetLoc, targetVelocity );
						if ( _interceptAim == null ) {
							hasSolution = false;
						} else {
							targetAimLocation = (Vector3)_interceptAim;
						}

					}
				}

				// direct aim - use default targetAimLocation
			} else {
				hasSolution = false;
				targetAimLocation = Vector3.zero;
			}
			return targetAimLocation;
		}

		void TurnSound ( AudioObject ao, float rate, float rateMax ) {
			// rate should be positive
			if ( rate > .01f && rateMax > 0f ) { // prevent near 0 from floating point error
				float rateFactor = rate / rateMax;
				ao.audioSource.pitch = ao.origPitch * ( ao.pitchMin + ( rateFactor * ( ao.pitchMax - ao.pitchMin ) ) );
				ao.audioSource.volume = ao.origVolume * ( ao.volumeMin + ( rateFactor * ( ao.volumeMax - ao.volumeMin ) ) );
				if ( ao.audioSource.isPlaying == false ) {
					ao.audioSource.Play();
				}
				ao.stopTime = Time.time + .1f; // continuously refresh stop time
			} else {
				if ( Time.time >= ao.stopTime ) { // make sure audio has been stopped for at least .1 seconds
					ao.audioSource.Stop();
				}
			}
		}

		// checks if turret has moved outside of its gimbal limits. If so, it puts it back to the appropriate limit
		void CheckGimbalLimits () {
			if ( error == true ) { return; }

			if ( rotator.localEulerAngles.y > limitLeftRight && rotator.localEulerAngles.y <= 180 ) {
				rotator.localEulerAngles = new Vector3( rotator.localEulerAngles.x, limitLeftRight, rotator.localEulerAngles.z );
				curRotRate = 0f;
			} else if ( rotator.localEulerAngles.y < 360 - limitLeftRight && rotator.localEulerAngles.y >= 180 ) {
				rotator.localEulerAngles = new Vector3( rotator.localEulerAngles.x, 360 - limitLeftRight, rotator.localEulerAngles.z );
				curRotRate = 0f;
			}
			if ( elevator.localEulerAngles.x > -limitDown && elevator.localEulerAngles.x <= 180 ) {
				elevator.localEulerAngles = new Vector3( -limitDown, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
				curEleRate = 0f;
			} else if ( elevator.localEulerAngles.x < 360 - limitUp && elevator.localEulerAngles.x >= 180 ) {
				elevator.localEulerAngles = new Vector3( 360 - limitUp, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
				curEleRate = 0f;
			}
		}

		// check if the turret is aimed at the target
		public bool AimCheck ( float aimTolerance ) {
			return AimCheck( 0f, aimTolerance );
		}
		public bool AimCheck ( float targetSize, float aimTolerance ) {
			if ( error == true ) { return false; }
			bool ready = false;
			if ( target && hasSolution == true ) {
				float targetRange = 0;
				if ( targetSize != 0 ) { // won't need range if size is 0
					targetRange = Vector3.Distance( weaponExit.position, target.position );
				}
				float deg = 0f;
				if ( targetSize != 0 ) {
					deg = Mathf.Atan( ( targetSize * .5f ) / targetRange ) * Mathf.Rad2Deg;
				}
				float targetFovRadius = Mathf.Clamp( deg + aimTolerance, 0f, 180f );
				if ( Vector3.Angle( weaponExit.forward, targetAimLocation - weaponExit.position ) <= targetFovRadius ) {
					ready = true;
				}
			}
			return ready;
		}

		public float? GetAngleToTargetAim () {
			if ( target ) {
				return Vector3.Angle( weaponExit.forward, targetAimLocation - weaponExit.position );
			} else {
				return null;
			}
		}

		public float? GetRotationToTargetAim () {
			if ( target && hasSolution == true ) {
				// find target's location in rotation plane
				return Vector3.SignedAngle( transform.forward, targetAimLocation, transform.up );
			} else {
				return null; // no target or no solution
			}
		}

		public float? GetElevationToTargetAim () {
			if ( target && hasSolution == true ) {
				// find target's location in elevation plane
				Vector3 _localTarget = rotator.InverseTransformPoint( targetAimLocation );

				Vector3 _cross = Vector3.Cross( rotator.up, targetAimLocation - weaponExit.position );
				Vector3 _level = Vector3.Cross( _cross, rotator.up ); // find direction towards target but level with local plane
				float _angle = Vector3.Angle( _level, targetAimLocation - weaponExit.position );
				if ( _localTarget.y < rotator.InverseTransformPoint( weaponExit.position ).y ) { _angle *= -1; } // should angle be negative?
				Vector3 _elevatorPlaneTarget = weaponExit.position + ( Quaternion.AngleAxis( _angle, -rotator.right ) * rotator.forward * 1000f );

				return Vector3.SignedAngle( rotator.forward, _elevatorPlaneTarget, -rotator.right );
			} else {
				return null; // no target or no solution
			}
		}

		// tests if a given position is within the gimbal limits of this turret 
		public bool PositionWithinLimits ( Vector3 position ) {
			if ( error == true ) { return false; }
			// find targets's location in rotation plane
			Vector3 _localTarget = rotator.InverseTransformPoint( position );
			Vector3 _rotatorPlaneTarget = rotator.TransformPoint( new Vector3( _localTarget.x, 0f, _localTarget.z ) );

			float _testAngle = Vector3.SignedAngle( transform.forward, _rotatorPlaneTarget - transform.position, transform.up );
			if ( _testAngle > limitLeftRight || _testAngle < -limitLeftRight ) {
				return false;
			}

			// find targets's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)
			Vector3 _cross = Vector3.Cross( rotator.up, position - weaponExit.position );
			Vector3 _level = Vector3.Cross( _cross, rotator.up ); // find direction towards target but level with local plane
			float _angle = Vector3.Angle( _level, position - weaponExit.position );
			if ( _localTarget.y < elevator.localPosition.y + weaponExit.localPosition.y ) { _angle *= -1; } // should angle be negative?
			elevatorPlaneTarget = weaponExit.position + ( Quaternion.AngleAxis( _angle, -rotator.right ) * rotator.forward * 1000f );

			_testAngle = Vector3.SignedAngle( rotator.forward, elevatorPlaneTarget - weaponExit.position, -rotator.right );
			if ( _testAngle < limitDown || _testAngle > limitUp ) {
				return false;
			}
			return true;
		}

		public void SetAngleGoal ( float x, float y ) {
			SetAngleGoal( new Vector2( x, y ) );
		}
		public void SetAngleGoal ( Vector2 angle ) {
			requestAngle = true;
			requestedAngle = angle;
		}

		public void ClearAngleGoal () {
			requestAngle = false;
		}

		public bool CheckAngleGoal () {
			if ( error == true ) { return false; }
			if ( requestAngle == true ) {
				Vector3 rotTarg = rotator.position + ( Quaternion.AngleAxis( requestedAngle.y, transform.up ) * transform.forward * 1000f );
				Vector3 eleTarget = elevator.position + ( Quaternion.AngleAxis( requestedAngle.x, -rotator.right ) * rotator.forward * 1000f );
				if ( Vector3.Angle( rotTarg - rotator.position, rotator.forward ) > .1f || Vector3.Angle( eleTarget - elevator.position, elevator.forward ) > .1f ) {
					return false;
				}
			} else {
				return false;
			}
			return true;
		}

		public Transform GetRotator () { //helper for editor
			return rotator;
		}
		public Transform GetElevator () { //helper for editor
			return elevator;
		}
		public Transform GetBase () { //helper for editor
			return transform;
		}
		public Transform GetWeaponExit () { //helper for editor
			return weaponExit;
		}

		bool CheckErrors () {
			error = false;

			if ( rotator == null ) { Debug.Log( this + " " + transform.root.name + ": Turret rotator part hasn't been defined." ); error = true; }
			if ( elevator == null ) { Debug.Log( this + " " + transform.root.name + ": Turret elevator part hasn't been defined." ); error = true; }
			if ( !Mathf.Approximately( transform.localScale.x, transform.localScale.z ) ) { Debug.Log( this + " " + transform.root.name + ": Turret transform x and z scales must be equal." ); error = true; }
			if ( rotator ) {
				if ( rotator.localScale.x != 1 ) { Debug.Log( this + " " + transform.root.name + ": rotator part transform xyz scales must = 1." ); error = true; }
				if ( rotator.localScale.y != 1 ) { Debug.Log( this + " " + transform.root.name + ": rotator part transform xyz scales must = 1." ); error = true; }
				if ( rotator.localScale.z != 1 ) { Debug.Log( this + " " + transform.root.name + ": rotator part transform xyz scales must = 1." ); error = true; }
			}

			return error;
		}
	}
}