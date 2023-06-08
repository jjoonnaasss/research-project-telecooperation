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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Character;

// script managing all individual characters, including their creation and destruction
public class CharacterManager : MonoBehaviour
{
    [SerializeField] private LayerMask characterLayer;
    [SerializeField] private LayerMask rewindTrailLayer;
    [SerializeField] private Transform characterHeightCapsule;
    [SerializeField] private LayerMask characterHeightCapsuleLayer;
    [SerializeField] private Transform characterParent;
    [SerializeField] private float characterHeight;
    [SerializeField] private Transform indicatorParent;

    [Header("Prefabs")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject destinationIndicatorPrefab;

    [Header("Settings")]
    [SerializeField] private float characterTranslationSpeed = 1;
    [SerializeField] private float characterRotationSpeed = 100;
    [SerializeField] private float characterDeleteDestroyDelay = 2;
    [SerializeField] private Material bodyMaterialDefault;
    [SerializeField] private Material bodyMaterialTransparent;
    [SerializeField] private Material bodyMaterialEditMode;
    [SerializeField] private Material bodyMaterialDeleted;
    [SerializeField] private Material jointMaterialDefault;
    [SerializeField] private float destinationIndicatorHeight = 3;
    [SerializeField] private float initialMaterialOpacity = 1;
    [SerializeField] private float triggerFullOpacityThreshold = 1;
    [SerializeField] private float lengthScale = 20;
    [SerializeField] private int infiniteLengthVal = 3600;
    [SerializeField] private float defaultLengthSetting = 0.1f;
    [SerializeField] private float defaultTransparencySetting = 0;
    [SerializeField] private float maxDestinationDeleteDistance = 0.5f;
    [SerializeField] private Color[] characterColors;

    // character variables
    private List<GameObject> characters = new List<GameObject>();
    private bool characterCreationMode = false;
    private GameObject newCharacter;
    private Transform characterToEdit;
    private Color characterToEditColor;
    private float currentMaterialOpacity;
    private List<Vector3> currentDestinations;
    private List<GameObject> destinationIndicators;
    private VisualizationMode activeVisualization = VisualizationMode.humanoid;
    private TraceMode activeTrace = TraceMode.none;
    private Dictionary<TraceMode, float> traceLengthSettings = new Dictionary<TraceMode, float>();
    private Dictionary<TraceMode, float> traceTransparencySettings = new Dictionary<TraceMode, float>();

    private float simulationStartTime;
    private int charactersCloseToDestination;

    private void Awake()
    {
        ManagerCollection.characterManager = this;

        this.currentDestinations = new List<Vector3>();
        this.destinationIndicators = new List<GameObject>();
    }

    // check if character creation mode is active
    public bool GetCreationMode()
    {
        return this.characterCreationMode;
    }

    // (de-)activate character creation mode
    public void SetCreationMode(bool mode)
    {
        this.characterCreationMode = mode;
    }

    // check if character edit mode is active
    public bool GetEditModeActive()
    {
        return this.characterToEdit;
    }

    // get individual characters
    public GameObject[] GetCharacters()
    {
        return this.characters.ToArray();
    }

    // get current destinations
    public Vector3[] GetCurrentDestinations()
    {
        return this.currentDestinations.ToArray();
    }

    // update/scale height of the character currently being created (based on the pointer of the given controller)
    public void UpdateNewCharacterHeight(Transform visualizer, bool pointerController)
    {
        // make sure that we have a valid height capsule pointer position
        Vector3 heightCapsulePointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, this.characterHeightCapsuleLayer);
        if (!Single.IsInfinity(heightCapsulePointerPos.x))
        {
            visualizer.position = heightCapsulePointerPos;
            ManagerCollection.gameManager.hmd.SetControllerRayEndPoint(heightCapsulePointerPos);
            this.SetCurrentCharacterHeight(heightCapsulePointerPos.y - ManagerCollection.alignmentManager.GetFloor().position.y);
        }
    }

    // check if there currently is a character being created/scaled
    public bool GetHeightAdjustmentInProgress()
    {
        return this.characterHeightCapsule.gameObject.activeInHierarchy;
    }

    // get character hit by the ray of the given controller, returns null if no character is hit
    public Transform GetCharacterHitByRay(bool pointerController)
    {
        return ManagerCollection.gameManager.hmd.GetTransformHitByRay(pointerController, this.characterLayer);
    }

    // get raycast hit between the ray of the given controller and a rewind trail, returns null if no trail is hit
    public RaycastHit? GetRewindTrailRaycastHit(bool pointerController)
    {
        return ManagerCollection.gameManager.hmd.GetControllerRaycastHit(pointerController, this.rewindTrailLayer);
    }

    // start character creation mode
    public void EnterCharacterCreationMode()
    {
        ManagerCollection.gameManager.SetCreationMode(true);
        ManagerCollection.obstacleManager.ExitObstacleEditMode();  // includes updating the lastInteraction time
        this.SetCreationMode(true);
        ManagerCollection.statusTextManager.ShowCharacterCreationMode();
    }

    // stop character creation mode
    public void ExitCharacterCreationMode()
    {
        ManagerCollection.gameManager.SetCreationMode(true); // enable overall creation mode, because obstacle placement is activated next
        this.ExitCharacterEditMode(); // includes updating the lastInteraction time
        this.SetCreationMode(false);
        ManagerCollection.obstacleManager.RestartObstaclePlacement(); // start obstacle creation mode
    }

    // (re-)start character creation mode
    private void RestartCharacterCreation()
    {
        this.FinalizeNewCharacter();
        ManagerCollection.statusTextManager.ShowCharacterCreationMode();
    }

    // finish creation of the current new character
    private void FinalizeNewCharacter()
    {
        // deactivate capsule used for getting character-height pointer through collision between capsule and controller ray
        this.characterHeightCapsule.gameObject.SetActive(false);

        if (this.newCharacter)
        {
            this.newCharacter.GetComponent<Character>().SetAutoRotate(false);
            this.characters.Add(this.newCharacter);
            this.newCharacter = null;
        }
    }

    // either create new character or finalize the current new character
    public void HandleCharacterCreation(bool pointerController, int floorLayer)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // instantiate new character
        if (!this.newCharacter)
        {
            // make sure that we have a valid floor pointer position
            Vector3 floorPointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, floorLayer);
            if (Single.IsInfinity(floorPointerPos.x)) return;

            // instantiate character
            this.CreateCharacter(floorPointerPos);
        }
        // scale current new character
        else
        {
            // make sure that we have a valid height capsule pointer position
            Vector3 heightCapsulePointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, this.characterHeightCapsuleLayer);
            if (Single.IsInfinity(heightCapsulePointerPos.x)) return;

            // scale the character
            this.SetCurrentCharacterHeight(heightCapsulePointerPos.y - ManagerCollection.alignmentManager.GetFloor().position.y);

            // finalize current new character and restart character creation mode
            this.RestartCharacterCreation();
        }
    }

    // instantiate a new character
    private void CreateCharacter(Vector3 pos)
    {
        // select color for the new character
        Color? characterColor = this.GetNextCharacterColor();

        // instantiate new character and configure its appearance
        this.newCharacter = Instantiate(this.characterPrefab, pos, Quaternion.identity, this.characterParent);
        this.newCharacter.GetComponentsInChildren<SkinnedMeshRenderer>()[1].material = this.jointMaterialDefault;
        this.ApplyOpacityToCharacter(this.newCharacter, this.currentMaterialOpacity, characterColor);

        // initialize character-script of the new character
        Character character = this.newCharacter.GetComponent<Character>();
        character.SetCharacterManager(this);
        character.SetTraceColor(characterColor);
        character.SetStartingPosAndRot(pos);
        character.SetVisualizationMode(this.activeVisualization);
        this.ApplyTraceSettings(character, this.activeTrace);
        character.SetTraceMode(this.activeTrace);
        character.SetAutoRotate(true);
        foreach (Vector3 dest in this.currentDestinations) character.AddDestination(dest, false);

        // activate capsule used for getting character-height pointer through collision between capsule and controller ray
        this.characterHeightCapsule.gameObject.SetActive(true);
        this.characterHeightCapsule.position = pos;
    }

    // scale the current new character
    public void SetCurrentCharacterHeight(float height)
    {
        float factor = height / this.characterHeight;
        this.newCharacter.transform.localScale = new Vector3(factor, factor, factor);
    }

    // start edit mode for the given character
    public void EnterCharacterEditMode(Transform character)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // store original color of the character to edit
        SkinnedMeshRenderer smr = character.GetComponentInChildren<SkinnedMeshRenderer>();
        this.characterToEditColor = smr.material.color;

        // apply the edit mode material
        smr.material = this.bodyMaterialEditMode;

        // store reference to the character to be edited
        this.characterToEdit = character;

        // show instructions for the edit mode
        ManagerCollection.statusTextManager.ShowCharacterEditMode();
    }

    // stop character edit mode
    public void ExitCharacterEditMode()
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        if (this.characterToEdit)
        {
            // store new position and rotation as starting point of the edited character
            this.characterToEdit.GetComponent<Character>().SetStartingPosAndRot(this.characterToEdit.position);

            // restore original appearance of the character
            this.ApplyOpacityToCharacter(this.characterToEdit.gameObject, this.currentMaterialOpacity, this.characterToEditColor);
        }

        // go back to the character creation mode
        this.characterToEdit = null;
        this.RestartCharacterCreation();
    }

    // handle input for the character edit mode
    public void HandleCharacterEditInput()
    {
        // translate character
        Vector2 leftInput = ManagerCollection.gameManager.hmd.GetLeftJoystickInput();

        Vector3 xTranslation = Camera.main.transform.right;
        xTranslation.y = 0;
        Vector3 zTranslation = Camera.main.transform.forward;
        zTranslation.y = 0;

        this.characterToEdit.Translate((xTranslation * leftInput.x + zTranslation * leftInput.y) * this.characterTranslationSpeed * Time.deltaTime, Space.World);

        // rotate character
        Vector2 rightInput = ManagerCollection.gameManager.hmd.GetRightJoystickInput();
        this.characterToEdit.Rotate(new Vector3(0, rightInput.x * this.characterRotationSpeed * Time.deltaTime));
    }

    // delete the given character
    public void DeleteCharacter(Transform character, bool instant = false)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // change appearance of the character (e.g. red material), remove his traces and destroy him after the configured delay
        if (this.characters.Remove(character.gameObject))
        {
            character.GetComponentInChildren<SkinnedMeshRenderer>().material = this.bodyMaterialDeleted;
            character.GetComponent<Character>().ResetTraces();
            Destroy(character.gameObject, instant ? 0 : this.characterDeleteDestroyDelay);
        }
    }

    // delete an existing destination or create a new one
    public void HandleDestinationInput(bool pointerController, int floorLayer, bool startWalking)
    {
        // make sure that we have a valid floor pointer position
        Vector3 floorPointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, floorLayer);
        if (Single.IsInfinity(floorPointerPos.x)) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // if the input was triggered close enough to an existing destination, delete it instead of creating a new one
        if (this.TryDeletingDestination(floorPointerPos)) return;

        // add new destination
        this.AddDestination(floorPointerPos, startWalking);
    }

    // create new destination at the given position
    public void AddDestination(Vector3 dest, bool startWalking = false)
    {
        // only allow destinations placed on the floor
        dest.y = ManagerCollection.alignmentManager.GetFloor().position.y;

        // store new destination in the list
        this.currentDestinations.Add(dest);

        // instantiate new indicator and store it in the list
        GameObject indicator = Instantiate(this.destinationIndicatorPrefab, dest, Quaternion.identity, this.indicatorParent);
        indicator.GetComponent<LineRenderer>().SetPositions(new Vector3[] { dest, dest + Vector3.up * this.destinationIndicatorHeight });
        indicator.SetActive(true);
        this.destinationIndicators.Add(indicator);

        // hand the new destination over to the characters
        this.ApplyNewDestination(dest, startWalking);
    }

    // hand the given destination over to the characters
    private void ApplyNewDestination(Vector3 dest, bool startWalking)
    {
        foreach (GameObject character in this.characters)
        {
            character.GetComponent<Character>().AddDestination(dest, startWalking);
        }
    }

    // delete the destination closest to the given position, if there is at least one close enough
    private bool TryDeletingDestination(Vector3 floorPointerPos)
    {
        Dictionary<int, float> closeEnoughDestinations = new Dictionary<int, float>();

        // collect all destinations close enough to the given position
        for (int i = 0; i < this.currentDestinations.Count; i++)
        {
            float distance = Vector3.Distance(floorPointerPos, this.currentDestinations[i]);
            if (distance <= this.maxDestinationDeleteDistance) closeEnoughDestinations.Add(i, distance);
        }

        // check, if there is at least one destination close enough
        if (closeEnoughDestinations.Count == 0) return false;

        // delete the closest destination, tell the characters to delete it as well and remove the according indicator
        int closestIndex = closeEnoughDestinations.OrderBy(kvp => kvp.Value).First().Key;
        Destroy(this.destinationIndicators[closestIndex]);
        this.destinationIndicators.RemoveAt(closestIndex);
        this.currentDestinations.RemoveAt(closestIndex);
        foreach (GameObject character in this.characters) character.GetComponent<Character>().RemoveDestination(closestIndex);

        return true;
    }

    // reset all characters to their starting points and remove all fires
    public void ResetCharacters()
    {
        foreach (GameObject character in this.characters) character.GetComponent<Character>().ResetToStartingPos();

        ManagerCollection.fireManager.RemoveAllFires();
        ManagerCollection.statusTextManager.HideTimer();
    }

    // stop the NavMeshAgents of all characters
    public void StopNavMeshAgents()
    {
        foreach (GameObject character in this.characters) character.GetComponent<Character>().StopNavMeshAgent();
    }

    // start the NavMeshAgents of all characters
    public void StartNavMeshAgents()
    {
        foreach (GameObject character in this.characters) character.GetComponent<Character>().StartNavMeshAgent();
    }

    // start simulation (let all characters flee from the fires)
    public void StartFleeing()
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // store time when the simulation started and reset counter
        this.simulationStartTime = Time.time;
        this.charactersCloseToDestination = 0;

        // collect fire positions and let characters start fleeing
        Vector3[] firePositions = ManagerCollection.fireManager.GetFirePositions();
        foreach (GameObject character in this.characters) character.GetComponent<Character>().StartFleeing(firePositions);
    }

    // increase counter of characters, that are close to their destination
    public void OnCharacterCloseToDestination()
    {
        this.charactersCloseToDestination++;

        // display time the simulation took once all characters are near their destination
        if (this.charactersCloseToDestination >= this.characters.Count) ManagerCollection.statusTextManager.ShowTimer(Time.time - this.simulationStartTime);
    }

    // apply the given opacity to the characters
    public void HandleOpacityInput(float opacity)
    {
        if (opacity == this.currentMaterialOpacity) return;

        this.currentMaterialOpacity = opacity;
        foreach (GameObject character in this.characters) this.ApplyOpacityToCharacter(character, opacity);
    }

    // apply given opacity to the given character
    private void ApplyOpacityToCharacter(GameObject character, float opacity, Color? characterColor = null)
    {
        // meshrenderer of the sphere
        MeshRenderer meshRenderer = character.GetComponentInChildren<MeshRenderer>(true);
        // meshrenderers of the humanoid
        SkinnedMeshRenderer[] skinnedMeshRenderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // use previous color if no new one was given
        if (characterColor == null) characterColor = meshRenderer.material.color;

        // apply opaque material to sphere and humanoid
        if (opacity >= this.triggerFullOpacityThreshold)
        {
            meshRenderer.material = this.bodyMaterialDefault;
            skinnedMeshRenderers[0].material = this.bodyMaterialDefault;
        }
        // apply transparent material to sphere and humanoid
        else
        {
            meshRenderer.material = this.bodyMaterialTransparent;
            skinnedMeshRenderers[0].material = this.bodyMaterialTransparent;
        }

        // add custom color to sphere and humanoid
        meshRenderer.material.color = (Color)characterColor;
        skinnedMeshRenderers[0].material.color = (Color)characterColor;
        
        // apply opacity to the sphere
        Color color = meshRenderer.material.color;
        color.a = opacity;
        meshRenderer.material.color = color;

        // apply opacity to the humanoid
        foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
        {
            color = smr.material.color;
            color.a = opacity;
            smr.material.color = color;
        }
    }

    // set the visualization mode and hand it over to the characters
    public void SetVisualizationMode(VisualizationMode mode)
    {
        this.activeVisualization = mode;
        foreach (GameObject character in this.characters) character.GetComponent<Character>().SetVisualizationMode(mode);
    }

    // set the trace mode and hand it over to the characters
    public void SetTraceMode(TraceMode mode)
    {
        this.activeTrace = mode;
        foreach (GameObject character in this.characters)
        {
            character.GetComponent<Character>().SetTraceMode(mode);
            this.ApplyTraceSettings(character.GetComponent<Character>(), mode);
        }
    }

    // apply the current trace settings for the given trace mode to the given character
    private void ApplyTraceSettings(Character character, TraceMode mode)
    {
        character.SetTraceTransparency(mode, this.GetTraceTransparencySetting(mode));
        float calculatedLength = (this.GetTraceLengthSetting(mode) == 1) ? this.infiniteLengthVal : this.GetTraceLengthSetting(mode) * this.lengthScale;
        character.SetTraceLength(mode, calculatedLength);
    }

    // set the trace length setting, calculate the actual value based on the configured max value, hand it over to the characters and return it
    public float SetTraceLength(float length)
    {
        this.traceLengthSettings[this.activeTrace] = length;
        float calculatedLength = (length == 1) ? this.infiniteLengthVal : length * this.lengthScale;
        foreach (GameObject character in this.characters) character.GetComponent<Character>().SetTraceLength(this.activeTrace, calculatedLength);

        return calculatedLength;
    }

    // set the trace transparency setting and hand it over to the characters
    public void SetTraceTransparency(float transparency)
    {
        this.traceTransparencySettings[this.activeTrace] = transparency;
        foreach (GameObject character in this.characters) character.GetComponent<Character>().SetTraceTransparency(this.activeTrace, transparency);
    }

    // get the trace length setting for the given trace mode
    public float GetTraceLengthSetting(TraceMode mode)
    {
        if (!this.traceLengthSettings.ContainsKey(mode)) this.SetTraceLength(this.defaultLengthSetting);
        return this.traceLengthSettings[mode];
    }

    // get the trace transparency setting for the given trace mode
    public float GetTraceTransparencySetting(TraceMode mode)
    {
        if (!this.traceTransparencySettings.ContainsKey(mode)) this.SetTraceTransparency(this.defaultTransparencySetting);
        return this.traceTransparencySettings[mode];
    }

    // delete all characters
    public void RemoveAllCharacters()
    {
        // delete all characters
        while (this.characters.Count > 0) this.DeleteCharacter(this.characters[0].transform, true);

        // reset variables
        this.newCharacter = null;
        this.characterToEdit = null;
    }

    // delete all destinations
    public void ClearDestinations()
    {
        // destroy all indicators
        foreach (GameObject indicator in this.destinationIndicators) Destroy(indicator);
        
        // clear lists
        this.destinationIndicators.Clear();
        this.currentDestinations.Clear();

        // clear lists in the Characters
        foreach (GameObject character in this.characters) character.GetComponent<Character>().ClearDestinations();
    }

    // recreate a character with the given position, rotation and scale (executed when loading saved data)
    public void RecreateCharacter(Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        // get color for the new character
        Color? characterColor = this.GetNextCharacterColor();

        // instantiate new characters and apply the given transformations
        GameObject characterObject = Instantiate(this.characterPrefab, this.characterParent);
        characterObject.transform.localPosition = localPos;
        characterObject.transform.localRotation = localRot;
        characterObject.transform.localScale = localScale;

        // apply the desired opacity
        characterObject.GetComponentsInChildren<SkinnedMeshRenderer>()[1].material = this.jointMaterialDefault;
        this.ApplyOpacityToCharacter(characterObject, this.currentMaterialOpacity, characterColor);

        // apply the desired settings to the character
        Character character = characterObject.GetComponent<Character>();
        character.SetCharacterManager(this);
        character.SetTraceColor(characterColor);
        character.SetStartingPosAndRot(characterObject.transform.position);
        character.SetVisualizationMode(this.activeVisualization);
        this.ApplyTraceSettings(character, this.activeTrace);
        character.SetTraceMode(this.activeTrace);

        // store new character in the list
        this.characters.Add(characterObject);
    }

    // determine color for the next new character
    private Color? GetNextCharacterColor()
    {
        if (this.characterColors.Length == 0) return null;

        int index = this.characters.Count % this.characterColors.Length;
        return this.characterColors[index];
    }

    // set starting points of all characters to their current positions
    public void SetAllStartingPositions()
    {
        foreach (GameObject characterObject in this.characters) characterObject.GetComponent<Character>().SetStartingPosAndRot(characterObject.transform.position);
    }
}