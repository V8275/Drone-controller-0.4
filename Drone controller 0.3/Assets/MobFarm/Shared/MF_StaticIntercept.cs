using UnityEngine;
using System.Collections;

namespace MobFarm {

	[HelpURL( "http://mobfarmgames.weebly.com/mf_staticintercept.html" )]
	public class MFcompute {

		//first-order intercept
		public static Vector3? Intercept ( Vector3 shooterPosition, Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, Vector3 targetVelocity ) {
			Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
			float? t = InterceptTime( shotSpeed, targetPosition - shooterPosition, targetRelativeVelocity );
			if ( t == null ) {
				return null;
			} else {
				return targetPosition + ( t * targetRelativeVelocity );
			}
		}
		//first-order intercept using relative target position
		public static float? InterceptTime ( float shotSpeed, Vector3 targetRelativePosition, Vector3 targetRelativeVelocity ) {
			float velocitySquared = targetRelativeVelocity.sqrMagnitude;
			if ( velocitySquared < 0.001f ) {
				return 0f;
			}
			float a = velocitySquared - ( shotSpeed * shotSpeed );

			//handle similar velocities
			if ( Mathf.Abs( a ) < 0.001f ) {
				float t = -targetRelativePosition.sqrMagnitude / ( 2f * Vector3.Dot( targetRelativeVelocity, targetRelativePosition ) );
				return Mathf.Max( t, 0f ); //don't shoot back in time
			}

			float b = 2f * Vector3.Dot( targetRelativeVelocity, targetRelativePosition );
			float c = targetRelativePosition.sqrMagnitude;
			float determinant = b * b - 4f * a * c;

			if ( determinant > 0f ) { //determinant > 0; two intercept paths (most common)
				determinant = Mathf.Sqrt( determinant );
				float t1 = ( -b + determinant ) / ( 2f * a );
				float t2 = ( -b - determinant ) / ( 2f * a );
				if ( t1 > 0f ) {
					if ( t2 > 0f ) {
						return Mathf.Min( t1, t2 ); // both are positive
					} else {
						return t1; // only t1 is positive
					}
				} else {
					if ( t2 > 0f ) {
						return t2; // only t2 is positive
					} else {
						return null; // no intercept path
					}
				}
			} else if ( determinant < 0f ) { //determinant < 0; no intercept path
				return null;
			} else { //determinant = 0; one intercept path, pretty much never happens
				return Mathf.Max( -b / ( 2f * a ), 0f ); //don't shoot back in time
			}
		}
		/* Intercept() and InterceptTime() modified and based off of linear intercept code by Daniel Brauer. Used with permission. The copyright notice below only applies to those portions of code.
		The MIT License (MIT)
		Copyright (c) 2008 Daniel Brauer
		Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
		The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
		*/
	}
}

