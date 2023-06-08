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
using System.IO;
using UnityEngine;
using static OVRSpatialAnchor;

// script handling saving and loading the current setup of obstacles and characters
public class SaveLoadManager : MonoBehaviour
{
    [SerializeField] private Transform saveLoadParent;
    [SerializeField] private Transform obstacleParent;
    [SerializeField] private Transform characterParent;
    [SerializeField] private Transform ghostParent;
    [SerializeField] private Transform[] positionVisualizers;
    [SerializeField] private float spatialAnchorSaveDelay = 1;

    // anchor alignment variables
    private Vector3 alignmentPosition;
    private bool alignmentPositionSet = false;
    private bool inProgress = false;

    private string persistentPath;
    private SaveData saveData;
    private OVRSpatialAnchor spatialAnchor = null;

    private void Awake()
    {
        ManagerCollection.saveLoadManager = this;

        this.persistentPath = Application.persistentDataPath;
    }

    #region METHODS BELONGING TO SAVING ONLY
    // write save data to a new save file or overwrite the given save file
    public void SaveData(string fileToOverwrite = "Create new file")
    {
        string fileName = (fileToOverwrite == "Create new file") ? this.GetNewFileName() : fileToOverwrite;
        this.WriteSaveFile(fileName);

        this.inProgress = false;
        // hide alignment position visualizers
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);
    }

    // get file name for a new save file
    private string GetNewFileName()
    {
        int environmentCount = this.GetExistingSaveFileNames().Count;

        // increase count further to find unused name (necessary for the case that e.g. environment1 was deleted, when environment2 already existed)
        while (File.Exists(this.persistentPath + "/environment" + environmentCount.ToString() + ".json")) environmentCount++;

        return "/environment" + environmentCount.ToString() + ".json";
    }

    // write save data to the save file with the given name
    private void WriteSaveFile(string fileName)
    {
        // collect save data and convert it to a json string
        SaveData saveData = this.CollectSaveData();
        string json = JsonUtility.ToJson(saveData);

        // write the json string to the save file
        File.WriteAllText(this.persistentPath + fileName, json);

        // show success message to the user
        Debug.LogWarning("Wrote save-data to file: " + this.persistentPath + fileName);
        ManagerCollection.statusTextManager.ShowSuccessMessage("Successfully created a save file!");
    }

    // collect save data from the current state of the application
    private SaveData CollectSaveData()
    {
        // collect all obstacles and characters
        GameObject[] obstacles = ManagerCollection.obstacleManager.GetObstacles();
        GameObject[] characters = ManagerCollection.characterManager.GetCharacters();

        // extract relevant data from the obstacles and characters
        ObstacleData[] obstacleData = new ObstacleData[obstacles.Length];
        CharacterData[] characterData = new CharacterData[characters.Length];
        for (int i = 0; i < obstacles.Length; i++) obstacleData[i] = new ObstacleData(obstacles[i].transform);
        for (int i = 0; i < characters.Length; i++) characterData[i] = new CharacterData(characters[i].transform);

        // extract relevant data from the parent objects
        ParentData obstacleParentData = new ParentData(this.obstacleParent);
        ParentData characterParentData = new ParentData(this.characterParent);

        // extract relevant data from the destinations
        Vector3[] destinations = ManagerCollection.characterManager.GetCurrentDestinations();
        for (int i = 0; i < destinations.Length; i++) destinations[i] = this.saveLoadParent.InverseTransformPoint(destinations[i]);

        // TODO: maybe check whether the spatial anchor is null
        return new SaveData(obstacleData, characterData, obstacleParentData, characterParentData, destinations, this.spatialAnchor.Uuid);
    }

    // handle alignment input of the given controller
    public void HandleAlignmentInput(bool controller)
    {
        // make sure that we have a valid tip position
        Vector3 tipPosition = ManagerCollection.gameManager.hmd.GetControllerTipPosition(controller);
        if (Single.IsInfinity(tipPosition.x)) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        if (!this.alignmentPositionSet)
        {
            // record new alignment position and place a position visualizer
            this.alignmentPosition = tipPosition;
            this.positionVisualizers[0].position = tipPosition;
            this.positionVisualizers[0].gameObject.SetActive(true);

            this.alignmentPositionSet = true;
            ManagerCollection.statusTextManager.ShowAnchorAlignmentPosition();

            // execute anchor alignment
            this.AlignAnchor();
        }
    }

    // execute anchor alignment
    private void AlignAnchor()
    {
        // clear parents if the save-/load-alignment was already executed before to prevent the obstacles/characters/destinations from being moved with the saveLoadParent
        if (this.saveLoadParent.childCount > 0) this.DiscardParentHierarchy();

        if (this.spatialAnchor != null)
        {
            // destroy old spatial anchor component
            Destroy(this.spatialAnchor);
            this.spatialAnchor = null;
        }

        // align saveLoadParent with the recorded alignment position
        this.saveLoadParent.position = this.alignmentPosition;

        // set saveLoadParent as parent of the obstacleParent, characterParent and ghostParent
        this.CreateParentHierarchy();

        // create new spatial anchor at the position of the save load parent
        this.CreateNewSpatialAnchor();
    }

    // create a new spatial anchor based on the transform of the saveLoadParent
    private void CreateNewSpatialAnchor()
    {
        // create spatial anchor component
        this.saveLoadParent.gameObject.AddComponent<OVRSpatialAnchor>();
        this.spatialAnchor = this.saveLoadParent.GetComponent<OVRSpatialAnchor>();

        // save the spatial anchor after the configured delay (delay in order to make sure, that the new component is ready)
        Invoke("SaveSpatialAnchor", this.spatialAnchorSaveDelay);
    }

    // save the current spatial anchor to local storage and signal the ui manager, once the anchor was aligned
    private void SaveSpatialAnchor()
    {
        // save spatial anchor to the local storage
        SaveOptions saveOptions = new SaveOptions();
        saveOptions.Storage = OVRSpace.StorageLocation.Local;
        this.spatialAnchor.Save(saveOptions, this.SpatialAnchorSaveOnCompleteCallback);
    }

    // callback for saving the spatial anchor
    private void SpatialAnchorSaveOnCompleteCallback(OVRSpatialAnchor anchor, bool success)
    {
        if (!success)
        {
            // show an error, if the spatial anchor could not be saved to the local storage
            Debug.LogError("The spatial anchor with the following UUID could not be saved to local storage: " + anchor.Uuid.ToString());
            ManagerCollection.statusTextManager.ShowErrorMessage("The spatial anchor with the following UUID could not be saved to local storage: " + anchor.Uuid.ToString());
            // close save/load page
            ManagerCollection.uiManager.SetSaveLoadVisibility(false);
        }
        else
        {
            Debug.LogWarning("Successfully saved spatial anchor with the following UUID to local storage: " + anchor.Uuid.ToString());

            // signal the ui manager, that the anchor was aligned
            ManagerCollection.uiManager.OnSaveLoadAnchorCompleted();
            ManagerCollection.statusTextManager.ShowAnchorAlignmentDone();
        }
    }

    // delete old spatial anchor, not used at the moment
    private void DestroySpatialAnchor()
    {
        // erase the old anchor
        EraseOptions eraseOptions = new EraseOptions();
        eraseOptions.Storage = OVRSpace.StorageLocation.Local;
        this.spatialAnchor.Erase(eraseOptions, this.UpdateSpatialAnchorCallback);

    }

    // called when the erase-call for the old anchor has finished
    private void UpdateSpatialAnchorCallback(OVRSpatialAnchor anchor, bool success)
    {
        if (!success) Debug.LogError("The spatial anchor with the following UUID could not be erased from local storage: " + anchor.Uuid.ToString());

        // destroy old spatial anchor component
        Destroy(this.spatialAnchor);
        this.spatialAnchor = null;

        // run alignment again, now without the old spatial anchor
        this.AlignAnchor();
    }

    //clear parents of the obstacleParent, characterParent and ghostParent
    private void DiscardParentHierarchy()
    {
        this.obstacleParent.parent = null;
        this.characterParent.parent = null;
        this.ghostParent.parent = null;
    }
    #endregion

    #region METHODS BELONGING TO LOADING ONLY
    // load save data from the file with the given name
    public void LoadData(string fileToLoad)
    {
        // check, if the given file actually exists
        if (!File.Exists(this.persistentPath + fileToLoad))
        {
            // show error message to the user
            Debug.LogError("Save-file not found: " + this.persistentPath + fileToLoad);
            ManagerCollection.statusTextManager.ShowErrorMessage("Save-file not found: " + this.persistentPath + fileToLoad);
            // close save/load page
            ManagerCollection.uiManager.SetSaveLoadVisibility(false);

            return;
        }

        // delete existing characters, obstacles and destinations
        ManagerCollection.obstacleManager.RemoveAllObstaclesAndArcades();
        ManagerCollection.characterManager.RemoveAllCharacters();
        ManagerCollection.characterManager.ClearDestinations();

        // read save data from the file
        this.saveData = this.ReadSaveFile(fileToLoad);

        // destroy old spatial anchor component
        if (this.spatialAnchor != null)
        {
            Destroy(this.spatialAnchor);
            this.spatialAnchor = null;
        }

        // load the spatial anchor with the uuid read from the save file; this also recreates all objects when finished
        this.LoadSpatialAnchor(Guid.Parse(this.saveData.anchorUUID));
    }

    // read data from the save file with the given name
    private SaveData ReadSaveFile(string fileName)
    {
        string json = File.ReadAllText(this.persistentPath + fileName);

        return JsonUtility.FromJson<SaveData>(json);
    }

    // load spatial anchor with the given uuid, localize it and bind it to the saveLoadParent
    private void LoadSpatialAnchor(Guid uuid)
    {
        // load anchor with the given uuid from the local storage
        LoadOptions loadOptions = new LoadOptions();
        loadOptions.StorageLocation = OVRSpace.StorageLocation.Local;
        loadOptions.Uuids = new List<Guid> { uuid };
        loadOptions.MaxAnchorCount = 1;

        if (!LoadUnboundAnchors(loadOptions, this.HandleUnboundAnchor))
        {
            // show an error, if the anchor couldn't be loaded
            Debug.LogError("Couldn't load spatial anchor from the local storage!");
            ManagerCollection.statusTextManager.ShowErrorMessage("Couldn't load spatial anchor from the local storage!");
            // close save/load page
            ManagerCollection.uiManager.SetSaveLoadVisibility(false);
        }
    }

    // localizes the anchor contained in the given list and binds it to the saveLoadParent afterwards
    private void HandleUnboundAnchor(UnboundAnchor[] anchors)
    {
        // show an error, if there was an error while loading the required anchor
        if (anchors == null || anchors.Length == 0)
        {
            Debug.LogError("The spatial anchor with the UUID from the save file could not be found in the local storage!");
            ManagerCollection.statusTextManager.ShowErrorMessage("The spatial anchor with the UUID from the save file could not be found in the local storage!");
            // close save/load page
            ManagerCollection.uiManager.SetSaveLoadVisibility(false);

            return;
        }

        // localize the anchor and bind it afterwards
        anchors[0].Localize(this.BindAnchorToSaveLoadParent);
    }

    // binds the saveLoadParent to the anchor contained in the given list
    private void BindAnchorToSaveLoadParent(UnboundAnchor unboundAnchor, bool success)
    {
        // show an error, if the given anchor could not be localized
        if (!success)
        {
            Debug.LogError("The spatial anchor with the UUID from the save file could not be localized!");
            ManagerCollection.statusTextManager.ShowErrorMessage("The spatial anchor with the UUID from the save file could not be localized!");
            // close save/load page
            ManagerCollection.uiManager.SetSaveLoadVisibility(false);
            return;
        }

        // create spatial anchor component
        this.saveLoadParent.gameObject.AddComponent<OVRSpatialAnchor>();
        this.spatialAnchor = this.saveLoadParent.GetComponent<OVRSpatialAnchor>();

        // align saveLoadParent with the position and orientation of the loaded unbound anchor
        this.saveLoadParent.position = unboundAnchor.Pose.position;
        this.saveLoadParent.rotation = unboundAnchor.Pose.rotation;

        // bind the loaded anchor to the created anchor component
        unboundAnchor.BindTo(this.spatialAnchor);

        // recreate the saved objects, now that we have localized and bound the room anchor
        this.RecreateObjectsFromSaveData();
    }

    // recreate all saved objects from the given save data; is executed as a callback from loading the spatial anchor
    private void RecreateObjectsFromSaveData()
    {
        // set saveLoadParent as parent of the obstacleParent, characterParent and ghostParent
        this.CreateParentHierarchy();

        // restore position and rotation of the parent objects (ghostParent can use data of the characterParent as well)
        this.obstacleParent.localPosition = this.saveData.obstacleParentData.localPos;
        this.obstacleParent.localRotation = this.saveData.obstacleParentData.localRot;
        this.characterParent.localPosition = this.saveData.characterParentData.localPos;
        this.characterParent.localRotation = this.saveData.characterParentData.localRot;
        this.ghostParent.localPosition = this.saveData.characterParentData.localPos;
        this.ghostParent.localRotation = this.saveData.characterParentData.localRot;

        // recreate the stored obstacles and characters
        foreach (ObstacleData obstacle in this.saveData.obstacleData) ManagerCollection.obstacleManager.RecreateObstacle(obstacle.localPos, obstacle.localRot, obstacle.localScale);
        foreach (CharacterData character in this.saveData.characterData) ManagerCollection.characterManager.RecreateCharacter(character.localPos, character.localRot, character.localScale);

        // recreate the stored destinations
        foreach (Vector3 dest in this.saveData.destinations) ManagerCollection.characterManager.AddDestination(this.saveLoadParent.TransformPoint(dest));

        // loading is finished, hide the alignment position visualizers
        this.inProgress = false;
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);
    }

    // skip the anchor alignment, used when loading as you don't need to align the anchor in this case
    public void SkipAnchorAlignment()
    {
        this.alignmentPositionSet = true;
    }
    #endregion

    #region METHODS BELONGING TO BOTH SAVING AND LOADING
    // collect names of the existing save files
    public List<string> GetExistingSaveFileNames()
    {
        FileInfo[] files = new DirectoryInfo(this.persistentPath).GetFiles();
        List<string> saveFileNames = new List<string>();

        foreach (FileInfo file in files) if (file.Name.StartsWith("environment")) saveFileNames.Add("/" + file.Name);

        return saveFileNames;
    }

    // (re-)start alignment of the anchor
    public void RestartAnchorAlignment()
    {
        this.inProgress = true;
        this.alignmentPositionSet = false;
    }

    // check, whether the anchor alignment is currently in progress
    public bool GetInProgress()
    {
        return this.inProgress;
    }

    // hide alignment position visualizers when the anchor alignment is canceled
    public void OnCancel()
    {
        this.inProgress = false;
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);
    }

    // set saveLoadParent as parent of the obstacleParent, characterParent and ghostParent
    private void CreateParentHierarchy()
    {
        this.obstacleParent.parent = this.saveLoadParent;
        this.characterParent.parent = this.saveLoadParent;
        this.ghostParent.parent = this.saveLoadParent;
    }
    #endregion
}

#region DATA STRUCTS
// struct storing relevant data of a single obstacle
[System.Serializable]
public struct ObstacleData
{
    public Vector3 localPos;
    public Quaternion localRot;
    public Vector3 localScale;

    public ObstacleData(Transform obstacle)
    {
        this.localPos = obstacle.localPosition;
        this.localRot = obstacle.rotation;
        this.localScale = obstacle.localScale;
    }
}

// struct storing relevant data of a single character
[System.Serializable]
public struct CharacterData
{
    public Vector3 localPos;
    public Quaternion localRot;
    public Vector3 localScale;

    public CharacterData(Transform obstacle)
    {
        this.localPos = obstacle.localPosition;
        this.localRot = obstacle.rotation;
        this.localScale = obstacle.localScale;
    }
}

// struct storing relevant data of a parent object
[System.Serializable]
public struct ParentData
{
    public Vector3 localPos;
    public Quaternion localRot;

    public ParentData(Transform parent)
    {
        this.localPos = parent.localPosition;
        this.localRot = parent.rotation;
    }
}

// struct storing a complete set of save data
[System.Serializable]
public struct SaveData
{
    public ObstacleData[] obstacleData;
    public CharacterData[] characterData;
    public ParentData obstacleParentData;
    public ParentData characterParentData;
    public Vector3[] destinations;
    public string anchorUUID;

    public SaveData(ObstacleData[] obstacleData, CharacterData[] characterData, ParentData obstacleParentData, ParentData characterParentData, Vector3[] destinations, Guid anchorUUID)
    {
        this.obstacleData = obstacleData;
        this.characterData = characterData;
        this.obstacleParentData = obstacleParentData;
        this.characterParentData = characterParentData;
        this.destinations = destinations;
        this.anchorUUID = anchorUUID.ToString();
    }
}
#endregion