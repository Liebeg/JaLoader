﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JaLoader
{
    public class UncleHelper : MonoBehaviour
    {
        public static UncleHelper Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        public bool UncleEnabled = !SettingsManager.Instance.DisableUncle;
        public GameObject Uncle;

        public void OnSceneChange(Scene current, Scene next)
        {
            if(SceneManager.GetActiveScene().buildIndex == 3) 
            {
                Uncle = FindObjectOfType<UncleLogicC>().gameObject;

                Uncle.SetActive(UncleEnabled);
            }
        }

        private void DisableUncle()
        {
            UncleEnabled = false;
            SettingsManager.Instance.DisableUncle = true;

            Uncle.SetActive(false);
        }

        private void EnableUncle()
        {
            UncleEnabled = transform;
            SettingsManager.Instance.DisableUncle = false;

            Uncle.SetActive(true);
        }

        private void Talk(string message)
        {

        }
    }
}
