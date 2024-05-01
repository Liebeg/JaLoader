﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JaLoader
{
    public class GameTweaks : MonoBehaviour
    {
        #region Singleton
        public static GameTweaks Instance { get; private set; }

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

            settingsManager = SettingsManager.Instance;

            EventsManager.Instance.OnGameLoad += OnGameLoad;
            EventsManager.Instance.OnRouteGenerated += OnRouteGenerated;
        }

        #endregion

        private SettingsManager settingsManager;

        public bool ResettingPickingUp = false;

        private void OnGameLoad()
        {
            Invoke("UpdateMirrors", 3);

            if (settingsManager.FixLaikaShopMusic)
                Invoke("FixLaikaDealershipSong", 5);
        }

        public void ResetPickingUp()
        {
            ResettingPickingUp = false;
            FindObjectOfType<DragRigidbodyC>().pickingUp = false;
        }

        private void OnRouteGenerated(string startLocation, string endLocation, int distance)
        {
            if (settingsManager.FixLaikaShopMusic)
                Invoke("FixLaikaDealershipSong", 5);
        }

        private void FixLaikaDealershipSong()
        {
            var laikabuildings = FindObjectsOfType<LaikaBuildingC>();

            if(laikabuildings == null)
                return;

            foreach (var laikabuilding in laikabuildings)
            {
                laikabuilding.transform.Find("speakers_shop_01").GetComponent<AudioSource>().loop = true;

                if(!laikabuilding.transform.Find("speakers_shop_01").GetComponent<AudioSource>().isPlaying)
                    laikabuilding.transform.Find("speakers_shop_01").GetComponent<AudioSource>().Play();
            }
        }   

        public void UpdateMirrors()
        {
            UpdateMirrors(settingsManager.MirrorDistances);
        }

        public void UpdateMirrors(MirrorDistances value)
        {
            var mirrors = FindObjectsOfType<MirrorReflection>();

            if(mirrors == null)
                return;

            int distance = 0;

            switch(value)
            {
                case MirrorDistances.m250:
                    distance = 250;
                    break;

                case MirrorDistances.m500:
                    distance = 500;
                    break;

                case MirrorDistances.m750:
                    distance = 750;
                    break;

                case MirrorDistances.m1000:
                    distance = 1000;
                    break;

                case MirrorDistances.m1500:
                    distance = 1500;
                    break;

                case MirrorDistances.m2000:
                    distance = 2000;
                    break;

                case MirrorDistances.m2500:
                    distance = 2500;
                    break;

                case MirrorDistances.m3000:
                    distance = 3000;
                    break;

                case MirrorDistances.m3500:
                    distance = 3500;
                    break;

                case MirrorDistances.m4000:
                    distance = 4000;
                    break;

                case MirrorDistances.m5000:
                    distance = 5000;
                    break;
            }

            foreach (MirrorReflection mirror in mirrors)
            {
                mirror.m_FarClipDistance = distance;
            }
        }
    }
}
