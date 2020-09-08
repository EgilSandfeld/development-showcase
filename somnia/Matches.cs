using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AWE
{
    /// <summary>
    /// This script holds and manipulates data of user profile matching with app content like sounds and audio trigger types
    /// The scriptable objects made from this are fully serializable using JSON, and are loaded/unloaded using the BaseSO class
    /// The script utilizes <see href="https://github.com/dbrizov/NaughtyAttributes">NaughtyAttributes</see> for quick Unity inspector prettyness
    /// The class is flexible to changes in content, and allows for refreshed to the match values when requested by other classes
    /// The scriptable object makes sure to only hold data and do internal data changes fed with information from outside the class
    /// </summary>
    [CreateAssetMenu(fileName = "Matches", menuName = "AWE/Matches", order = 1)]
    public class Matches : BaseSO
    {
        #region Variables

        [HorizontalLine(2, EColor.Gray)]

        public int UpdateId;
        public List<Match> List;

        #endregion

        #region Init

        private void Awake()
        {
            Load();
        }

        #endregion

        #region Basic public methods

        [Button]
        public void Sort()
        {
            List = List.OrderBy(x => x.Name).ToList();
        }

        public void SetUpdateId(int id)
        {
            UpdateId = id;
        }

        public override void ResetSettings()
        {
            if (!Persist)
                return;

            UpdateId = 0;
            List.Clear();
        }

        #endregion

        #region Match methods

        public Match GetMatchFromName(string name)
        {
            return List.FirstOrDefault(x => x.Name == name);
        }

        public Match GetOrCreateCompositionAppreciationFromHeader(CompositionHeader header, float value = 0.5f)
        {
            return GetOrCreateDataFromString(header.Name, value);
        }

        private Match GetOrCreateDataFromString(string name, float value = 0.5f)
        {
            Match data = List.FirstOrDefault(x => x.Name == name);
            if (data == null)
            {
                data = new Match
                {
                    Name = name,
                    Value = value
                };
                List.Add(data);
                Sort();
                SaveToPersistence();
            }

            return data;
        }

        public float GetMatchValueFromHeader(CompositionHeader header)
        {
            Match data = List.FirstOrDefault(x => x.Name == header.Name);
            if (data == null)
                return 0.5f;
            else
                return data.Value;
        }

        public float GetMatchValueFromName(string name)
        {
            Match data = List.FirstOrDefault(x => x.Name == name);
            if (data == null)
                return 0.5f;
            else
                return data.Value;
        }


        private void SetDataFromString(string name, float value = 0.5f)
        {
            Match data = List.FirstOrDefault(x => x.Name == name);
            if (data == null)
            {
                data = new Match
                {
                    Name = name,
                    Value = value
                };
                List.Add(data);
                Sort();
            }
            else
                data.Value = value;
        }

        #endregion

        #region Calculate Match Values

        //Calculates an average match value on how well any content type fit with the user's preferences for sounds
        public bool CalculateMatch(List<AppreciationData> triggerAppreciations, CompositionHeader header, AppreciationData compAppreciation, List<AppreciationData> aubitAppreciations)
        {
            if (triggerAppreciations == null || compAppreciation == null || aubitAppreciations == null ||(triggerAppreciations.Count == 0 && compAppreciation.AppreciationIndirect == 0))
                return false;


            #region Trigger Types Match

            float matchFromTriggerTypes = 0;
            float lowest = -1;
            var e = header.GetEnabledOrAllTriggerTypeNames();

            for (int i = 0; i < e.Count; i++)
            {
                var triggerAppr = triggerAppreciations.FirstOrDefault(x => x.Name == e[i]);
                if (triggerAppr == null)
                {
                    if (0.5f < lowest || lowest == -1)
                        lowest = 0.5f;

                    matchFromTriggerTypes += 0.5f;
                }
                else
                {
                    if (triggerAppr.Appreciation < lowest || lowest == -1)
                        lowest = triggerAppr.Appreciation;

                    matchFromTriggerTypes += triggerAppr.Appreciation;
                }
            }

            matchFromTriggerTypes /= e.Count;

            #endregion


            #region Aubits Match

            float lowestAubitAppreciation = -1;
            if (aubitAppreciations.Count > 0)
            {
                lowestAubitAppreciation = aubitAppreciations[0].GetTotalAppreciation();

                for (int i = 1; i < aubitAppreciations.Count; i++)
                {
                    if (aubitAppreciations[i].GetTotalAppreciation() < lowestAubitAppreciation)
                        lowestAubitAppreciation = aubitAppreciations[i].GetTotalAppreciation();
                }

                lowestAubitAppreciation = HelperMethods.Scale(lowestAubitAppreciation, -30f, 30f, 0f, 1f, true);
            }
            else
                lowestAubitAppreciation = 0.5f;

            #endregion

            float totalMatch = (matchFromTriggerTypes + lowest + lowestAubitAppreciation + lowestAubitAppreciation) / 4f;

            if (totalMatch != 0.5f)
                SetDataFromString(header.Name, totalMatch);

            return true;
        }

        #endregion
    }

    [Serializable]
    public class Match
    {
        public string Name;
        public float Value;
    } 
}
