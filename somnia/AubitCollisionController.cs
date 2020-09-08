using AWE.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class to handle collision checks between aubits on the 3D Space and react
/// Registers objects that need to be checked for colliding
/// Does a check on normalized screen distance between objects, and trigger a collision when two objects are close
/// This script removes the needs for 2D physics module in Unity, thus saving build size from stripping the module
/// </summary>
public class AubitCollisionController : MonoBehaviour 
{
    #region Variables
    public static AubitCollisionController Instance;

    [SerializeField] private float distThreshold = 200f;
    [SerializeField] private bool Log;

    private float distThresholdNormalized;
    private float distExitThresholdNormalized;
    private bool isChecking;
    private List<DraggerCollider> draggers = new List<DraggerCollider>();
    private WaitForSeconds wfs = new WaitForSeconds(0.25f);
    private int uniqueId;

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;
        distThresholdNormalized = distThreshold / CanvasController.Instance.MaxDist;
        distExitThresholdNormalized = distThresholdNormalized + (distThresholdNormalized * 0.1f);
    }

    #endregion

    #region Registration

    internal void AddDraggerToChecks(Dragger dragger)
    {
        if (draggers.FirstOrDefault(x => x.dragger == dragger) == null)
        {
            draggers.Add(new DraggerCollider(uniqueId, dragger));
            uniqueId++;

            if (!isChecking && draggers.Count > 1)
            {
                StartCoroutine(RunCollisionChecker());
                if (Log)
                    Debug.Log("AddDraggerToChecks starting RunCollisionChecker, after adding dragger " + dragger.Name);
            }
            else if (Log)
                Debug.Log("AddDraggerToChecks added dragger " + dragger.Name);
        }
    }

    internal void RemoveDraggerFromChecks(Dragger dragger)
    {
        DraggerCollider dc = draggers.FirstOrDefault(x => x.dragger == dragger);
        if (dc != null)
        {
            if (dragger.draggerIDsInsideTriggerRadius.Count > 0)
            {
                int idToRemove = draggers.FirstOrDefault(x => x.dragger == dragger).id;
                for (int i = 0; i < draggers.Count - 1; i++)
                {
                    if (draggers[i].dragger.draggerIDsInsideTriggerRadius.Contains(idToRemove))
                        draggers[i].dragger.draggerIDsInsideTriggerRadius.Remove(idToRemove);
                }
            }

            draggers.Remove(dc);

            if (draggers.Count < 2)
            {
                isChecking = false;
                if (Log)
                    Debug.Log("RemoveDraggerFromChecks stopped collision check after removing dragger " + dragger.Name);
            }
            else if (Log)
                Debug.Log("RemoveDraggerFromChecks removed dragger " + dragger.Name);
        }
    }

    #endregion

    #region Collision check

    /// <summary>
    /// Begins running when there are 2+ aubits present on the 2D Space
    /// Checks if any 2 aubits are close to each other and handles this as a collision.
    /// This is cheaper than utilizing the OnTriggerEnter that resides in Unity Physics (Physics module has been stripped out of the app!)
    /// </summary>
    /// <returns></returns>
    private IEnumerator RunCollisionChecker()
    {
        isChecking = true;

        while (isChecking)
        {
            //Check only when the aubits are moving (playing) and screen is not dimmed
            if (WwisePlayPause.Instance.wwisePlaying && !DimController.Instance.isDimmed)
            {
                //Only check each entry in list once against others
                for (int i = 0; i < draggers.Count - 1; i++)
                {
                    for (int j = i + 1; j < draggers.Count; j++)
                    {
                        float distNormalized = Vector2.Distance(draggers[i].dragger.rt.anchoredPosition, draggers[j].dragger.rt.anchoredPosition) / CanvasController.Instance.MaxDist;

                        //Check if the distance is shorter than the threshold (= collision)
                        if (!draggers[i].dragger.draggerIDsInsideTriggerRadius.Contains(draggers[j].id))
                        {
                            if (distNormalized < distThresholdNormalized)
                            {
                                draggers[i].dragger.OnRadiusEnter(draggers[j].id, draggers[j].dragger);
                                if (draggers.Count > j)
                                    draggers[j].dragger.OnRadiusEnter(draggers[i].id, draggers[i].dragger);

                                if (Log)
                                    Debug.Log("RunCollisionChecker collision between " + draggers[i].Name + " and " + draggers[j].Name);
                            }
                        }
                        else
                        {
                            if (distNormalized > distExitThresholdNormalized)
                            {
                                draggers[i].dragger.OnRadiusExit(draggers[j].id, draggers[j].dragger);
                                draggers[j].dragger.OnRadiusExit(draggers[i].id, draggers[i].dragger);

                                if (Log)
                                    Debug.Log("RunCollisionChecker collision ended between " + draggers[i].Name + " and " + draggers[j].Name);
                            }
                        }
                    }
                }
            }

            yield return wfs;
        }
    }

    #endregion
}
