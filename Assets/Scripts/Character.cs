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

// script controlling an individual character
public class Character : MonoBehaviour
{
    public enum VisualizationMode {humanoid, sphere};
    public enum TraceMode {none, trail, ghostHumanoid, ghostSphere};

    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Animator animator;
    [SerializeField] private Rewindable rewindable;
    [SerializeField] private GameObject[] humanoidParts;
    [SerializeField] private GameObject spherePart;
    [SerializeField] private float closeThresholdMultiplier = 2;

    [Header("Traces")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GhostTrace ghostTraceHumanoid;
    [SerializeField] private GhostTrace ghostTraceSphere;

    private CharacterManager characterManager;

    private bool autoRotate = false;
    private Vector3 startingPos;
    private Quaternion startingRot;
    private List<Vector3> destinations;
    private bool fleeing = false;
    private bool closeToDestination = false;

    private void Awake()
    {
        this.destinations = new List<Vector3>();
    }

    void Update()
    {
        if (this.autoRotate)
        {
            // look at the camera/user
            Vector3 targetPos = Camera.main.transform.position;
            targetPos.y = this.transform.position.y;
            this.transform.LookAt(targetPos);
        }

        if (this.navMeshAgent.enabled && (this.animator.GetBool("isWalking") || this.animator.GetBool("isRunning")) && this.CheckIfArrived())
        {
            // if the character is currently walking because of rewind resuming and there is still a next target he can walk towards, don't stop him
            if (!this.rewindable.GetResuming() || !this.rewindable.ResumingChooseNextTarget(true))
            {
                // change animation when the character reached its destination
                this.animator.SetBool("isWalking", false);
                this.animator.SetBool("isRunning", false);

                // stop recording rewind positions
                this.rewindable.SetRecording(false);
            }
        }

        // tell character manager that this character just got close to its destination
        if (this.fleeing && !this.closeToDestination && this.CheckIfCloseToDestination())
        {
            this.closeToDestination = true;
            this.characterManager.OnCharacterCloseToDestination();
        }
    }

    // assign the character manager
    public void SetCharacterManager(CharacterManager characterManager)
    {
        this.characterManager = characterManager;
    }

    // change setting controlling whether the character keeps looking at the camera/user or not
    public void SetAutoRotate(bool val)
    {
        this.autoRotate = val;
        // store current state as starting point when auto rotation is stopped
        if (!val) this.SetStartingPosAndRot(this.transform.position);
    }

    // store current position and rotation as the character's starting point
    public void SetStartingPosAndRot(Vector3 pos)
    {
        this.startingPos = pos;
        this.startingRot = this.transform.rotation;
    }

    // give the character a new destination
    public void AddDestination(Vector3 dest, bool startWalking)
    {
        this.destinations.Add(dest);
        // only start walking to the new destination, if desired
        if (startWalking) this.StartWalkingTowards(dest);
    }

    // remove the destination with the given index
    public void RemoveDestination(int i)
    {
        this.destinations.RemoveAt(i);
        // TODO: handle case when the NavMeshAgent's current destination is removed
    }

    // remove all destinations
    public void ClearDestinations()
    {
        this.destinations.Clear();
    }

    // start fleeing from the given fires
    public void StartFleeing(Vector3[] firePositions)
    {
        // don't execute, if the character is already fleeing
        if (this.fleeing) return;

        // set flag and start recording rewind states
        this.fleeing = true;
        this.rewindable.SetRecording(true);

        // initialize data for the direction calculation
        Vector3[] fireDirections = new Vector3[firePositions.Length];
        Vector3[] destinationDirections = new Vector3[this.destinations.Count];
        Dictionary<int, float> destinationScores = new Dictionary<int, float>();
        Vector3 characterPos = this.transform.position;

        // calculate directions from the character's position to each of the fires
        for (int i = 0; i < firePositions.Length; i++)
        {
            Vector3 dir = firePositions[i] - characterPos;
            dir.y = 0;
            fireDirections[i] = dir.normalized;
        }

        // calculate directions from the character's position to each of the destinations and use them to calculate a score for each one
        for (int i = 0; i < destinationDirections.Length; i++)
        {
            // calculate direction
            Vector3 dir = this.destinations[i] - characterPos;
            dir.y = 0;
            destinationDirections[i] = dir.normalized;

            // a smaller score is better
            float score = 0;

            // calculate score based on the dot products between fire-directions and the current destination
            foreach (Vector3 fireDir in fireDirections)
            {
                float dotProduct = Vector3.Dot(destinationDirections[i], fireDir);
                // positive dot product means same direction -> gives being in the same direction as a fire more weight
                score += (dotProduct > 0) ? 2 * dotProduct : dotProduct;
            }

            // store the calculated store
            destinationScores[i] = score;
        }

        if (destinationScores.Count == 0) return;

        // select best destination and start walking towards it
        int bestIndex = destinationScores.OrderBy(kvp => kvp.Value).First().Key;
        this.StartWalkingTowards(this.destinations[bestIndex], true);
    }

    // let NavMeshAgent of the character walk towards the given destination and change animation accordingly
    public void StartWalkingTowards(Vector3 dest, bool run = false)
    {
        this.navMeshAgent.destination = dest;
        this.animator.SetBool(run ? "isRunning" : "isWalking", true);
    }

    // reset the character to its starting point
    public void ResetToStartingPos()
    {
        // set flags
        this.fleeing = false;
        this.closeToDestination = false;

        // apply starting position and rotation
        this.transform.position = this.startingPos;
        this.transform.rotation = this.startingRot;

        // show idle animation
        this.animator.SetBool("isWalking", false);
        this.animator.SetBool("isRunning", false);

        // enable nav mesh agent, but set his destination to the starting pos, so that he doesn't start walking
        this.navMeshAgent.enabled = true;
        this.navMeshAgent.destination = this.startingPos;

        // reset traces and rewind trail
        this.ResetTraces();
        this.rewindable.ResetTrail();
    }

    // remove all traces of the character
    public void ResetTraces()
    {
        this.ghostTraceHumanoid.DestroyAllGhosts();
        this.ghostTraceSphere.DestroyAllGhosts();
        this.trailRenderer.Clear();
    }

    // check, if the character reached its current destination
    private bool CheckIfArrived()
    {
        return (!this.navMeshAgent.pathPending && this.navMeshAgent.remainingDistance <= this.navMeshAgent.stoppingDistance);
    }

    // check, if the character is close to its current destination
    private bool CheckIfCloseToDestination()
    {
        return this.navMeshAgent.hasPath && this.navMeshAgent.remainingDistance <= this.navMeshAgent.stoppingDistance * this.closeThresholdMultiplier;
    }

    // stop the NavMeshAgent
    public void StopNavMeshAgent()
    {
        this.navMeshAgent.isStopped = true;
    }

    // start the NavMeshAgent
    public void StartNavMeshAgent(bool tryToRecover = true)
    {
        // start agent, if he stands on the NavMesh
        if (this.navMeshAgent.isOnNavMesh) this.navMeshAgent.isStopped = false;
        else if (tryToRecover)
        {
            // try to recover by reenabling the agent (set parameter to false to prevent endless loop)
            Debug.LogWarning("NavMeshAgent is not on a NavMesh! Trying to recover by disabling and enabling the agent...");
            this.navMeshAgent.enabled = false;
            this.navMeshAgent.enabled = true;
            this.StartNavMeshAgent(false);
        }
        else Debug.LogError("NavMeshAgent is not on a NavMesh!");
    }

    // set visualization mode and show/hide the according parts of the character
    public void SetVisualizationMode(VisualizationMode mode)
    {
        bool humanoidMode = mode == VisualizationMode.humanoid;

        foreach (GameObject go in this.humanoidParts)
        {
            go.SetActive(humanoidMode);
        }
        
        this.spherePart.SetActive(!humanoidMode);

        this.animator.enabled = humanoidMode;
    }

    // set trace mode
    public void SetTraceMode(TraceMode mode)
    {
        this.trailRenderer.enabled = mode == TraceMode.trail;
        this.ghostTraceHumanoid.SetActive(mode == TraceMode.ghostHumanoid);
        this.ghostTraceSphere.SetActive(mode == TraceMode.ghostSphere);
    }

    // set trace length
    public void SetTraceLength(TraceMode mode, float length)
    {
        if (mode == TraceMode.trail) this.trailRenderer.time = length;
        else if (mode == TraceMode.ghostHumanoid) this.ghostTraceHumanoid.SetLength(length);
        else if (mode == TraceMode.ghostSphere) this.ghostTraceSphere.SetLength(length);
    }

    // set trace transparency
    public void SetTraceTransparency(TraceMode mode, float transparency)
    {
        if (mode == TraceMode.trail) this.ApplyTrailTransparency(transparency);
        else if (mode == TraceMode.ghostHumanoid) this.ghostTraceHumanoid.SetTransparency(transparency);
        else if (mode == TraceMode.ghostSphere) this.ghostTraceSphere.SetTransparency(transparency);
    }

    // apply trace transparency to the trail
    private void ApplyTrailTransparency(float transparency)
    {
        Color color = this.trailRenderer.material.color;
        color.a = 1 - transparency;
        this.trailRenderer.material.color = color;
    }

    // set trace color
    public void SetTraceColor(Color? colorNullable)
    {
        if (colorNullable == null) return;

        // preserve original transparency of the trail trace
        Color color = (Color)colorNullable;
        float originalOpacity = this.trailRenderer.material.color.a;
        color.a = originalOpacity;

        // apply the given color to the traces
        this.trailRenderer.material.color = color;
        this.ghostTraceHumanoid.SetColor(color);
        this.ghostTraceSphere.SetColor(color);

        // apply the given color to the rewind trail
        this.rewindable.SetTrailColor(color);
    }

    // only show the ghosts that have been placed before the state with the given index was saved
    public void ApplyRewindStateToTraces(int index)
    {
        this.ghostTraceHumanoid.ApplyRewindStateIndex(index);
        this.ghostTraceSphere.ApplyRewindStateIndex(index);
    }
}
