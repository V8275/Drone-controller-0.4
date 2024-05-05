using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditorInternal;

namespace SimpleDroneController
{
    public class LevelManager : MonoBehaviour
    {

        public enum State
        {
            None,
            LevelStarted,
            InGame,
            Pause,
            UnPause,
            CheckpointPassed,
            LevelComplete
        }

        public State currentState;

        public GameObject inGameUI;
        public GameObject pauseUI;
        public GameObject levelCompleteUI;

        [SerializeField] bool isActive = true;
        [SerializeField] DroneController droneController;
        [SerializeField] DroneCamera droneCamera;
        [SerializeField] bool showHelpUI = true;
        [SerializeField] Text timerText;
        [SerializeField] Text controlsText;
        [SerializeField] Checkpoint checkpointPrefab;
        [SerializeField] Transform checkpointSpawns;
        [SerializeField] Text checkpointText;
        [SerializeField] int checkpointsPassed = 0;
        [SerializeField] int totalCheckpoints = 0;
        [SerializeField] KeyCode pauseKey = KeyCode.P;
        [SerializeField] AudioSource checkpointSFX;
        [SerializeField] AudioSource levelCompleteSFX;

        float timer;

        void Start()
        {
            SetState(State.LevelStarted);
        }

        void Update()
        {
            if (currentState == State.InGame || currentState == State.CheckpointPassed)
            {
                UpdateTimerText();
                CheckForPause();
            }

            if (currentState == State.LevelComplete)
            {
                CheckForRestart();
            }
        }

        private void SetState(State p_newState)
        {
            if (!isActive) return;

            currentState = p_newState;

            if (currentState == State.LevelStarted)
            {
                SpawnCheckpoints();
                UpdateCheckpointText();
                checkpointsPassed = 0;
                SetTimer(0f);
                SetTimeScale(1);
                SetState(State.InGame);
                UpdateControlsText();

                if (droneController != null)
                    droneController.SetEngineState(true);
            }

            if (currentState == State.InGame)
            {
                ShowUI(true, false, false);
            }

            if (currentState == State.Pause)
            {
                ShowUI(false, false, true);
                SetTimeScale(0);
            }

            if (currentState == State.UnPause)
            {
                SetTimeScale(1);
                SetState(State.InGame);
            }

            if (currentState == State.CheckpointPassed)
            {
                checkpointsPassed++;
                UpdateCheckpointText();
                PlayCheckpointSFX();

                if (IsLevelComplete())
                {
                    SetState(State.LevelComplete);
                    return;
                }
                    

                SetState(State.InGame);
            }

            if (currentState == State.LevelComplete)
            {
                ShowUI(false, true, false);
                PlayLevelCompleteSFX();

                if (droneController != null)
                    droneController.SetEngineState(false);
            }
        }

        private void UpdateControlsText()
        {
            if (droneController == null) return;
            if (controlsText == null) return;
            if (droneCamera == null) return;
            if (!showHelpUI) return;

            string controls = "";
            controls = controls + "Forward: " + droneController.m_position.longitudinal.forwardKey;
            controls = controls + "\n" + "Backward: " + droneController.m_position.longitudinal.backwardKey;
            controls = controls + "\n" + "Left: " + droneController.m_position.lateral.leftKey;
            controls = controls + "\n" + "Right: " + droneController.m_position.lateral.rightKey;
            controls = controls + "\n" + "Up: " + droneController.m_position.vertical.upKey;
            controls = controls + "\n" + "Down: " + droneController.m_position.vertical.downKey;
            controls = controls + "\n" + "Rotate Right: " + droneController.m_rotation.yaw.rightKey;
            controls = controls + "\n" + "Rotate Left: " + droneController.m_rotation.yaw.leftKey;
            controls = controls +"\n" + "Barrel Roll: " + droneController.m_visual.roll.key;
            controls = controls + "\n" + "Third Person Camera: " + droneCamera.thirdPersonKey;
            controls = controls + "\n" + "First Person Camera: " + droneCamera.firstPersonKey;
            controls = controls + "\n" + "Pause: " + pauseKey;

            controlsText.text = controls;
        }

        private void SpawnCheckpoints()
        {
            if (checkpointSpawns == null) return;
            if (checkpointPrefab == null) return;

            Transform[] spawnPoints = checkpointSpawns.GetComponentsInChildren<Transform>(false);

            if (spawnPoints == null) return;
            if (spawnPoints.Length  == 0) return;

            foreach (Transform spawn in spawnPoints)
            {
                Checkpoint newCheckpoint = Instantiate(checkpointPrefab);

                newCheckpoint.transform.position = spawn.position;
                newCheckpoint.transform.rotation = spawn.rotation;
                newCheckpoint.transform.SetParent(checkpointSpawns);
                newCheckpoint.SetLevelManager(this);
            }

            totalCheckpoints = spawnPoints.Length;
        }

        private void PlayCheckpointSFX()
        {
            if (checkpointSFX == null) return;

            checkpointSFX.Play();
        }

        private void PlayLevelCompleteSFX()
        {
            if (levelCompleteSFX == null) return;

            levelCompleteSFX.Play();
        }

        private bool IsLevelComplete()
        {
            return checkpointsPassed >= totalCheckpoints;
        }
        private void SetTimer(float p_timer)
        {
            if (timer != p_timer)
                timer = p_timer;
        }

        private void UpdateTimerText()
        {
            if (timerText == null) return;

            timer += Time.deltaTime;
            System.TimeSpan t = System.TimeSpan.FromSeconds(timer);
            timerText.text = "Time: " + string.Format("{0,1:0}:{1,2:00}", t.Minutes, t.Seconds);
        }

        private void UpdateCheckpointText()
        {
            if (checkpointText == null) return;

            checkpointText.text = "Checkpoints: " + checkpointsPassed.ToString() + "/" + totalCheckpoints.ToString();
        }

        private void CheckForPause()
        {
            if (Input.GetKeyDown(pauseKey) && currentState != State.Pause)
                SetState(State.Pause);
        }

        public void CheckpointPassed()
        {
            SetState(State.CheckpointPassed);
        }

        private void SetTimeScale(float p_timeScale)
        {
            if (Time.timeScale != p_timeScale)
                Time.timeScale = p_timeScale;
        }


        private void CheckForRestart()
        {
            if (Input.GetKey(KeyCode.R))
            {
                Restart();
            }
        }

        private void ShowUI(bool p_inGameUI, bool p_levelCompleteUI, bool p_pauseUI)
        {
            if (pauseUI == null || levelCompleteUI == null || inGameUI == null)
                return;

            pauseUI.SetActive(p_pauseUI);
            levelCompleteUI.SetActive(p_levelCompleteUI);
            inGameUI.SetActive(p_inGameUI);
        }

        public void OnClickUnPause()
        {
            SetState(State.UnPause);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            SetState(State.LevelStarted);
        }


    }
}

