using UnityEngine;
using System.Collections;

namespace MobFarm {

	public class MF_DemoMovement : MonoBehaviour {

		[Tooltip( "The current navigation target." )]
		public Transform navTarget;

		[Tooltip( "(deg. / sec.)\nHow fast the vehicle can turn" )]
		public float turnRate;
		[Tooltip( "Vehicle movement power." )]
		public float thrust;

		[Tooltip( "A group of waypoints. If this object has children, the waypoint list will be built from the children. Otherwise the object becomes a single waypoint." )]
		[SerializeField] protected Transform _waypointGroup;
		public Transform waypointGroup {
			get { return _waypointGroup; }
			set {
				_waypointGroup = value;
				waypoints = BuildArrayFromChildren( _waypointGroup );
			}
		}
		[Tooltip( "The list of waypoints built from the waypoint group." )]
		public Transform[] waypoints;
		[Tooltip( "If true: When reaching a waypoint, choose the next one at random, instead of in order." )]
		public bool randomWpt;
		[Tooltip( "The index of the current waypoint to be sent to a mobility script." )]
		public int curWpt;
		[Tooltip( "How close to approach waypoints before choosing the next one in the list." )]
		public float waypointProx = 2f;

		float throttle;
		Rigidbody myRigidbody;

		public void OnValidate () {
			waypointGroup = _waypointGroup;
			curWpt = Mathf.Clamp( curWpt, 0, Mathf.Max( 0, waypoints.Length - 1 ) ); // check for out of range curWpt
		}

		void Awake () {
			myRigidbody = GetComponentInParent<Rigidbody>();

			navTarget = null;
			if ( randomWpt ) {
				curWpt = Random.Range( 0, waypoints.Length );
			} else {
				curWpt = 0;
			}
		}

		void OnDisable () { // reset for object pool support
			waypointGroup = null;
			curWpt = 0;
			throttle = 0f;
			if ( myRigidbody ) {
				myRigidbody.velocity = Vector3.zero;
				myRigidbody.angularVelocity = Vector3.zero;
			}
		}

		void FixedUpdate () {

			if ( waypoints.Length > 0 ) {
				if ( waypoints[curWpt] ) {
					// next waypoint
					if ( ( transform.position - waypoints[curWpt].position ).sqrMagnitude <= waypointProx * waypointProx ) { // at waypoint
						if ( randomWpt == true ) {
							curWpt = Random.Range( 0, waypoints.Length );
						} else {
							curWpt = Mod( curWpt + 1, waypoints.Length );
						}
					}
					navTarget = waypoints[curWpt];
				}
			}

			if ( navTarget != null ) {
				Vector3 navTargetAim = navTarget.position;
				Steer( navTargetAim );
				float _angle = Vector3.Angle( navTargetAim - transform.position, transform.forward );
				throttle = 1f;
			} else {
				Steer( transform.position + transform.forward );
				throttle = 0f;
			}

			Move( throttle );
		}

		void Steer ( Vector3 goal ) {
			Quaternion rot = Quaternion.identity;
			if ( myRigidbody ) {
				if ( goal != myRigidbody.position ) { // avoid LookRotation of 0
					rot = Quaternion.LookRotation( goal - myRigidbody.position, Vector3.up );
					myRigidbody.MoveRotation( Quaternion.RotateTowards( myRigidbody.rotation, rot, turnRate * Time.fixedDeltaTime ) );
				}
			} else {
				rot = Quaternion.LookRotation( goal - transform.position, transform.up );
				transform.rotation = Quaternion.RotateTowards( transform.rotation, rot, turnRate * Time.fixedDeltaTime );
			}
		}

		public void Move ( float percent ) {
			if ( navTarget && waypointProx > 0 ) {
				if ( ( transform.position - navTarget.position ).sqrMagnitude <= waypointProx * waypointProx ) {
					throttle = 0f;
				}
			}
			if ( myRigidbody ) {
				myRigidbody.AddForce( transform.forward * thrust * throttle * Time.fixedDeltaTime );
			} else {
				transform.position = transform.position + ( transform.forward * thrust * throttle * Time.fixedDeltaTime );
			}
		}

		// proper modulo, NOT ramainder operator (%)
		int Mod ( int a, int b ) {
			if ( b == 0 ) { return 0; }
			return ( ( ( a ) % b ) + b ) % b;
		}

		// Build an array from a given parent's children
		Transform[] BuildArrayFromChildren ( Transform trans ) {
			Transform[] bArray;
			if ( trans ) { // build array contents from children of trans
				int _childCount = trans.childCount;
				if ( _childCount > 0 ) { // found at least 1 child, use children
					bArray = new Transform[_childCount];
					for ( int i = 0; i < _childCount; i++ ) {
						bArray[i] = trans.GetChild( i );
					}
				} else { // no children, use parent 
					bArray = new Transform[1];
					bArray[0] = trans;
				}
			} else {
				bArray = new Transform[0];
			}
			return bArray;
		}
	}
}

