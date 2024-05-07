using UnityEngine;
using UnityEditor;
using System.Collections;

namespace MobFarm {

    [CustomEditor( typeof( MF_EasyTurret ) )]
    [CanEditMultipleObjects]
    public class MF_EasyTurret_Editor : Editor {

        public void OnSceneGUI () {
            SerializedObject obj = new SerializedObject( target );
            MF_EasyTurret script = (MF_EasyTurret)target;

            SerializedProperty showArcs = obj.FindProperty( "showArcs" );
            SerializedProperty limitLeftRight = obj.FindProperty( "limitLeftRight" );
            SerializedProperty limitUp = obj.FindProperty( "limitUp" );
            SerializedProperty limitDown = obj.FindProperty( "limitDown" );

            // used to get transform positions of the target
            Transform trans = script.GetBase();
            Transform rotator = script.GetRotator();
            Transform elevator = script.GetElevator();

            Color selectedColorX = new Color( 1f, .2f, .2f, .75f );
            Color selectedFadeX = new Color( selectedColorX.r, selectedColorX.g, selectedColorX.b, .25f );
            Color selectedColorY = new Color( .9f, 1f, .2f, .6f );
            Color selectedFadeY = new Color( selectedColorY.r, selectedColorY.g, selectedColorY.b, .2f );
            Color unselectedColor = new Color( 1f, 1f, 1f, .3f );
            Color unselectedFade = new Color( unselectedColor.r, unselectedColor.g, unselectedColor.b, .1f );

            if ( elevator && rotator ) {
                if ( showArcs.boolValue == true ) {
                    float handleSize = HandleUtility.GetHandleSize( script.transform.position );

                    // turret centerline
                    float centerLength = 0f;
                    if ( limitLeftRight.floatValue < 180f ) {
                        Handles.color = selectedColorY;
                        centerLength = 5f;
                    } else { // no rotation restriction
                        Handles.color = unselectedColor;
                        centerLength = 2f;
                    }
                    Handles.DrawDottedLine( rotator.position, rotator.position + ( trans.forward * centerLength * handleSize ), 5f );

                    // elevatior centerline
                    Handles.color = new Color( selectedColorX.r, selectedColorX.g, selectedColorX.b, 1f );
                    Handles.DrawDottedLine( elevator.position, elevator.position + ( rotator.forward * 10f * handleSize ), 5f );

                    // elevation lines
                    Handles.color = selectedColorX;
                    Vector3 angleDownV = Quaternion.AngleAxis( limitDown.floatValue, -elevator.right ) * rotator.forward;
                    Handles.DrawLine( elevator.position, rotator.position + angleDownV * 10f * handleSize, 1f );
                    Vector3 angleUpV = Quaternion.AngleAxis( limitUp.floatValue, -elevator.right ) * rotator.forward;
                    Handles.DrawLine( elevator.position, rotator.position + angleUpV * 10f * handleSize, 1f );
                    Handles.color = selectedFadeX;
                    Handles.DrawWireArc( elevator.position, -elevator.right, angleDownV, ( limitDown.floatValue * -1f ) + limitUp.floatValue, 1.1f * handleSize, 10f );

                    // rotation lines
                    Vector3 angleLeftV = Quaternion.AngleAxis( -limitLeftRight.floatValue, trans.up ) * trans.forward;
                    Vector3 angleRightV = Quaternion.AngleAxis( limitLeftRight.floatValue, trans.up ) * trans.forward;
                    Handles.color = selectedColorY;
                    if ( limitLeftRight.floatValue < 180f ) {
                        Handles.DrawLine( rotator.position, trans.position + angleLeftV * 10f * handleSize, 1f );
                        Handles.DrawLine( rotator.position, trans.position + angleRightV * 10f * handleSize, 1f );
                        Handles.color = selectedFadeY;
                        Handles.DrawWireArc( rotator.position, trans.up, angleLeftV, ( limitLeftRight.floatValue * 2f ), 1.1f * handleSize, 10f );
                    } else { // no rotation restriction
                        Handles.color = unselectedFade;
                        Handles.DrawWireArc( rotator.position, trans.up, angleLeftV, ( limitLeftRight.floatValue * 2f ), 1.1f * handleSize, 3f );
                    }

                    // trajectory line

                    //SerializedProperty shotSpeed = obj.FindProperty( "shotSpeed" );
                    //SerializedProperty aimType = obj.FindProperty( "aimType" );
                    //Transform weaponExit = script.GetWeaponExit();

                    //if ( shotSpeed.floatValue > 0 && weaponExit != null && aimType.enumValueIndex >= 2 ) {
                    //    float flightTime = 3f; // temp value
                    //    int segments = (int)Mathf.Round( flightTime * 4f ); // 4 segments per second
                    //    Vector3 thisPoint = weaponExit.position;
                    //    Vector3 velocity = ( weaponExit.forward * shotSpeed.floatValue );
                    //    // gun aim
                    //    Vector3[] tPoints = new Vector3[segments * 2];
                    //    for ( int i = 0; i < tPoints.Length; i += 2 ) {
                    //        tPoints[i] = thisPoint;
                    //        for ( int v = 0; v < 1 / ( Time.fixedDeltaTime * 4 ); v++ ) { // simulate physics steps, only draw line every .25 seconds
                    //            velocity += Physics.gravity * Time.fixedDeltaTime;
                    //            thisPoint += velocity * Time.fixedDeltaTime;
                    //        }
                    //        tPoints[i + 1] = thisPoint;
                    //    }
                    //    Handles.color = Color.white;
                    //    Handles.DrawLines( tPoints );
                    //}
                }
            }
        }

        public override void OnInspectorGUI () {
            serializedObject.Update();

            SerializedProperty thisProp; // temp prop to reduce property name duplication

            SerializedProperty showArcs = serializedObject.FindProperty( "showArcs" );
            SerializedProperty editorTarget = serializedObject.FindProperty( "editorTarget" );
            SerializedProperty myTarget = serializedObject.FindProperty( "target" );
            SerializedProperty hasSolution = serializedObject.FindProperty( "hasSolution" );
            SerializedProperty aimType = serializedObject.FindProperty( "aimType" );
            SerializedProperty ballisticArc = serializedObject.FindProperty( "ballisticArc" );
            SerializedProperty highArcIterations = serializedObject.FindProperty( "highArcIterations" );
            SerializedProperty shotSpeed = serializedObject.FindProperty( "shotSpeed" );
            SerializedProperty rotationRate = serializedObject.FindProperty( "rotationRate" );
            SerializedProperty elevationRate = serializedObject.FindProperty( "elevationRate" );
            SerializedProperty motionType = serializedObject.FindProperty( "motionType" );
            SerializedProperty dampen = serializedObject.FindProperty( "dampen" );
            SerializedProperty rotationAccel = serializedObject.FindProperty( "rotationAccel" );
            SerializedProperty elevationAccel = serializedObject.FindProperty( "elevationAccel" );
            SerializedProperty limitLeftRight = serializedObject.FindProperty( "limitLeftRight" );
            SerializedProperty limitUp = serializedObject.FindProperty( "limitUp" );
            SerializedProperty limitDown = serializedObject.FindProperty( "limitDown" );
            SerializedProperty restAngle = serializedObject.FindProperty( "restAngle" );
            SerializedProperty restDelay = serializedObject.FindProperty( "restDelay" );
            SerializedProperty rotator = serializedObject.FindProperty( "rotator" );
            SerializedProperty elevator = serializedObject.FindProperty( "elevator" );
            SerializedProperty weaponExit = serializedObject.FindProperty( "weaponExit" );
            SerializedProperty velocityRoot = serializedObject.FindProperty( "velocityRoot" );
            SerializedProperty rotatorSound = serializedObject.FindProperty( "rotatorSound" );
            SerializedProperty elevatorSound = serializedObject.FindProperty( "elevatorSound" );
            SerializedProperty eventTargets = serializedObject.FindProperty( "eventTargets" );

            GUILayout.Space( 4f );
            GUILayout.BeginHorizontal();
            GUI.enabled = false;
            GUILayout.Label( new GUIContent( "Script" ), EditorStyles.label, GUILayout.Width( 50f ) );
            EditorGUILayout.ObjectField( "", MonoScript.FromMonoBehaviour( (MF_EasyTurret)target ), typeof( MF_EasyTurret ), false, GUILayout.Width( 150f ) );
            GUI.enabled = true;
            GUILayout.Label( "", GUILayout.Width( 5f ) );
            if ( showArcs.boolValue == true ) {
                showArcs.boolValue = GUILayout.Toggle( showArcs.boolValue, new GUIContent( "Hide Arcs", "Show / hide arc guides in scene view." ), "Button", GUILayout.Width( 75f ) );
            } else {
                showArcs.boolValue = GUILayout.Toggle( showArcs.boolValue, new GUIContent( "Show Arcs", "Show / hide arc guides in scene view." ), "Button", GUILayout.Width( 75f ) );
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = editorTarget;
            if ( Application.isPlaying ) { GUI.enabled = false; }
            GUILayout.Label( new GUIContent( "Editor Target", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 85f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = myTarget;
            GUI.enabled = false;
            GUILayout.Label( new GUIContent( "Target", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 45f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = aimType;
            GUILayout.Label( new GUIContent( "Aim Type", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 60f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 125f ) );
            GUILayout.Label( "", GUILayout.Width( 5f ) );
            GUI.enabled = false;
            thisProp = hasSolution;
            GUILayout.Label( new GUIContent( "Has Solution", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 20f ) );
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if ( aimType.enumValueIndex == (int)MF_EasyTurret.AimType.Ballistic || aimType.enumValueIndex == (int)MF_EasyTurret.AimType.BallisticIntercept ) {
                GUILayout.BeginHorizontal();
                thisProp = ballisticArc;
                GUILayout.Label( new GUIContent( "Ballistic Arc", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 75f ) );
                EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 55f ) );
                GUILayout.Label( "", GUILayout.Width( 5f ) );
                thisProp = highArcIterations;
                GUILayout.Label( new GUIContent( "High Arc Iterations", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 110f ) );
                EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 30f ) );
                GUILayout.EndHorizontal();
            }

            if ( aimType.enumValueIndex != (int)MF_EasyTurret.AimType.Direct ) {
                GUILayout.BeginHorizontal();
                thisProp = shotSpeed;
                GUILayout.Label( new GUIContent( "Shot Speed", shotSpeed.tooltip ), EditorStyles.label, GUILayout.Width( 70f ) );
                if ( Application.isPlaying ) { GUI.enabled = false; }
                EditorGUILayout.PropertyField( shotSpeed, GUIContent.none, GUILayout.Width( 70f ) );
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space( 8f );
            GUILayout.BeginHorizontal();
            GUILayout.Label( "Rotation Properties:", EditorStyles.boldLabel, GUILayout.Width( 120f ) );
            GUILayout.Label( "(Deg. per second)", EditorStyles.miniLabel );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = motionType;
            GUILayout.Label( new GUIContent( "Motion Type", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 80f ) );
            if ( motionType.enumValueIndex == (int)MF_EasyTurret.MotionType.Smooth ) {
                GUILayout.Label( "", GUILayout.Width( 5f ) );
                thisProp = dampen;
                GUILayout.Label( new GUIContent( "Dampen", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 50f ) );
                EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 30f ) );
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = rotationRate;
            GUILayout.Label( new GUIContent( "Rotation Rate", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 40f ) );
            GUILayout.Label( "", GUILayout.Width( 5f ) );
            thisProp = elevationRate;
            GUILayout.Label( new GUIContent( "Elevation Rate", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 85f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 40f ) );
            GUILayout.EndHorizontal();

            if ( motionType.enumValueIndex == (int)MF_EasyTurret.MotionType.Smooth ) {
                GUILayout.BeginHorizontal();
                thisProp = rotationAccel;
                GUILayout.Label( new GUIContent( "Rotation Accel", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 87f ) );
                EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 40f ) );
                GUILayout.Label( "", GUILayout.Width( 5f ) );
                thisProp = elevationAccel;
                GUILayout.Label( new GUIContent( "Elevation Accel", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 92f ) );
                EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 40f ) );
                GUILayout.EndHorizontal();
            }

            GUILayout.Space( 8f );
            GUILayout.BeginHorizontal();
            GUILayout.Label( "Turn Arc Limit:", EditorStyles.boldLabel, GUILayout.Width( 90f ) );
            GUILayout.Label( "(0° to 180°) Mirrored on both sides.", EditorStyles.miniLabel );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = limitLeftRight;
            GUILayout.Label( new GUIContent( "Limit Left/Right", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 90f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();


            GUILayout.Space( 8f );
            GUILayout.BeginHorizontal();
            GUILayout.Label( "Elevation Arc Limits:", EditorStyles.boldLabel, GUILayout.Width( 123f ) );
            GUILayout.Label( "(-90° to 90°)", EditorStyles.miniLabel );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = limitUp;
            GUILayout.Label( new GUIContent( "Limit Up", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 90f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = limitDown;
            GUILayout.Label( new GUIContent( "Limit Down", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 90f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.Space( 8f );
            GUILayout.BeginHorizontal();
            GUILayout.Label( "Rest Angle:", EditorStyles.boldLabel, GUILayout.Width( 72f ) );
            GUILayout.Label( "(x = elevation, y = rotation)", EditorStyles.miniLabel );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = restAngle;
            GUILayout.Label( new GUIContent( "Rest Angle", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 65f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 110f ) );
            GUILayout.Label( "", GUILayout.Width( 5f ) );
            thisProp = restDelay;
            GUILayout.Label( new GUIContent( "Rest Delay", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 65f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none, GUILayout.Width( 40f ) );
            GUILayout.EndHorizontal();

            GUILayout.Space( 8f );
            GUILayout.Label( "Parts:", EditorStyles.boldLabel );

            GUILayout.BeginHorizontal();
            thisProp = rotator;
            GUILayout.Label( new GUIContent( "Rotator", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = elevator;
            GUILayout.Label( new GUIContent( "Elevator", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = weaponExit;
            GUILayout.Label( new GUIContent( "Weapon Exit", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            thisProp = velocityRoot;
            GUILayout.Label( new GUIContent( "Velocity Root", thisProp.tooltip ), EditorStyles.label, GUILayout.Width( 80f ) );
            EditorGUILayout.PropertyField( thisProp, GUIContent.none );
            GUILayout.EndHorizontal();

            GUILayout.Space( 8f );
            //GUILayout.Label( "Audio:", EditorStyles.boldLabel );

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( rotatorSound );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( elevatorSound );
            GUILayout.EndHorizontal();

            GUILayout.Space( 8f );
            GUILayout.BeginHorizontal();
            if ( Application.isPlaying ) { GUI.enabled = false; }
            EditorGUILayout.PropertyField( eventTargets );
            if ( Application.isPlaying ) { GUI.enabled = true; }
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}


