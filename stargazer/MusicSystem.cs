using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicSystem : MonoBehaviour 
{
    #region Variables

    public static MusicSystem Instance;
    public static System.Action OnEnabled;
    public static System.Action<float> OnSuck;
    public static System.Action<CurveType, float, float> OnPulse;
    public static System.Action<float> OnBeat;
    public static System.Action<float> OnBar;
    public static System.Action<MusicTime> OnDivision;



    [Space]
    [Header("Configuration")]
    [SerializeField] private SO_PlayerData playerData;
    [SerializeField] private AnimationCurve curveSine;
    [SerializeField] private AnimationCurve curveSaw;
    [SerializeField] private List<SO_Song> songs = new List<SO_Song>();
    [SerializeField] private SO_Song forceSong;
    [SerializeField] private int division = 4;
    [Tooltip("Initial number of stars that must be connected in succession before music segment increments")] [SerializeField] private int StarsToConnectBeforeNewMusicSegment = 8;
    [SerializeField] private int maxSuckForN = 4;
    [SerializeField] private bool Log;



    [Space]
    [Header("Read Only")]
    [Tooltip("Song currently playing")] [SerializeField] private SO_Song currentSong;
    [Tooltip("Time signature")] [SerializeField] private int beatsPerBar = 4;

    [Tooltip("Predefined length of 1 bar")] [SerializeField] private float barDuration = 2f;
    [Tooltip("Actual measured length of 1 bar")] [SerializeField] private float measuredBarDuration = 2f;
    [Tooltip("Desync between predefined and measured bar duration in seconds")] [SerializeField] private float barDurationDesyncSec = 0f;

    [Tooltip("Predefined length of 1 beat")] [SerializeField] private float beatDuration = 0.5f;
    [Tooltip("Actual measured length of 1 beat")] [SerializeField] private float measuredBeatDuration = 0.5f;
    [Tooltip("Actual measured length of 1 beat with 80% filtering")] [SerializeField] private float measuredBeatDurationFiltered = 0.5f;
    [Tooltip("Measured Desync time in Milliseconds between when a beat is supposed to happen and when it is actually triggered")] [Range(0f, 25f)] [SerializeField] private float beatDesyncMs;
    [Tooltip("Measured Filtered Desync time between when a beat is supposed to happen and when it is actually triggered")] [Range(0f, 25f)] [SerializeField] private float beatDesyncMsFiltered;
    [Tooltip("Measured Desync time in Seconds between when a beat is supposed to happen and when it is actually triggered")] internal float BeatDesyncSec;
    [Tooltip("Measured Filtered Desync time between when a beat is supposed to happen and when it is actually triggered")] internal float BeatDesyncSecFiltered;

    [Header("Current Music Time")]
    [Tooltip("Total amount of bars")] [SerializeField] private int totalBars = 1;
    [Tooltip("Current bar in music segment. 0-indexed")] [SerializeField] private int currentBar = 0;
    [Tooltip("Current beat in time. 0-indexed")] [SerializeField] private int currentBeat = 0;
    [Tooltip("Current Division in time. 0-indexed")] [SerializeField] private int currentDivision = 0;

    [Header("Music")]
    [Tooltip("The current segment of the music playing")] [SerializeField] private MusicSegment currentMusicSegment = MusicSegment.None;
    [Tooltip("The new queued segment to play")] [SerializeField] private MusicSegment newMusicSegment = MusicSegment.None;
    [Tooltip("Number of stars that must be connected in succession before music segment increments")] [SerializeField] private int currentStarsToConnectBeforeNewMusicSegment = 8;
    [Tooltip("Number of stars currently connected in succession in this music segment")] [SerializeField] private int currentStarsConnected = 0;
    [Tooltip("Time in seconds until the next pulse should trigger")] [SerializeField] private float timeToNextPulse;

    [SerializeField] private float futurePulseTimeStamp;
    [SerializeField] private float pastPulseTimeStamp;
    [SerializeField] private float deltaTimeStamps;

    [Header("Wwise Music Playlist")]
    private AkMusicPlaylistCallbackInfo playlist;
    [SerializeField] private uint EventId;
    [SerializeField] private uint playingId;
    [SerializeField] private uint playlistId; //< ID of active node in music playlist container
    [SerializeField] private uint uNumPlaylistItems; //< Number of items in playlist node (may be segments or other playlists)
    [SerializeField] private uint uPlaylistSelection; //< Selection: set by sound engine, modified by callback function (if not in range 0 <= uPlaylistSelection < uNumPlaylistItems then ignored)
    [SerializeField] private uint uPlaylistItemDone; //< Playlist node done: set by sound engine, modified by callback function (if set to anything but 0 then the current playlist item is done, and uPlaylistSelection is ignored)


    private int currentMusicSegmentIndex = -1;
    private int newMusicSegmentIndex = -1;
    private float lastBeatTime = 0;
    private float lastBarTime = 0;
    private float nextBeatTime;
    private float nextBarTime;
    private float twoBarStartTime;
    private float nextBeatAt;
    private float timeToNextSuckEnds;
    private float divisionDuration;
    private float nextDivisionTime;
    private float allowNextComplexityChangeTimeStamp;
    private bool readyToResetCurrentBar;
    private int searchCount;
    private int complexityLevel = -1;
    private int actualSuckForN = 1;
    private MusicTime musicTimeAtDivision = new MusicTime();
    private MusicTime musicTimeToNextSuckEnds;
    private MusicTime timeToNextPulseMusicTime;
    private Coroutine crSubGrid;
    private Coroutine crRunBeats;
    private Coroutine crFindSong;
    private SO_RhythmGrid rg;
    

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;
        OnEnabled?.Invoke();
    }

    private IEnumerator Start()
    {
        //Choose a song to play
        if (forceSong != null)
            currentSong = forceSong;
        else if (currentSong == null)
        {
            if (songs.Count > 0)
                currentSong = songs[Random.Range(0, songs.Count)];
            else
            {
                Debug.LogError("No Songs in MusicSystem -> Songs");
                yield break;
            }
        }

        yield return WwiseInfo.allBanksLoaded;

        StartMusic();

        while (!CalibrateSystem.Instance.KnowsState)
            yield return null;

        if (!CalibrateSystem.Instance.IsCalibrated())
        {
            if (Log)
                Debug.Log("Needs calibrating");
            
            CalibrateSystem.OnCalibrationRollStart += OnCalibrationRollStart;

            while (!CalibrateSystem.Instance.IsCalibrated())
                yield return null;

            TriggerNewMusicSegment(0); //Start song from its intro
        }
        else
        {
            //Continue from Idle musicsegment to intro segment of the chosen song
            if (Log)
                Debug.Log("Calibration uses saved.");

            PlaySong(currentSong);
            TriggerNewMusicSegment(0); //Start song from its intro
        }
    }

    #endregion

    #region Wwise Callbacks

    /// <summary>
    /// Callbacks from Wwise, when Bar, Beat or playlist changes
    /// 
    /// <seealso cref="https://www.audiokinetic.com/library/2016.2.6_6153/?source=SDK&id=struct_ak_music_playlist_callback_info.html"/>
    /// </summary>
    private void MusicCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (in_type == AkCallbackType.AK_MusicPlaylistSelect)
            OnMusicPlaylistSelect(in_info);

        if (in_type == AkCallbackType.AK_MusicSyncEntry)
            OnMusicSyncEntry(in_info);

        if (in_type == AkCallbackType.AK_MusicSyncBar)
            OnMusicSyncBar(in_info);

        if (in_type == AkCallbackType.AK_MusicSyncBeat)
            OnMusicSyncBeat(in_info);
    }


    /// <summary>
    /// Called when Wwise changes music playlist.
    /// Example: eventID: 840238966 playingID: 31 playlistID: 441850003 uNumPlaylistItems: 1 uPlaylistSelection: 0 uPlaylistItemDone: 0 
    /// eventID is the playing event (Horizon)
    /// playingID is the Object that is played (MusicSystem object)
    /// playlistID is the id of that playlist that is played (playlist called 441850003 or "Intro")
    /// uNumPlayListItems is number of items in this playlist (the Intro playlist Container of Horizon has 1 item: the "Intro" segment)
    /// uPlaylistSelection is the index of the playlist items current selected to play
    /// uPlaylistItemDone is the item in the playlist which is considered done and finished playing (nothing yet in this example as it is set to loop)
    /// </summary>
    /// <param name="in_info">The in information.</param>
    private void OnMusicPlaylistSelect(AkCallbackInfo in_info)
    {
        AkMusicPlaylistCallbackInfo newPlaylist = (AkMusicPlaylistCallbackInfo)in_info;
        //if (playlist != null)
        //    Debug.Log("playlistID: " + playlist.playlistID + " new Playlist: " + newPlaylist.playlistID);

        if (playlist == null || playlist.playlistID != newPlaylist.playlistID)
        {
            playlist = newPlaylist;
            EventId = playlist.eventID;
            playingId = playlist.playingID;
            playlistId = playlist.playlistID;
            uNumPlaylistItems = playlist.uNumPlaylistItems;
            uPlaylistSelection = playlist.uPlaylistSelection;
            uPlaylistItemDone = playlist.uPlaylistItemDone;

            if (Log)
                Debug.Log("Music Segment: " + currentMusicSegment + " eventID: " + EventId + " playingID: " + playingId + " playlistID: " + playlistId + " new Playlist: " + newPlaylist.playlistID + " uNumPlaylistItems: " + uNumPlaylistItems + " uPlaylistSelection: " + uPlaylistSelection + " uPlaylistItemDone: " + uPlaylistItemDone);
        }
    }


    /// <summary>
    /// Called when Wwise music syncs a new entry
    /// </summary>
    private void OnMusicSyncEntry(AkCallbackInfo in_info)
    {
        if (newMusicSegment != currentMusicSegment)
        {
            //Make a pre-suck as we start the first pulses, we want a suck to happen before that playlist starts playing
            if (currentMusicSegment == MusicSegment.None)
            {
                //Debug.Log("TimeToNextBar at Intro -> Horizon 1" + TimeToNextBar());
                OnSuck?.Invoke(TimeToNextBar());
            }

            currentMusicSegment = newMusicSegment;
            currentMusicSegmentIndex = (int)currentMusicSegment;

            //Debug.Log("MUSIC TIME: " + CurrentBar + ":" + CurrentBeat + ":" + CurrentDivision);

            currentBar = -1;
            readyToResetCurrentBar = true;
            if (Log)
                Debug.Log("New Music Sync Entry. Music Segment: " + currentMusicSegment + " eventID: " + EventId + " playingID: " + playingId + " playlistID: " + playlistId + " uNumPlaylistItems: " + uNumPlaylistItems + " uPlaylistSelection: " + uPlaylistSelection + " uPlaylistItemDone: " + uPlaylistItemDone + " CurrentBar: " + currentBar);
        }
    }


    /// <summary>
    /// Called when Wwise music syncs a bar
    /// </summary>
    /// <param name="in_info">The in information.</param>
    private void OnMusicSyncBar(AkCallbackInfo in_info)
    {
        //Prevent Wwise from triggering twice in the same frame
        if (Time.realtimeSinceStartup >= nextBarTime)
        {
            AkMusicSyncCallbackInfo music = (AkMusicSyncCallbackInfo)in_info;

            barDuration = music.segmentInfo_fBarDuration;
            nextBarTime = Time.realtimeSinceStartup + (barDuration * 0.5f);

            if (lastBarTime == 0f)
                lastBarTime = Time.realtimeSinceStartup;

            //The first couple of Wwise Calls don't happen in the correct time and are thrown at Unity
            //Therefore, we wait a little while before acknowledging them
            if (Time.realtimeSinceStartup > 4)
            {
                measuredBarDuration = (Time.realtimeSinceStartup - lastBarTime);
                barDurationDesyncSec = measuredBarDuration - barDuration;
                lastBarTime = Time.realtimeSinceStartup;
                beatsPerBar = (int)(barDuration / beatDuration);
            }

            OnBar?.Invoke(barDuration);
            totalBars++;
            currentBar++;
            currentBeat = -1;
            //CurrentDivision = -1;

            if (readyToResetCurrentBar && currentBar % 4 == 0)
            {
                currentBar = 0;
                readyToResetCurrentBar = false;

                if (crRunBeats != null)
                    StopCoroutine(crRunBeats);

                int index = currentMusicSegmentIndex - 1;
                if (index < 0)
                    index = 0;

                crRunBeats = StartCoroutine(RunBeats(barDuration, currentSong.rhythmGrids[index].BeatsPerBar));
            }
            else if (currentMusicSegmentIndex > 0)
            {
                if (currentBar > 0 && currentBar % currentSong.rhythmGrids[currentMusicSegmentIndex - 1].Bars == 0)
                    currentBar = 0;

                if (crRunBeats != null)
                    StopCoroutine(crRunBeats);

                crRunBeats = StartCoroutine(RunBeats(barDuration, currentSong.rhythmGrids[currentMusicSegmentIndex - 1].BeatsPerBar));
            }
            else
            {
                if (currentBar > 0 && currentBar % 2 == 0)
                    currentBar = 0;

                if (crRunBeats != null)
                    StopCoroutine(crRunBeats);

                crRunBeats = StartCoroutine(RunBeats(barDuration, 4));
            }


            if (currentBar != 1)
            {
                twoBarStartTime = Time.time;
                //Debug.Log("Reset TwoBarStartTime: " + TwoBarStartTime);
            }
            //if (Log)
            //    Debug.Log("Bar " + music.musicSyncType + " " + music.segmentInfo_fBarDuration + " CurrentBar: " + CurrentBar);
        }
        /*else
        {
            Debug.LogWarning("New bar from Wwise before NextBarTime was ready. Time.realtimeSinceStartup " + Time.realtimeSinceStartup + " nextBarTime: " + nextBarTime);
        }*/
    }


    /// <summary>
    /// Called when Wwise music syncs a beat
    /// </summary>
    /// <param name="in_info">The in information.</param>
    private void OnMusicSyncBeat(AkCallbackInfo in_info)
    {
        if (Time.realtimeSinceStartup >= nextBeatTime)
        {
            AkMusicSyncCallbackInfo music = (AkMusicSyncCallbackInfo)in_info;

            /*if (Log)
                Debug.Log("Beat " + CurrentBeat + " sync type: " + music.musicSyncType + " " + music.segmentInfo_fBeatDuration);*/

            beatDuration = music.segmentInfo_fBeatDuration;
            nextBeatTime = Time.realtimeSinceStartup + beatDuration - 0.5f;

            //The first couple of Wwise Calls don't happen in the correct time and are thrown at Unity
            //Therefore, we wait a little while before acknowledging them
            if (Time.realtimeSinceStartup > 4)
            {
                measuredBeatDuration = (Time.realtimeSinceStartup - lastBeatTime);
                lastBeatTime = Time.realtimeSinceStartup;
                measuredBeatDurationFiltered = (measuredBeatDurationFiltered * 0.8f) + (measuredBeatDuration * 0.2f);
            }

            /*OnBeat?.Invoke(BeatDuration);

            CurrentBeat++;
            if (CurrentBeat > CurrentSong.BeatsPerBar)
                CurrentBeat = 1;

            if (crSubGrid != null)
                StopCoroutine(crSubGrid);

            crSubGrid = StartCoroutine(SubGrid(BeatDuration, Division));*/
        }
    }

    #endregion

    #region Beat and Divisions

    /// <summary>
    /// Runs logic loop to trigger beats at intervals. This only runs for "beats" number of times
    /// Is called by <see cref="OnMusicSyncBar(AkCallbackInfo)"/>
    /// Takes desyncing into account and attempts to counterbalance the measured beat desync (delay) of when each beat was supposed to happen
    /// This will let the beat trigger earlier next in order to self correct
    /// </summary>
    /// <param name="barDuration">Duration of the bar.</param>
    /// <param name="beats">The number of beats in this bar</param>
    IEnumerator RunBeats(float barDuration, int beats)
    {
        beatDuration = barDuration / beats;
        nextBeatAt = Time.realtimeSinceStartup;

        while (currentBeat < beats)
        {
            if (Time.realtimeSinceStartup >= nextBeatAt)
            {
                OnBeat?.Invoke(beatDuration);

                BeatDesyncSec = Time.realtimeSinceStartup - nextBeatAt;
                beatDesyncMs = BeatDesyncSec * 1000f;

                if (BeatDesyncSecFiltered == 0f)
                    BeatDesyncSecFiltered = BeatDesyncSec;

                BeatDesyncSecFiltered = (BeatDesyncSecFiltered * 0.8f) + (BeatDesyncSec * 0.2f);
                beatDesyncMsFiltered = BeatDesyncSecFiltered * 1000f;
                currentBeat++;

                if (currentMusicSegmentIndex > 0)
                {
                    if (currentBeat > currentSong.rhythmGrids[currentMusicSegmentIndex - 1].BeatsPerBar - 1)
                        currentBeat = 0;
                }
                else if (currentBeat > 3)
                    currentBeat = 0;


                nextBeatAt = Time.realtimeSinceStartup + beatDuration - BeatDesyncSecFiltered;

                if (crSubGrid != null)
                    StopCoroutine(crSubGrid);

                crSubGrid = StartCoroutine(SubGrid(beatDuration, division));
            }

            yield return null;
        }
    }


    /// <summary>
    /// Handles beat division, so a beat can have several sub-divisions
    /// Runs the logic of triggering the subdivisions at the corresponding times
    /// </summary>
    IEnumerator SubGrid(float duration, int division)
    {
        divisionDuration = duration / (float)division;
        nextDivisionTime = Time.realtimeSinceStartup;

        currentDivision = -1;
        while (currentDivision < division - 1)
        {
            //When reaching the next division sub grid
            if (Time.realtimeSinceStartup >= nextDivisionTime)
            {
                currentDivision++;

                if (currentDivision < this.division)
                {
                    musicTimeAtDivision.Bar = currentBar;
                    musicTimeAtDivision.Beat = currentBeat;
                    musicTimeAtDivision.Division = currentDivision;

                    OnDivision?.Invoke(musicTimeAtDivision);

                    nextDivisionTime = Time.realtimeSinceStartup + divisionDuration;
                    if (currentMusicSegmentIndex > 0)
                    {
                        rg = currentSong.rhythmGrids[currentMusicSegmentIndex - 1];

                        if (complexityLevel == -1)
                            complexityLevel = playerData.GetComplexityFromPlayerDifficulty();

                        if (complexityLevel != playerData.GetComplexityFromPlayerDifficulty() && Time.realtimeSinceStartup > allowNextComplexityChangeTimeStamp)
                        {
                            PerformanceSystem.Instance.OnComplexityChanged(playerData.GetComplexityFromPlayerDifficulty() > complexityLevel);
                            complexityLevel = playerData.GetComplexityFromPlayerDifficulty();
                            float secIntoTwoBarPeriod = Time.time - twoBarStartTime;
                            WwiseSystem.Instance.ChangeComplexity(complexityLevel, Mathf.RoundToInt(secIntoTwoBarPeriod * 1000f));
                            allowNextComplexityChangeTimeStamp = Time.realtimeSinceStartup + ((barDuration * 2f) - secIntoTwoBarPeriod);
                        }

                        if (futurePulseTimeStamp == 0)
                            CalculatePulseTimestamps();

                        //Send event out that there is a pulse at this division of the rhythm grid
                        if (rg.HasPulseAt(complexityLevel, musicTimeAtDivision))
                        {
                            CalculatePulseTimestamps();

                            OnPulse?.Invoke(rg.Curve, timeToNextPulse, BeatDesyncSecFiltered);

                            //Find the interval to the n next pulse, so we always see 4 Sucks ahead in time
                            //Let the actualSuckForN increase each time there's a new Suck, as we want to see the sucks asap on screen when getting a new star
                            //Even if we have the first short sucks, we still need the sucks 4 pulses ahead to trigger from the start
                            if (OnSuck != null && actualSuckForN < maxSuckForN)
                            {
                                musicTimeToNextSuckEnds = rg.GetTimeToNextPulse(playerData.GetComplexityFromPlayerDifficulty(), musicTimeAtDivision, 1);
                                actualSuckForN++;
                                timeToNextSuckEnds = musicTimeToNextSuckEnds.ToSeconds(barDuration, beatDuration, divisionDuration);
                                OnSuck?.Invoke(timeToNextSuckEnds);
                            }

                            musicTimeToNextSuckEnds = rg.GetTimeToNextPulse(playerData.GetComplexityFromPlayerDifficulty(), musicTimeAtDivision, maxSuckForN);
                            timeToNextSuckEnds = musicTimeToNextSuckEnds.ToSeconds(barDuration, beatDuration, divisionDuration);
                            OnSuck?.Invoke(timeToNextSuckEnds);

                            if (Log)
                                Debug.Log("musicTimeAtDivision: " + musicTimeAtDivision.Print() + " musicTimeToSecondNextPulse: " + musicTimeTo2ndNextPulse.Print() + " timeToSecondNextPulse: " + timeToSecondNextPulse.ToString("F1"));
                        }
                    }
                }
                else
                    Debug.LogError("CurrentDivision: " + currentDivision + " in division: " + division);

            }
            yield return null;
        }
    }

    private void CalculatePulseTimestamps()
    {
        if (Time.realtimeSinceStartup > futurePulseTimeStamp)
            DoCalculateTimestamps();
        else
            Invoke(nameof(DoCalculateTimestamps), (futurePulseTimeStamp - Time.realtimeSinceStartup) + 0.05f);
    }

    private void DoCalculateTimestamps()
    {
        timeToNextPulseMusicTime = rg.GetTimeToNextPulse(playerData.GetComplexityFromPlayerDifficulty(), musicTimeAtDivision);
        timeToNextPulse = timeToNextPulseMusicTime.ToSeconds(barDuration, beatDuration, divisionDuration);

        if (Time.realtimeSinceStartup > futurePulseTimeStamp)
        {
            pastPulseTimeStamp = futurePulseTimeStamp;
            futurePulseTimeStamp = Time.realtimeSinceStartup + timeToNextPulse;
            deltaTimeStamps = futurePulseTimeStamp - pastPulseTimeStamp;
        }
    }

    public float GetSecondsToNextPulse(int n = 1)
    {
        if (currentMusicSegmentIndex <= 0)
            return TimeToNextBar();

        if (n == 1)
            return futurePulseTimeStamp - Time.realtimeSinceStartup;

        if (rg == null)
            rg = currentSong.rhythmGrids[currentMusicSegmentIndex - 1];

        MusicTime nextPulseMusicTime = rg.GetTimeToNextPulse(playerData.GetComplexityFromPlayerDifficulty(), musicTimeAtDivision, n);
        return nextPulseMusicTime.ToSeconds(barDuration, beatDuration, divisionDuration);
    }



    #endregion

    #region Gameplay


    /// <summary>
    /// Starts the music event.
    /// With no other instructions it will start the "idle" song
    /// </summary>
    public void StartMusic()
    {
        WwiseSystem.Instance.Play("Music", gameObject, MusicCallback, AkCallbackType.AK_MusicSyncAll | AkCallbackType.AK_MusicPlaylistSelect);
        //GalaxySystem.OnCreatedFirstStarInConstellation += OnCreatedFirstStarInConstellation; //Subscribe for when galaxy has a created constellation
    }



    /// <summary>
    /// Stops the music event.
    /// </summary>
    public void StopMusic()
    {
        WwiseSystem.Instance.Stop("Music", gameObject, 3000);
    }



    /// <summary>
    /// Switches to the song provided (only if <see cref="StartMusic"/> has been called)
    /// If no newSong is provided, it will find another random song from the Songs list
    /// </summary>
    public void PlaySong(SO_Song newSong = null)
    {
        if (crFindSong != null)
            StopCoroutine(crFindSong);

        crFindSong = StartCoroutine(FindSong(newSong));
    }



    IEnumerator FindSong(SO_Song newSong = null)
    {
        if (forceSong != null)
            newSong = forceSong;
        else if (newSong == null)
        {
            if (songs.Count > 1)
            {
                searchCount = 0;
                newSong = songs[Random.Range(0, songs.Count - 1)];
                while (newSong == currentSong && searchCount < songs.Count * 2)
                {
                    newSong = songs[Random.Range(0, songs.Count)];
                    yield return null;
                }

                if (Log)
                    Debug.Log("Selecting another song to play: " + newSong.name);
            }
            else
            {
                newSong = currentSong;

                if (Log)
                    Debug.Log("Selecting same song (" + currentSong.name + ") as before because there is only one song in Songs list");
            }
        }

        currentSong = newSong;
        WwiseSystem.Instance.SetSong(currentSong.title != "" ? currentSong.title : currentSong.name);
    }


    /// <summary>
    /// Stops the current song (only if <see cref="StartMusic"/> has been called)
    /// </summary>
    public void StopCurrentSong()
    {
        WwiseSystem.Instance.SetSong(""); //Nothing sets it to idle (no music)
    }



    /// <summary>
    /// Called when CalibrateSystem starts the heat map process
    /// </summary>
    private void OnCalibrationRollStart()
    {
        CalibrateSystem.OnCalibrationRollStart -= OnCalibrationRollStart;
        //Continue from Idle music segment to intro segment of the chosen song
        PlaySong(currentSong);
        //Debug.Log("OnCalibrationHeatMapStart: Playing Intro");
    }
    
    
    
    /// <summary>
    /// Begins the rhythms grid segments of the current song
    /// Invoked by GalaxySystem when that's done creating the constellation
    /// </summary>
    public void OnCreatedFirstStarInConstellation()
    {
        GalaxySystem.OnCreatedFirstStarInConstellation -= OnCreatedFirstStarInConstellation;

        if(currentMusicSegment == MusicSegment.Intro)
            TriggerNewMusicSegment();
        //Debug.Log("OnCreatedFirstStarInConstellation: Began Rhythms");
    }



    /// <summary>
    /// When player hits a constellation star, this is called
    /// Will trigger a new music segments once every x stars hit
    /// </summary>
    /// <param name="totalCount">The total count.</param>
    internal void OnStarReached()
    {
        actualSuckForN = 1;
        currentStarsConnected++;

        if (currentStarsConnected >= currentStarsToConnectBeforeNewMusicSegment && playerData.LongTermPerformance >= 0.25f)
        {
            currentStarsConnected = 0;
            TriggerNewMusicSegment(triggerNewMusicSegmentEvent: true);
        }
    }



    /// <summary>
    /// Triggers a new music segment in Wwise
    /// </summary>
    internal void TriggerNewMusicSegment(int segment = -1, bool triggerNewMusicSegmentEvent = false)
    {
        if (segment > -1)
        {
            if (segment == newMusicSegmentIndex)
                return;

            newMusicSegmentIndex = segment;
        }
        else
        {
            //When at the intro and shifting to another segment, check if there's a stored progress to switch to instead of just the first segment
            if (currentMusicSegmentIndex == 0 && currentSong.MusicSegmentIndexProgress > -1)
                newMusicSegmentIndex = currentSong.MusicSegmentIndexProgress;
            else
                newMusicSegmentIndex++; 
        }

        if (newMusicSegmentIndex > currentSong.MusicSegments)
            newMusicSegmentIndex = 1;

        newMusicSegment = (MusicSegment)newMusicSegmentIndex;

        //Only store non-intro segments
        if (newMusicSegmentIndex > 0)
            currentSong.MusicSegmentIndexProgress = newMusicSegmentIndex;

        currentStarsToConnectBeforeNewMusicSegment += 2; //Increment by 2

        int msIntoTwoBarPeriod = Mathf.RoundToInt((Time.time - twoBarStartTime) * 1000);

        WwiseSystem.Instance.SetMusicSegment(newMusicSegment, msIntoTwoBarPeriod, triggerNewMusicSegmentEvent);


        if (Log)
            Debug.Log("Queuing new Music Segment: " + newMusicSegment);
    }


    #endregion

    #region Helpers


    internal void AddSong(SO_Song song)
    {
        if (!songs.Contains(song))
            songs.Add(song);
    }


    #region Music Time Helpers


    /// <summary>
    /// Gives the time in seconds until the next beat.
    /// </summary>
    internal float TimeToNextBeat()
    {
        //Finds actual time until the next music grid cue of a beat
        if (lastBeatTime > 0f && beatDuration > 0f)
            return (lastBeatTime + beatDuration) - Time.realtimeSinceStartup;
        else
            return 0;
    }


    /// <summary>
    /// Gives the time in seconds until the next bar.
    /// </summary>
    internal float TimeToNextBar()
    {
        return (lastBarTime + barDuration) - Time.realtimeSinceStartup;
    }


    public float GetTimeToNextPulse()
    {
        if (newMusicSegment != MusicSegment.None && newMusicSegment != MusicSegment.Outro && currentSong != null)
        {
            musicTimeToNextSuckEnds = currentSong.rhythmGrids[newMusicSegmentIndex - 1].GetTimeToNextPulse(playerData.GetComplexityFromPlayerDifficulty(), musicTimeAtDivision, 1);
            timeToNextSuckEnds = musicTimeToNextSuckEnds.ToSeconds(barDuration, beatDuration, divisionDuration);
            return timeToNextSuckEnds;
        }
        else
            return 0;
    }


    /// <summary>
    /// Calculate the delta time to the closest pulse, be that the future or past pulse
    /// </summary>
    /// <returns></returns>
    internal float TimeToClosestPulse()
    {
        float timeToFuturePulse = futurePulseTimeStamp - Time.realtimeSinceStartup;
        float timeToPastPulse = Time.realtimeSinceStartup - pastPulseTimeStamp;

        if (Log && timeToFuturePulse > 1.3f && timeToPastPulse > 1.3f)
            Debug.Log("Time now: " + Time.realtimeSinceStartup + " PastPulseTimeStamp: " + pastPulseTimeStamp + " timeToPastPulse: " + timeToPastPulse.ToString("F3") + " FuturePulseTimeStamp: " + futurePulseTimeStamp + " timeToFuturePulse: " + timeToFuturePulse.ToString("F3") + " DeltaTimeStamps: " + deltaTimeStamps.ToString("F3"));

        if (futurePulseTimeStamp == 0f || deltaTimeStamps < 0.1f)
            return 0;

        if (timeToFuturePulse > barDuration * 2f && timeToPastPulse > barDuration * 2f)
            return 0;

        return timeToFuturePulse < timeToPastPulse ? timeToFuturePulse : timeToPastPulse;
    }


    /// <summary>
    /// Calculates time to end of the new music segment (could be same as current music segment)
    /// </summary>
    /// <param name="correctForStartDesync">if set to <c>true</c> [correct for start desync].</param>
    internal float TimeToEndOfSegment(bool correctForStartDesync)
    {
        float timeLeftThisBar = TimeToNextBar();
        int index = newMusicSegmentIndex - 1 > -1 ? newMusicSegmentIndex - 1 : 0;
        float timeLeftOtherBars = (currentBar + 1 % currentSong.rhythmGrids[index].Bars) == 0 ? 0 : ((currentSong.rhythmGrids[index].Bars - (currentBar + 1)) * barDuration);
        return timeLeftThisBar + timeLeftOtherBars + (correctForStartDesync ? -0.6f : 0);
    }

    internal float TimeToNextDoubleBar()
    {
        float timeLeftThisBar = TimeToNextBar();
        if (currentBar % 2 == 0)
            return timeLeftThisBar;
        else
            return timeLeftThisBar + barDuration;
    }

    #endregion

    /// <summary>
    /// Gets a pre-defined curve for animating the stars
    /// </summary>
    /// <param name="curveType">Type of the curve.</param>
    /// <returns></returns>
    public AnimationCurve GetCurve(CurveType curveType)
    {
        switch (curveType)
        {
            case CurveType.Sine:
                return curveSine;
            case CurveType.Saw:
                return curveSaw;
            /*case CurveType.Square:
                break;
            case CurveType.ShortAttack:
                break;
            case CurveType.LongAttack:
                break;*/
            default:
                return curveSine;
        }
    }

    #endregion
}
