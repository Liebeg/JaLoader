﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JaLoader
{
    public class CustomObjectsManager : MonoBehaviour
    {
        #region Singleton
        public static CustomObjectsManager Instance { get; private set; }

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

            EventsManager.Instance.OnSave += OnSave;
            EventsManager.Instance.OnMenuLoad += OnMenuLoad;
            EventsManager.Instance.OnGameLoad += OnGameLoad;
            EventsManager.Instance.OnNewGame += DeleteData;
        }
        #endregion

        [SerializeField] private CustomObjectSave data = new CustomObjectSave();

        public readonly Dictionary<string, GameObject> database = new Dictionary<string, GameObject>();
        private Dictionary<(string, int), GameObject> spawnedDatabase = new Dictionary<(string, int), GameObject>();

        // TODO: Add custom wheels support
        private List<Transform> bootSlots = new List<Transform>();
        private GameObject boot;

        private int currentFreeID = 0;
        private bool allObjectsRegistered;

        private SettingsManager settingsManager = SettingsManager.Instance;
        public bool ignoreAlreadyExists;

        private void Update()
        {
            if (!settingsManager.DebugMode)
                return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    Console.Instance.LogDebug("JaLoader", "Saved custom objects status!");
                    SaveData();
                }
                else if (Input.GetKeyDown(KeyCode.L))
                {
                    Console.Instance.LogDebug("JaLoader", "Loaded custom objects status!");
                    LoadData(true);
                }
            }
        }

        private void Start()
        {
            if (!allObjectsRegistered)
                StartCoroutine(WaitUntilLoadFinished());
        }

        private void OnSave()
        {
            data = new CustomObjectSave();

            SaveData();
        }

        private void OnMenuLoad()
        {
            StartCoroutine(LoadDelay(0.01f, false));
        }

        private void OnGameLoad()
        {
            if (bootSlots == null)
                bootSlots = new List<Transform>();
            else
                bootSlots.Clear();

            var bootObjects = FindObjectsOfType<InventoryLogicC>();

            for (int i = 0; i < bootObjects.Length; i++)
            {
                if (bootObjects[i].gameObject.name == "Boot" && bootObjects[i].transform.parent.parent.parent.name == "FrameHolder")
                {
                    boot = bootObjects[i].gameObject;
                    break;
                }
            }

            for (int i = 0; i < boot.transform.GetComponentsInChildren<InventoryRelayC>().Length; i++)
            {
                bootSlots.Add(boot.transform.GetComponentsInChildren<InventoryRelayC>()[i].transform);
            }

            ModHelper.Instance.RefreshPartHolders();

            StartCoroutine(LoadDelay(1f, true));
        }

        /// <summary>
        /// Register a custom object to the database.
        /// </summary>
        /// <param name="obj">The object in question</param>
        /// <param name="registryName">Internal object name</param>
        public void RegisterObject(GameObject obj, string registryName)
        {
            if (obj == null)
            {
                if (!ignoreAlreadyExists)
                {
                    Console.Instance.LogError("CustomObjectsManager", "The object you're trying to register is null!");
                }
                
                return;
            }

            if (database.ContainsKey(registryName))
            {
                if (!ignoreAlreadyExists)
                {
                    Console.Instance.LogError("CustomObjectsManager", $"An object with the registry key {registryName} already exists! Please pick another one.");
                }
                return;
            }

            obj.SetActive(false);
            obj.GetComponent<CustomObjectInfo>().objRegistryName = registryName;

            database.Add($"{registryName}", obj);
            DontDestroyOnLoad(obj);
        }

        /// <summary>
        /// Get the object from the database.
        /// </summary>
        /// <param name="registryName">Internal object name</param>
        /// <returns></returns>
        public GameObject GetObject(string registryName)
        {
            if (database.ContainsKey(registryName))
                return database[registryName];

            return null;
        }

        /// <summary>
        /// Get the registry name from an object.
        /// </summary>
        /// <param name="obj">The object in question</param>
        /// <returns></returns>
        public string GetRegistryNameByObject(GameObject obj)
        {
            foreach (var entry in database)
            {
                if (entry.Value == obj)
                    return entry.Key;
            }

            return null;
        }

        /// <summary>
        /// Spawn an object from the database.
        /// </summary>
        /// <param name="registryName">Internal object name</param>
        /// <returns></returns>
        public GameObject SpawnObject(string registryName)
        {
            GameObject objToSpawn = GetObject(registryName);

            if(objToSpawn == null) return null;

            GameObject spawnedObj = Instantiate(objToSpawn);
            SceneManager.MoveGameObjectToScene(spawnedObj, SceneManager.GetActiveScene());
            spawnedObj.SetActive(true);

            IncrementID(registryName, spawnedObj);

            return spawnedObj;
        }

        public void OverwriteObject(string registryName, GameObject obj)
        {
            if (database.ContainsKey(registryName))
            {
                database[registryName] = obj;
            }
        }

        public bool HasObjectBeenSpawned(GameObject obj)
        {
            foreach (var entry in spawnedDatabase)
            {
                if (entry.Value == obj)
                    return true;
            }

            return false;
        }

        public void AddObjectToSpawned(GameObject obj, string name)
        {
            if (HasObjectBeenSpawned(obj))
                return;

            currentFreeID++;
            spawnedDatabase.Add((name, currentFreeID), obj);
        }

        /// <summary>
        /// Spawn an object from the database at a specific position.
        /// </summary>
        /// <param name="registryName">Internal object name</param>
        /// <param name="position">The position you'd like to spawn it at</param>
        /// <returns></returns>
        public GameObject SpawnObject(string registryName, Vector3 position)
        {
            GameObject objToSpawn = GetObject(registryName);

            if (objToSpawn == null) return null;

            GameObject spawnedObj = Instantiate(objToSpawn);
            SceneManager.MoveGameObjectToScene(spawnedObj, SceneManager.GetActiveScene());
            spawnedObj.transform.position = position;
            spawnedObj.GetComponent<CustomObjectInfo>().SpawnNoRegister = true;
            spawnedObj.SetActive(true);

            IncrementID(registryName, spawnedObj);

            return spawnedObj;
        }

        /// <summary>
        /// Remove an object from the database.
        /// </summary>
        /// <param name="registryName">Internal object name</param>
        public void DeleteObject(string registryName)
        {
            DestroyImmediate(spawnedDatabase[(registryName, currentFreeID)]);
            spawnedDatabase.Remove((registryName, currentFreeID));
            currentFreeID--;
        }

        public GameObject SpawnObjectWithoutRegistering(string registryName, Vector3 pos, Vector3 rot, bool enableObject)
        {
            GameObject objToSpawn = GetObject(registryName);

            if (objToSpawn == null) return null;

            GameObject spawnedObj = Instantiate(objToSpawn);
            SceneManager.MoveGameObjectToScene(spawnedObj, SceneManager.GetActiveScene());
            spawnedObj.transform.position = pos;
            spawnedObj.transform.eulerAngles = rot;
            spawnedObj.GetComponent<CustomObjectInfo>().SpawnNoRegister = true;
            spawnedObj.SetActive(enableObject);

            return spawnedObj;
        }

        /// <summary>
        /// Spawn an object from the database at a specific position and rotation.
        /// </summary>
        /// <param name="registryName">Internal object name</param>
        /// <param name="position">The position you'd like to spawn it at</param>
        /// <param name="rotation">The rotation you'd like to spawn it at</param>
        /// <returns></returns>
        public GameObject SpawnObject(string registryName, Vector3 position, Quaternion rotation)
        {
            GameObject objToSpawn = GetObject(registryName);

            if (objToSpawn == null) return null;

            GameObject spawnedObj = Instantiate(objToSpawn);
            SceneManager.MoveGameObjectToScene(spawnedObj, SceneManager.GetActiveScene());
            spawnedObj.transform.position = position;
            spawnedObj.transform.rotation = rotation;
            spawnedObj.GetComponent<CustomObjectInfo>().SpawnNoRegister = true;
            spawnedObj.SetActive(true);

            IncrementID(registryName, spawnedObj);

            return spawnedObj;
        }

        private void IncrementID(string registryName, GameObject obj)
        {
            currentFreeID++;
            spawnedDatabase.Add((registryName, currentFreeID), obj);
        }

        public void SaveData()
        {
            data.Clear();

            foreach (var entry in spawnedDatabase.Keys)
            {
                if (spawnedDatabase[entry] == null || !spawnedDatabase[entry].GetComponent<ObjectPickupC>() || spawnedDatabase[entry].GetComponent<ExtraComponentC_ModExtension>())
                    continue;
                List<float> parameters = new List<float>();
                PartTypes type = PartTypes.Default;
                bool inEngine = spawnedDatabase[entry].GetComponent<ObjectPickupC>().isInEngine;
                string json = "";
                Vector3 trunkPos = Vector3.zero;
                if (!inEngine && spawnedDatabase[entry].GetComponent<ObjectPickupC>().inventoryPlacedAt == null)
                    continue;

                if (spawnedDatabase[entry].GetComponent<EngineComponentC>())
                {
                    parameters.Add(spawnedDatabase[entry].GetComponent<EngineComponentC>().condition);

                    switch (spawnedDatabase[entry].GetComponent<ObjectPickupC>().engineString)
                    {
                        case "EngineBlock":
                            type = PartTypes.Engine;
                            break;

                        case "FuelTank":
                            type = PartTypes.FuelTank;
                            break;

                        case "Carburettor":
                            type = PartTypes.Carburettor;
                            break;

                        case "AirFilter":
                            type = PartTypes.AirFilter;
                            break;

                        case "IgnitionCoil":
                            type = PartTypes.IgnitionCoil;
                            break;

                        case "Battery":
                            type = PartTypes.Battery;
                            break;

                        case "WaterContainer":
                            type = PartTypes.WaterTank;
                            break;

                        default:
                            continue;
                    }
                }

                if(!inEngine)
                    trunkPos = spawnedDatabase[entry].GetComponent<ObjectPickupC>().inventoryPlacedAt.localPosition;

                json = TupleToString((inEngine, type, parameters.ToArray(), trunkPos));
                StringToTuple(json);

                string name = $"{entry.Item1}_{entry.Item2}";
                
                data.Add(name, json);
            }

            File.WriteAllText(Path.Combine(Application.persistentDataPath, @"CustomObjectsData.json"), JsonUtility.ToJson(data, true));
        }

        public void LoadData(bool full)
        {
            if (database.Count == 0 || database == null)
                return;

            spawnedDatabase.Clear();

            currentFreeID = 0;

            if (File.Exists(Path.Combine(Application.persistentDataPath, @"CustomObjectsData.json")))
            {
                string json = File.ReadAllText(Path.Combine(Application.persistentDataPath, @"CustomObjectsData.json"));
                data = JsonUtility.FromJson<CustomObjectSave>(json);

                foreach (string entry in data.Keys)
                {
                    string name = entry.Split('_')[0];
                    string id = entry.Split('_')[1];

                    if (!database.ContainsKey(name))
                        continue;

                    (bool, PartTypes, float[], Vector3) tuple = StringToTuple(data[entry]);
                    GameObject obj = SpawnObject(name, Vector3.zero, Quaternion.identity);
                    obj.GetComponent<Rigidbody>().isKinematic = true;
                    obj.GetComponent<Collider>().isTrigger = true;
                    if (obj.GetComponent<EngineComponentC>())
                    {
                        obj.GetComponent<EngineComponentC>().condition = tuple.Item3[0];
                    }

                    #region Load In Engine
                    if (tuple.Item1.Equals(true))
                    {
                        switch (tuple.Item2)
                        {
                            case PartTypes.Engine:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.Engine];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.FuelTank:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.FuelTank];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.Carburettor:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.Carburettor];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.AirFilter:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.AirFilter];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.IgnitionCoil:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.IgnitionCoil];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.Battery:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.Battery];
                                PlaceObjectInEngine(obj);
                                break;

                            case PartTypes.WaterTank:
                                obj.transform.parent = ModHelper.Instance.partHolders[PartTypes.WaterTank];
                                PlaceObjectInEngine(obj);
                                break;
                        }
                    }
                    #endregion
                    else
                    {
                        if (!full)
                            continue;

                        for (int i = 0; i < bootSlots.Count; i++)
                        {
                            if (bootSlots[i].localPosition == tuple.Item4)
                            {
                                bootSlots[i].GetComponent<InventoryRelayC>().Occupy();
                                obj.transform.parent = bootSlots[i].transform;
                                obj.GetComponent<ObjectPickupC>().inventoryPlacedAt = bootSlots[i].transform;
                                obj.transform.localPosition = obj.GetComponent<ObjectPickupC>().inventoryAdjustPosition;
                                obj.transform.localEulerAngles = obj.GetComponent<ObjectPickupC>().inventoryAdjustRotation;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void DeleteData()
        {
            File.Delete(Path.Combine(Application.persistentDataPath, @"CustomObjectsData.json"));

            if (spawnedDatabase != null)
                spawnedDatabase.Clear();
            else
                spawnedDatabase = new Dictionary<(string, int), GameObject>();

            currentFreeID = 0;
        }

        private void PlaceObjectInEngine(GameObject obj)
        {
            if (obj.transform.parent.GetComponent<HoldingLogicC>().isOccupied)
            {
                obj.transform.parent.GetComponent<HoldingLogicC>().isOccupied = false;
                obj.transform.parent.GetComponent<Collider>().enabled = true;
                Destroy(obj.transform.parent.GetChild(0).gameObject);
            }

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.GetComponent<ObjectPickupC>().isInEngine = true;
            obj.GetComponent<ObjectPickupC>().placedAt = obj.transform.parent.gameObject;
            obj.transform.parent.GetComponent<HoldingLogicC>().isOccupied = true;
            obj.transform.parent.GetComponent<Collider>().enabled = false;

            obj.SendMessage("SendStatsToCarPerf");
        }

        private static string TupleToString((bool, PartTypes, float[], Vector3) point)
        {
            string arrayStr = "";

            for (int i = 0; i < point.Item3.Length; i++)
            {
                if (i == point.Item3.Length - 1)
                {
                    arrayStr += $"{point.Item3[i]}";
                }
                else
                {
                    arrayStr += $"{point.Item3[i]} ";
                }
            }

            string str = $"{point.Item1}|{(int)point.Item2}|{arrayStr}|{point.Item4.x}|{point.Item4.y}|{point.Item4.z}";

            return str;
        }

        // isInEngine (if false then it's in trunk) | PartType | other parameters (condition, fuel level etc) | trunk position
        private static (bool, PartTypes, float[], Vector3) StringToTuple(string str)
        {
            string[] param = str.Split('|');
            string[] floatParam = param[2].Split();

            bool inEngine = bool.Parse(param[0]);
            PartTypes partType = (PartTypes)int.Parse(param[1]);
            float[] floatArrayParam = new float[floatParam.Length];
            Vector3 vector3 = new Vector3(float.Parse(param[3]), float.Parse(param[4]), float.Parse(param[5]));

            for (int i = 0; i < floatParam.Length; i++)
            {
                floatArrayParam[i] = float.Parse(floatParam[i]);
            }

            return (inEngine, partType, floatArrayParam, vector3);
        }

        private IEnumerator WaitUntilLoadFinished()
        {
            while (!ModLoader.Instance.finishedInitializingPartTwoMods)
                yield return null;

            allObjectsRegistered = true;
            EventsManager.Instance.OnCustomObjectsRegisterFinish();

            LoadData(false);

            yield return null;
        }

        private IEnumerator LoadDelay(float seconds, bool fullLoad)
        {
            ModHelper.Instance.RefreshPartHolders();
            
            yield return new WaitForSeconds(seconds);

            LoadData(fullLoad);
            if(fullLoad)
                LaikaCatalogueExtension.Instance.AddPages("", "", 0);
        }
    }


    [Serializable]
    public class CustomObjectSave : SerializableDictionary<string, string> { };
}