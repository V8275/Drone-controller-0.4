using UnityEngine;

namespace SimpleDroneController
{
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        [System.Serializable]
        public class PositionMovement
        {
            [System.Serializable]
            public class Longitudinal
            {
                public KeyCode forwardKey = KeyCode.W;
                public KeyCode backwardKey = KeyCode.S;
                public float input = 0.0f;
                public float smooth = 4.0f;
            }

            [System.Serializable]
            public class Lateral
            {
                public KeyCode leftKey = KeyCode.A;
                public KeyCode rightKey = KeyCode.D;
                public float input = 0.0f;
                public float smooth = 4.0f;
            }

            [System.Serializable]
            public class Vertical
            {
                public KeyCode upKey = KeyCode.I;
                public KeyCode downKey = KeyCode.K;
                public float input = 0.0f;
                public float smooth = 4.0f;
            }

            public KeyCode AfterburnerKey = KeyCode.Tab; // кнопка форсажа

            public float speed = 100.0f;
            public float AfterburnerSpeed; // скорость с форсажем
            public Longitudinal longitudinal;
            public Lateral lateral;
            public Vertical vertical;
            [HideInInspector] public bool abEffect = false;
        }


        [System.Serializable]
        public class RotationMovement
        {
            [System.Serializable]
            public class Yaw
            {
                public KeyCode leftKey = KeyCode.J;
                public KeyCode rightKey = KeyCode.L;
                public float input = 0.0f;
                public float maxSpeed = 2.0f;
                public float smooth = 4.0f;
            }

            public Yaw yaw;
        }

        [System.Serializable]
        public class Visual
        {
            public Transform transform;
            public Tilt tilt;
            public Roll roll;
            public Hover hover;
            public Propeller propeller;

            [System.Serializable]
            public class Tilt
            {
                public float lateralAngle = 25.0f;
                public float longitudinalAngle = 30.0f;
                public float yawAngle = 20.0f;
                public float verticalAngle = 5.0f;
                public float smooth = 7.0f;
            }

            [System.Serializable]
            public class Roll
            {
                public bool isEnabled = true;
                public bool isRolling = false;
                public KeyCode key = KeyCode.Space;
                public float coolDownSeconds = 1.0f;
            }

            [System.Serializable]
            public class Hover
            {
                public bool isEnabled = true;
                public float speed = 0.5f;
                public float height = 0.2f;
            }

            [System.Serializable]
            public class Propeller
            {
                public bool isEnabled = true;
                public Transform[] transforms;
                public float speed = 900.0f;
            }
        }

        [System.Serializable]
        public class Sound
        {
            public bool enabled = true;
            public AudioSource engine;
        }

        [System.Serializable]
        public class Effects
        {
            public ParticleSystem mainEngineFlame;
            public ParticleSystem leftManeuveringEngineFlame;
            public ParticleSystem rightManeuveringEngineFlame;
            public ParticleSystem leftLateralEngineFlame;
            public ParticleSystem rightLateralEngineFlame;
            public ParticleSystem bottomAltitudeEngineFlame;
            public ParticleSystem topAltitudeEngineFlame;
            public ParticleSystem reverseEngineFlame;
        }

        [SerializeField] bool isOn = true;
        public PositionMovement m_position;
        public RotationMovement m_rotation;
        public Visual m_visual;
        public Effects m_effects;
        public Rigidbody m_rigidbody;
        public Sound m_sound;

        private void Awake()
        {
            if (!TryGetComponent(out m_rigidbody))
            {
                Debug.LogWarning("Rigidbody component not found on the GameObject.");
            }
            m_position.AfterburnerSpeed = m_position.speed + (m_position.speed * 0.2f);
        }


        private void Update()
        {
            UpdateInput();
            ActivateEffects();
            UpdateSound();
        }

        private void FixedUpdate()
        {
            MovePosition();
            MoveRotation();
            TiltAndRoll();
            Hover();
            SpinPropeller();
        }

        public void SetEngineState(bool p_isOn)
        {
            isOn = p_isOn;
        }

        private void UpdateInput()
        {
            if (!isOn)
            {
                m_position.lateral.input = 0.0f;
                m_position.longitudinal.input = 0.0f;
                m_position.vertical.input = 0.0f;
                m_rotation.yaw.input = 0.0f;
                return;
            }

            if (Input.GetKey(m_position.AfterburnerKey)) m_position.abEffect = true;
            else m_position.abEffect = false;

            //LATERAL LEFT RIGHT
            m_position.lateral.input = GetInputAxis(m_position.lateral.input, m_position.lateral.smooth, m_position.lateral.rightKey, m_position.lateral.leftKey);

            //LATERAL FORWARD BACKWARD
            m_position.longitudinal.input = GetInputAxis(m_position.longitudinal.input, m_position.longitudinal.smooth,
                m_position.longitudinal.forwardKey, m_position.longitudinal.backwardKey);


            //VERTICAL
            m_position.vertical.input = GetInputAxis(m_position.vertical.input, m_position.vertical.smooth, m_position.vertical.upKey, m_position.vertical.downKey);

            //YAW INPUT
            m_rotation.yaw.input = GetInputAxis(m_rotation.yaw.input, m_rotation.yaw.smooth, m_rotation.yaw.rightKey, m_rotation.yaw.leftKey);
        }

        private static float GetInputAxis(float p_currentValue, float p_speed, KeyCode p_positiveKey, KeyCode p_negativeKey)
        {
            float target = Input.GetKey(p_positiveKey) ? 1.0f : Input.GetKey(p_negativeKey) ? -1.0f : 0.0f;

            if (p_currentValue == target)
                return p_currentValue;

            p_currentValue = Mathf.Lerp(p_currentValue, target, p_speed * Time.deltaTime);

            if (Mathf.Abs(p_currentValue - target) < 0.01f)
            {
                p_currentValue = target;
            }

            return p_currentValue;
        }

        private void MovePosition()
        {
            if (m_rigidbody == null)
                return;

            float x = m_position.lateral.input;
            float y = m_position.vertical.input;
            float z = m_position.longitudinal.input;

            // Convert input values to local direction
            Vector3 localDirection = transform.TransformDirection(new Vector3(x, y, z));

            // Set Rigidbody velocity based on local direction
            if (!m_position.abEffect) m_rigidbody.velocity = localDirection * m_position.speed;
            else m_rigidbody.velocity = localDirection * m_position.AfterburnerSpeed;
        }

        private void MoveRotation()
        {
            float yaw = transform.eulerAngles.y + (m_rotation.yaw.input * m_rotation.yaw.maxSpeed);
            m_rigidbody.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }

        float rollCooldownCounter = 0.0f;
        float currRoll = 0.0f;
        private void TiltAndRoll()
        {
            float x = (m_position.longitudinal.input * m_visual.tilt.longitudinalAngle) + (m_position.vertical.input * m_visual.tilt.verticalAngle); //ForwardBackward , vertical
            float z = (m_position.lateral.input * m_visual.tilt.lateralAngle) + (m_rotation.yaw.input * m_visual.tilt.yawAngle); //Yaw , RightLeft


            if (m_visual.roll.isEnabled)
            {
                if (Input.GetKey(m_visual.roll.key) && !m_visual.roll.isRolling && rollCooldownCounter >= m_visual.roll.coolDownSeconds)
                {
                    m_visual.roll.isRolling = true;
                    rollCooldownCounter = 0.0f;
                }

                if (m_visual.roll.isRolling)
                {
                    currRoll = currRoll + (Time.deltaTime * 900.0f);
                    if (currRoll >= 180.0f)
                        currRoll = -180.0f;

                    if (Mathf.Abs(currRoll) < 5.5f)
                    {
                        currRoll = 0.0f;
                        m_visual.roll.isRolling = false;
                    }

                    z = currRoll;
                }

                if (!m_visual.roll.isRolling && rollCooldownCounter < m_visual.roll.coolDownSeconds)
                    rollCooldownCounter += Time.deltaTime;
            }

            Quaternion current = m_visual.transform.localRotation;
            Quaternion target = Quaternion.Euler(x, 0f, -z);

            m_visual.transform.localRotation = Quaternion.Slerp(current, target, Time.deltaTime * m_visual.tilt.smooth);
        }


        Vector3 posOffset = new Vector3();
        Vector3 tempPos = new Vector3();
        void Hover()
        {
            if (!m_visual.hover.isEnabled)
                return;

            if (m_visual.transform == null)
            {
                Debug.LogError("Transform component is not found under Visual component");
                return;
            }

            float floatAmountY = Mathf.Sin(Time.fixedTime * Mathf.PI * m_visual.hover.speed) * m_visual.hover.height;
            float floatAmountX = Mathf.Cos(Time.fixedTime * Mathf.PI * m_visual.hover.speed) * m_visual.hover.height;

            tempPos = posOffset;
            tempPos.y += floatAmountY;
            tempPos.x += floatAmountX;

            m_visual.transform.localPosition = tempPos;
        }

        void SpinPropeller()
        {
            if (!m_visual.propeller.isEnabled)
                return;

            if (m_visual.propeller.transforms == null)
                return;

            for (int i = 0; i < m_visual.propeller.transforms.Length; i++)
            {
                m_visual.propeller.transforms[i].Rotate(Vector3.up * m_visual.propeller.speed * Time.deltaTime, Space.Self);
            }
        }

        private void UpdateSound()
        {
            if (m_sound == null) return;
            if (!m_sound.enabled) return;

            m_sound.engine.pitch = 1 + (m_rigidbody.velocity.magnitude / 130);
        }

        private void ActivateEffects()
        {
            if (!isOn)
            {
                DeactivateAllEffects();
                return;
            }

            if (m_effects.mainEngineFlame != null)
            {
                m_effects.mainEngineFlame.Play();
            }

            if (m_effects.leftManeuveringEngineFlame != null)
            {
                if (m_position.lateral.input < 0)
                {
                    m_effects.leftManeuveringEngineFlame.Play();
                }
                else
                {
                    m_effects.leftManeuveringEngineFlame.Clear();
                    m_effects.leftManeuveringEngineFlame.Pause();
                }
            }

            if (m_effects.rightManeuveringEngineFlame != null)
            {
                if (m_position.lateral.input > 0)
                {
                    m_effects.rightManeuveringEngineFlame.Play();
                }
                else
                {
                    m_effects.rightManeuveringEngineFlame.Clear();
                    m_effects.rightManeuveringEngineFlame.Pause();
                }
            }

            if (m_effects.leftLateralEngineFlame != null)
            {
                if (m_position.longitudinal.input < 0)
                {
                    m_effects.leftLateralEngineFlame.Play();
                }
                else
                {
                    m_effects.leftLateralEngineFlame.Pause();
                    m_effects.leftLateralEngineFlame.Clear();
                }
            }

            if (m_effects.rightLateralEngineFlame != null)
            {
                if (m_position.longitudinal.input > 0)
                {
                    m_effects.rightLateralEngineFlame.Play();
                }
                else
                {
                    m_effects.rightLateralEngineFlame.Pause();
                    m_effects.rightLateralEngineFlame.Clear();
                }
            }

            if (m_effects.bottomAltitudeEngineFlame != null)
            {
                if (m_position.vertical.input < 0)
                {
                    m_effects.bottomAltitudeEngineFlame.Play();
                }
                else
                {
                    m_effects.bottomAltitudeEngineFlame.Pause();
                    m_effects.bottomAltitudeEngineFlame.Clear();
                }
            }

            if (m_effects.topAltitudeEngineFlame != null)
            {
                if (m_position.vertical.input > 0)
                {
                    m_effects.topAltitudeEngineFlame.Play();
                }
                else
                {
                    m_effects.topAltitudeEngineFlame.Pause();
                    m_effects.topAltitudeEngineFlame.Clear();
                }
            }

            if (m_effects.reverseEngineFlame != null)
            {
                if (m_position.longitudinal.input < 0)
                {
                    m_effects.reverseEngineFlame.Play();
                }
                else
                {
                    m_effects.reverseEngineFlame.Pause();
                    m_effects.reverseEngineFlame.Clear();
                }
            }
        }

        private void DeactivateAllEffects()
        {
            if (m_effects.mainEngineFlame != null)
            {
                m_effects.mainEngineFlame.Pause();
                m_effects.mainEngineFlame.Clear();
            }
            if (m_effects.leftManeuveringEngineFlame != null)
            {
                m_effects.leftManeuveringEngineFlame.Pause();
                m_effects.leftManeuveringEngineFlame.Clear();
            }
            if (m_effects.rightManeuveringEngineFlame != null)
            {
                m_effects.rightManeuveringEngineFlame.Pause();
                m_effects.rightManeuveringEngineFlame.Clear();
            }
            if (m_effects.leftLateralEngineFlame != null)
            {
                m_effects.leftLateralEngineFlame.Pause();
                m_effects.leftLateralEngineFlame.Clear();
            }
            if (m_effects.rightLateralEngineFlame != null)
            {
                m_effects.rightLateralEngineFlame.Pause();
                m_effects.rightLateralEngineFlame.Clear();
            }
            if (m_effects.bottomAltitudeEngineFlame != null)
            {
                m_effects.bottomAltitudeEngineFlame.Pause();
                m_effects.bottomAltitudeEngineFlame.Clear();
            }
            if (m_effects.topAltitudeEngineFlame != null)
            {
                m_effects.topAltitudeEngineFlame.Pause();
                m_effects.topAltitudeEngineFlame.Clear();
            }
            if (m_effects.reverseEngineFlame != null)
            {
                m_effects.reverseEngineFlame.Pause();
                m_effects.reverseEngineFlame.Clear();
            }
        }
    }
}