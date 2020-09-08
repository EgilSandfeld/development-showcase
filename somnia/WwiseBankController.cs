using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Loads and unloads Wwise soundbanks, optimizes loading when multiple sound objects request load of a soundbank
/// Possible to quickly "Log" what's happening from Unity editor
/// </summary>
public class WwiseBankController : MonoBehaviour
{
    #region Variables

    public static WwiseBankController Instance;

    [SerializeField] private bool Log;
    [SerializeField] private float generalFadeTime = 3f;
    private List<WwiseSoundBankData> banksLoaded = new List<WwiseSoundBankData>();

    #endregion

    #region Init

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Public methods

    public bool AreAllBanksLoaded()
    {
        for (int i = 0; i < banksLoaded.Count; i++)
        {
            if (banksLoaded[i].BankStatus != WwiseBankLoadStatus.LOADED)
                return false;
        }

        return true;
    }

    public async Task<WwiseSoundBankData> LoadBank(string bankName, bool decodeBankIfAdded, bool incrementIfLoaded = true, bool log = false)
    {
        WwiseSoundBankData wsbd = banksLoaded.FirstOrDefault(x => x.BankName == bankName);
        if (wsbd == null)
        {
            WwiseBankLoader loader = gameObject.AddComponent<WwiseBankLoader>();
            loader.Log = log ? true : (Log ? true : false);
            wsbd = new WwiseSoundBankData(loader, decodeBankIfAdded, bankName, 1);
            banksLoaded.Add(wsbd);
            await loader.Load(wsbd);

            if (wsbd.BankStatus == WwiseBankLoadStatus.LOADED)
            {
                if (log)
                    Debug.Log("Created " + bankName + " in banks: 1 using it");
            }
            else
                Debug.LogWarning(bankName + " not created as status: " + wsbd.BankStatus);
        }
        else
        {
            if (wsbd.BankStatus == WwiseBankLoadStatus.UNLOADED)
                await wsbd.Loader.Load(wsbd);

            while (wsbd.BankStatus == WwiseBankLoadStatus.LOADING)
                await Task.Yield();

            if (incrementIfLoaded)
                wsbd.ReferenceCount++;
            
            if (Log)
                Debug.Log("Added " + bankName + " in banks: " + wsbd.ReferenceCount + " using it");
        }
        return wsbd;
    }

    public void UnloadBank(string bankName, bool decrement = true, int waitBeforeUnload = 0)
    {
        WwiseSoundBankData wsbd = banksLoaded.FirstOrDefault(x => x.BankName == bankName && x.BankStatus == WwiseBankLoadStatus.LOADED);
        if (wsbd != null)
            UnloadBank(wsbd, decrement, waitBeforeUnload);
        else if (decrement)
            Debug.LogError("Could not remove soundbank: " + bankName + " because it doesn't exist in banksLoaded");
    }

    public async void UnloadBank(WwiseSoundBankData wsbd, bool decrement = true, int waitBeforeUnload = 0)
    {
        if (waitBeforeUnload > 0)
            await Task.Delay(waitBeforeUnload * 1000);

        if (decrement)
            wsbd.ReferenceCount--;

        if (wsbd.ReferenceCount > 0)
        {
            if (Log)
                Debug.Log("One less aubit using " + wsbd.BankName + ". Now: " + wsbd.ReferenceCount);
        }
        else
        {
            if (Log)
                Debug.Log("No aubits using, so unload: " + wsbd.BankName);

            banksLoaded.Remove(wsbd);
            wsbd.Loader.Unload();
        }
    }

    #endregion
}

/// <summary>
/// Wrapper for holding soundbank info and a count of aubits using it
/// </summary>
[Serializable]
public class WwiseSoundBankData
{
    public string BankName;
    public bool Decode;
    public WwiseBankLoader Loader;
    public WwiseBankLoadStatus BankStatus = WwiseBankLoadStatus.UNLOADED;
    public int ReferenceCount;
    public IntPtr MemoryBankPtr;

    public WwiseSoundBankData(WwiseBankLoader _loader, bool decode, string _bankName, int _soundsUsingThisBank)
    {
        Loader = _loader;
        Decode = decode;
        BankName = _bankName;
        ReferenceCount = _soundsUsingThisBank;
    }
}
