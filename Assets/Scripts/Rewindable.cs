/*
Jonas Wombacher - Research Project Telecooperation
Copyright (C) 2023 Jonas Wombacher

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// script managing the rewinding of the character it is attached to
public class Rewindable : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject positionIndicatorPrefab;
    [SerializeField] private TrailRenderer rewindTrail;
    [SerializeField] private TrailMesh trailMeshPrefab;

    [Header("Settings")]
    [SerializeField] private float saveInterval;
    [SerializeField] private float trailMeshDiameter = 0.2f;

    private Character character;
    private NavMeshAgent navMeshAgent;
    private Transform rewindPositionParent;
    private Transform trailMeshParent;
    private TrailMesh rewindTrailMesh;
    private List<CharacterRewindState> characterRewindStates;
    private List<GameObject> indicators;
    private float lastSave;
    private bool recording;
    private int currentStateIndex;
    private bool resuming;
    private int resumeTargetIndex;
    private Material trailMaterial;

    private void Start()
    {
        // get references to the parent objects, the attached character and the attached nav mesh agent
        this.SearchParents();
        this.character = this.GetComponent<Character>();
        this.navMeshAgent = this.GetComponent<NavMeshAgent>();

        // initialize lists
        this.characterRewindStates = new List<CharacterRewindState>();
        this.indicators = new List<GameObject>();

        // create a game object containing the trail mesh for the attached character
        this.rewindTrailMesh = Instantiate(this.trailMeshPrefab, Vector3.zero, Quaternion.identity, this.trailMeshParent);
        this.rewindTrailMesh.rewindable = this;

        this.recording = false;
    }

    private void Update()
    {
        // save the character's current state if it is due according to the configured interval
        if (this.recording && Time.time - this.lastSave >= this.saveInterval)
        {
            this.SaveCurrentState();
            this.lastSave = Time.time;
        }
    }

    private void OnDestroy()
    {
        // also destroy the trail mesh object instantiated by this script
        if (this.rewindTrailMesh != null) Destroy(this.rewindTrailMesh.gameObject);
        
        // also destroy all position indicators
        foreach (GameObject indicator in this.indicators) if (indicator != null) Destroy(indicator);
    }

    // apply the given color to the rewind trail
    public void SetTrailColor(Color color)
    {
        this.rewindTrail.material.color = color;
    }

    // start/stop recording rewind states
    public void SetRecording(bool recording)
    {
        if (this.recording == recording) return;

        // set flag
        this.recording = recording;

        // start/stop emitting the rewind trail
        this.rewindTrail.emitting = recording;

        // initialize timestamp of last save to immediately save the starting position as the first state
        if (recording) this.lastSave = Time.time - this.saveInterval;
        else
        {
            // create mesh of the finished trail
            this.UpdateTrailMesh();

            // set index to the last position, as the character is at its destination's position
            this.currentStateIndex = this.characterRewindStates.Count - 1;
        }
    }

    // check if this rewind manager is currently recording
    public bool GetRecording()
    {
        return this.recording;
    }

    // set flag indicating whether the character is currently walking because of this rewind manager
    public void SetResuming(bool resuming)
    {
        this.resuming = resuming;
    }

    // check if the character is currently walking because of this rewind manager
    public bool GetResuming()
    {
        return this.resuming;
    }

    // save the character's current state
    private void SaveCurrentState()
    {
        // store state data
        this.StoreCurrentStateData();

        // spawn indicator for the saved state
        this.indicators.Add(Instantiate(this.positionIndicatorPrefab, this.transform.position, Quaternion.identity, this.rewindPositionParent));
    }

    // store the current state of the character this script is attached to in the list of states
    private void StoreCurrentStateData()
    {
        this.characterRewindStates.Add(new CharacterRewindState(this.transform, this.animator));
    }

    // apply the character's old state with the given index
    public void ApplyStateAtIndex(int index)
    {
        // only allow applying states once the recording is done
        if (this.recording) return;

        // only allow valid indices
        if (index < 0 || index >= this.characterRewindStates.Count) return;

        // deactivate nav mesh agent to stop character from walking, unset resuming flag
        this.navMeshAgent.enabled = false;
        this.SetResuming(false);

        // get the desired state
        CharacterRewindState state = this.characterRewindStates[index];

        // apply animation data
        this.animator.Play(state.stateNameHash, 0, state.normalizedTime);
        this.animator.speed = 0;

        // apply transform data
        this.transform.localPosition = state.localPos;
        this.transform.localRotation = state.localRot;
        this.transform.localScale = state.localScale;

        // store the new index
        this.currentStateIndex = index;

        // only show the ghosts spawned before the state with the given index was saved
        this.character.ApplyRewindStateToTraces(index);
    }

    // apply the state following the current one
    public void GoToNextState()
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();
        this.ApplyStateAtIndex(this.currentStateIndex + 1);
    }

    // apply the state preceding the current one
    public void GoToPreviousState()
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();
        this.ApplyStateAtIndex(this.currentStateIndex - 1);
    }

    // search the RewindPositionParent and TrailMeshParent game objects by their names and store them
    private void SearchParents()
    {
        // rewind position parent
        GameObject rewindParent = GameObject.Find("RewindPositionParent");
        if (rewindParent) this.rewindPositionParent = rewindParent.transform;

        // trail mesh parent
        GameObject trailMeshParent = GameObject.Find("TrailMeshParent");
        if (trailMeshParent) this.trailMeshParent = trailMeshParent.transform;
    }

    // calculate new data for the trail renderer's mesh and apply to the attached character's trail mesh
    private void UpdateTrailMesh()
    {
        (Vector3[] vertices, int[] triangles) = TrailMeshGenerator.GenerateMeshData(this.rewindTrail, this.trailMeshDiameter);
        this.rewindTrailMesh.ApplyMeshData(vertices, triangles);
    }

    // apply the rewind state nearest to the given position
    public void HandleRewindTrailSelected(Vector3 position)
    {
        if (this.characterRewindStates.Count == 0) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // transform given world space position into the local space of the character parent
        Vector3 localPos = this.transform.parent.InverseTransformPoint(position);

        // select the rewind state closest to the given position
        int closestIndex = 0;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < this.characterRewindStates.Count; i++)
        {
            float distance = Vector3.Distance(localPos, this.characterRewindStates[i].localPos);

            if (distance < closestDistance)
            {
                closestIndex = i;
                closestDistance = distance;
            }
        }

        // apply the selected rewind state
        this.ApplyStateAtIndex(closestIndex);
    }

    // let the attached character start walking or halt him (if he is already walking)
    public void HandleResumeOrHalt(bool run = false)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        if (this.navMeshAgent.enabled) this.HaltCharacter();
        else this.ResumeCharacter(run);
    }

    // let the attached character start walking
    private void ResumeCharacter(bool run = false)
    {
        // activate the nav mesh agent, resume the animator and set the resuming flag
        this.navMeshAgent.enabled = true;
        this.animator.speed = 1;
        this.SetResuming(true);

        // let character start walking towards the rewind state next in line
        this.resumeTargetIndex = this.GetNearestFollowingRewindStateIndex();
        this.WalkTowardsRewindState(this.resumeTargetIndex, run);
    }

    // get the index of the nearest following rewind state
    private int GetNearestFollowingRewindStateIndex()
    {
        // get list with the distances of the states from the current character position, still in the same order as the characterRewindStates list
        Vector3 characterLocalPos = this.character.transform.localPosition;
        List<float> distances = this.characterRewindStates.ConvertAll<float>(crs => Vector3.Distance(characterLocalPos, crs.localPos));

        // create a dictionary mapping from the states' indices to their distance
        Dictionary<int, float> distancesByIndex = distances.Select((distance, index) => new { index, distance }).ToDictionary(x => x.index, x => x.distance);

        // create a list with the indices of the states sorted by their distances (first index has smallest distance)
        List<int> sortedIndices = (from entry in distancesByIndex orderby entry.Value ascending select entry.Key).ToList();

        // return the bigger of the two closest indices to ensure that the character is walking towards the end of the rewind trail
        return sortedIndices[0] > sortedIndices[1] ? sortedIndices[0] : sortedIndices[1];
    }

    // halt the attached character
    private void HaltCharacter()
    {
        // deactivate the nav mesh agent, pause the animator and unset the resuming flag
        this.navMeshAgent.enabled = false;
        this.animator.speed = 0;
        this.SetResuming(false);
    }

    // choose the next rewind state to set as the target, return false if the final one has been reached
    public bool ResumingChooseNextTarget(bool run = false)
    {
        // final state has been reached, unset flag and return false
        if (this.resumeTargetIndex + 1 >= this.characterRewindStates.Count)
        {
            this.SetResuming(false);
            return false;
        }

        // let character walk towards the next state
        this.resumeTargetIndex++;
        this.WalkTowardsRewindState(this.resumeTargetIndex, run);

        return true;
    }

    // return the number of states that have been saved until now
    public int GetStateCount()
    {
        return this.characterRewindStates.Count;
    }

    // reset the rewind trail
    public void ResetTrail()
    {
        // clear the trail's mesh
        this.rewindTrailMesh.ClearMesh();

        // clear the trail renderer
        this.rewindTrail.Clear();

        // remove all position indicators
        foreach (GameObject indicator in this.indicators) Destroy(indicator);
        this.indicators.Clear();

        // clear the stored state structs
        this.characterRewindStates.Clear();

        // allow animator to play the idle animation at normal speed
        this.animator.speed = 1;

        // unset resuming flag
        this.SetResuming(false);
    }

    // let the character walk towards the rewind state with the given index
    private void WalkTowardsRewindState(int index, bool run = false)
    {
        this.character.StartWalkingTowards(this.transform.parent.TransformPoint(this.characterRewindStates[index].localPos), run);
    }

    // highlight the rewind trail by settings its material to the given one
    public void HighlightTrail(Material material)
    {
        // store the original material
        this.trailMaterial = this.rewindTrail.material;

        // apply the given material
        this.rewindTrail.material = material;
        this.rewindTrail.generateLightingData = true;
    }

    // reset the trail to its original material
    public void RestoreOriginalTrailMaterial()
    {
        this.rewindTrail.material = this.trailMaterial;
        this.rewindTrail.generateLightingData = false;
    }
}

#region DATA STRUCTS
// struct storing the current state of the given character
[System.Serializable]
public struct CharacterRewindState
{
    // transform data
    public Vector3 localPos;
    public Quaternion localRot;
    public Vector3 localScale;

    // animation data
    public int stateNameHash;
    public float normalizedTime;


    public CharacterRewindState(Transform character, Animator animator)
    {
        this.localPos = character.localPosition;
        this.localRot = character.rotation;
        this.localScale = character.localScale;

        AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        this.stateNameHash = animatorStateInfo.shortNameHash;
        this.normalizedTime = animatorStateInfo.normalizedTime;
    }
}
#endregion