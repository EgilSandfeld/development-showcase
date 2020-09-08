using AWE.Core;
using NaughtyAttributes;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Foundational scriptable object serialization system that takes care of serializing to and from persistent data folder
/// Persistence may be disabled using !Persist for Unity authored files only
/// Class is abstract to require extensibility
/// The script utilizes <see href="https://github.com/dbrizov/NaughtyAttributes">NaughtyAttributes</see> for quick Unity inspector structure
/// </summary>
public abstract class BaseSO : ScriptableObject
{
    #region Variables

    public static bool CheckedPersistentDataPathExists;

    [Header("Persistence")]
    public bool Log;
    public bool Persist = true;
    [ShowIf("Persist")] public string PersistenceSubFolder = "PersistentData/";
    [ShowIf("Persist")] public string Directory;
    [ShowIf("Persist")] public string FilePath;

    private bool loaded;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        loaded = false;
    }

    /// <summary>
    /// See ScripableObject behaviours: https://goo.gl/HMWbUX
    /// </summary>
    private void OnEnable()
    {
        if (Application.isEditor)
        {
            Directory = "";
            FilePath = "";
            GetFilePath();

            if (!Application.isPlaying)
                Load();
        }
    }


    /// <summary>
    /// See ScripableObject behaviours: https://goo.gl/HMWbUX
    /// </summary>
    private void OnDisable()
    {
        if (loaded)
        {
            SaveToPersistence();
            loaded = false;
            PersistenceManager.OnDataReset -= ResetSettings;
    #if UNITY_EDITOR
            PersistenceManager.OnDataReset -= ResetFilePath;
    #endif
        }
    }

    #endregion

    #region Serialization

    [Button]
    public void SaveToPersistence()
    {
        if (!Persist)
            return;

        if (Log)
            Debug.Log(name + " SaveToPersistence");

        if (!System.IO.Directory.Exists(GetDirectoryPath()))
        {
            Directory = "";
            System.IO.Directory.CreateDirectory(GetDirectoryPath());
        }

        bool loadedBefore = loaded;
        loaded = false;
        string json = JsonUtility.ToJson(this, true);
        try
        {
            File.WriteAllText(GetFilePath(), json);
        }
        catch (System.Exception)
        {
            FilePath = "";
            Directory = "";
            //Debug.LogWarning("Resetting FilePath before saving to: " + GetFilePath());
            File.WriteAllText(GetFilePath(), json);
        }
        
        loaded = loadedBefore;
    }

    public void Load(bool force = false)
    {
        if (loaded && !force)
        {
            if (Log)
                Debug.Log(name + " already Loaded");

            return;
        }
        else 
        {
            if (!force)
            {
                FilePath = "";
                Directory = "";    
            }
            
            if (Log)
                Debug.Log(name + " Load. Forced: " + force);
        }

        CheckPersistentDataPath();

        LoadFromPersistence();

        if (string.IsNullOrEmpty(Directory))
            Directory = GetDirectoryPath();

        PersistenceManager.OnDataReset += ResetSettings;
#if UNITY_EDITOR
        PersistenceManager.OnDataReset += ResetFilePath;
#endif
    }

    [Button]
    public void LoadFromPersistence()
    {
        if (!Persist)
        {
            loaded = true;
            return;
        }

        if (Log)
            Debug.Log(name + " LoadFromPersistence");

        bool log = Log;

        //Try loading persistent data
        if (File.Exists(GetFilePath()))
        {
            var json = File.ReadAllText(GetFilePath());
            JsonUtility.FromJsonOverwrite(json, this);
        }

        Log = log;
        loaded = true;
    }

    [Button("Reset Data")]
    public abstract void ResetSettings();

    #endregion

    #region File Location

    [Button]
    public void ShowPersistentFile()
    {
#if UNITY_EDITOR
        EditorUtility.RevealInFinder(GetFilePath());
#endif
    }

    private string GetDirectoryPath()
    {
        if (string.IsNullOrEmpty(Directory))
            Directory = Path.Combine(Application.persistentDataPath, PersistenceSubFolder);

        return Directory;
    }

    public string GetFilePath()
    {
        if (string.IsNullOrEmpty(FilePath))
            FilePath = Path.Combine(GetDirectoryPath(), name.ToLower().Replace(" ", "_") + ".json");

        return FilePath;
    }

    private void ResetFilePath()
    {
        Directory = "";
        FilePath = "";
    }

    private static void CheckPersistentDataPath()
    {
        if (!CheckedPersistentDataPathExists)
        {
            if (!System.IO.Directory.Exists(Path.Combine(Application.persistentDataPath, "PersistentData")))
                System.IO.Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "PersistentData"));

            CheckedPersistentDataPathExists = true;
        }
    }

    #endregion
}
