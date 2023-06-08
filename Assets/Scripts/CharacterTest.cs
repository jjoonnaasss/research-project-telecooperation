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
using UnityEngine;
using UnityEngine.AI;

// script used for testing character functionality, not used in the actual simulation
public class CharacterTest : MonoBehaviour
{
    [SerializeField] private Vector3 target;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform ghostParent;
    [SerializeField] private GameObject prefabGhostHumanoid;

    [Header("Trail mesh test")]
    [SerializeField] private TrailRenderer testTrail;
    [SerializeField] private MeshCollider testMeshCollider;

    private float timer;
    private bool started = false;
    private bool goalReached = false;
    private int trailMeshTestCounter = 0;
    private int replayCounter = -1;

    private List<Vector3> gizmoPositions = new List<Vector3>();

    void Start()
    {
        //this.SetDestination(this.target);
        Invoke("StartRecording", 0.1f);
    }

    void Update()
    {
        /*
        if (this.CheckIfArrived()) this.animator.SetBool("isWalking", false);
        timer += Time.deltaTime;

        if (timer > 1)
        {
            this.timer = 0;
            this.SpawnMeshGhost();
        }
        */
        if (this.started && !this.goalReached && !this.navMeshAgent.pathPending && this.navMeshAgent.remainingDistance < 0.5f)
        {
            Debug.Log("reached");
            this.goalReached = true;
            Time.timeScale = 1;
            Time.fixedDeltaTime *= 10;
            this.animator.SetBool("isWalking", false);
            this.animator.SetBool("isRunning", false);
            this.GetComponent<Rewindable>().SetRecording(false);
            //this.replayCounter = 0;
        }
    }

    private void FixedUpdate()
    {
        //this.TestTrailMeshGeneration();
        if (this.replayCounter >= 0)
        {
            switch (this.replayCounter)
            {
                case 50:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(0);
                    Debug.Log(50);
                    break;
                case 100:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(1);
                    break;
                case 150:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(2);
                    break;
                case 200:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(3);
                    break;
                case 250:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(4);
                    break;
                case 300:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(5);
                    break;
                case 350:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(6);
                    break;
                case 400:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(7);
                    break;
                case 450:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(8);
                    break;
                case 500:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(9);
                    break;
                case 550:
                    this.GetComponent<Rewindable>().ApplyStateAtIndex(10);
                    break;
            }
            this.replayCounter++;
        }
    }

    private void StartRecording()
    {
        this.GetComponent<Rewindable>().SetRecording(true);
        Time.timeScale = 5;
        Time.fixedDeltaTime /= 10;
        /*NavMeshPath path = new NavMeshPath();
        this.navMeshAgent.CalculatePath(this.target, path);
        this.gizmoPositions.AddRange(path.corners);
        Debug.Log(path.corners.Length);*/

        this.SetDestination(this.target);

        this.started = true;
    }

    private void TestTrailMeshGeneration()
    {
        switch (this.trailMeshTestCounter)
        {
            case 0:
                this.testTrail.transform.Translate(new Vector3(0, 0, 1));
                break;
            case 10:
                this.testTrail.transform.Translate(new Vector3(2, 0, 1));
                break;
            case 20:
                this.testTrail.transform.Translate(new Vector3(0, 0, 1));
                break;
            case 30:
                this.testTrail.transform.Translate(new Vector3(-2, 0, 1));
                break;
            case 40:
                this.testTrail.transform.Translate(new Vector3(0, 0, 1));
                this.testTrail.emitting = false;
                break;
            case 50:
                Mesh mesh;
                if (this.testMeshCollider.sharedMesh == null) mesh = new Mesh();
                else mesh = this.testMeshCollider.sharedMesh;
                mesh.Clear();
                (Vector3[] vertices, int[] triangles) = TrailMeshGenerator.GenerateMeshData(this.testTrail, 0.25f);
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                this.testMeshCollider.sharedMesh = mesh;
                break;
        }

        this.trailMeshTestCounter++;        
    }

    private void PrintMeshSizes()
    {
        foreach (SkinnedMeshRenderer smr in this.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Debug.LogWarning(smr.bounds.size);
        }
    }

    public void SetDestination(Vector3 dest)
    {
        this.navMeshAgent.destination = dest;
        this.animator.SetBool("isWalking", true);
    }

    private bool CheckIfArrived()
    {
        return (!this.navMeshAgent.pathPending && this.navMeshAgent.remainingDistance <= this.navMeshAgent.stoppingDistance);
    }

    private void SpawnMeshGhost()
    {
        // instantiate new ghost
        GameObject ghost = Instantiate(this.prefabGhostHumanoid, this.transform.position, this.transform.rotation, this.ghostParent);

        // get references to the ghost's mesh renderers and filters
        MeshFilter[] ghostMeshFilters = ghost.GetComponentsInChildren<MeshFilter>(true);
        MeshRenderer[] ghostMeshRenderers = ghost.GetComponentsInChildren<MeshRenderer>(true);

        // get references to this character's skinned mesh renderers
        SkinnedMeshRenderer[] skinnedMeshRenderers = this.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // bake the surface mesh
        Mesh surfaceMesh = new Mesh();
        skinnedMeshRenderers[0].BakeMesh(surfaceMesh);

        // bake the joints mesh
        Mesh jointsMesh = new Mesh();
        skinnedMeshRenderers[1].BakeMesh(jointsMesh);

        // copy surface material to the baked surface mesh renderer
        Material surfaceMaterial = new Material(skinnedMeshRenderers[0].material);
        ghostMeshRenderers[0].material = surfaceMaterial;

        // copy joints material to the baked joints mesh renderer
        Material jointsMaterial = new Material(skinnedMeshRenderers[1].material);
        ghostMeshRenderers[1].material = jointsMaterial;

        // apply the baked meshes to the ghost's mesh filters
        ghostMeshFilters[0].mesh = surfaceMesh;
        ghostMeshFilters[1].mesh = jointsMesh;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Vector3 pos in this.gizmoPositions) Gizmos.DrawSphere(pos, 0.1f);
    }
}
