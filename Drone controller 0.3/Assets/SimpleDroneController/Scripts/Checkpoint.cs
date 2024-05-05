using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDroneController
{
    public class Checkpoint : MonoBehaviour
    {
        private LevelManager levelManager;
        

        public void SetLevelManager(LevelManager p_levelManager)
        {
            levelManager = p_levelManager;
        }

        void OnTriggerEnter(Collider other)
        {
            if (levelManager == null) return;

            if (other.tag == "Player")
            {
                levelManager.CheckpointPassed();
                gameObject.SetActive(false);

                //raceManager.onCheckPoint = true;

                //raceManager.checkPointAttributes.currentCount = raceManager.checkPointAttributes.currentCount + 1;

                //gameObject.SetActive(false);

                //raceManager.CheckPointSound();
            }
        }
    }
}

