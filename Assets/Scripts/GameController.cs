using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System;
using TMPro;


public class GameController : MonoBehaviour
{
    private int xGrid = 10;
    private int zGrid = 10;
    private int spawnedUnits = 6;
    private GameState gameState;

    public bool selected = false;
    public GameObject entitySelected;

    public GameObject worldAnchor;
    public GameObject[,] grid;

    public float seed = 0;

    // UI Elements
    private GameObject canvas;
    public bool showUI = true;
    public bool manualMode = false;

    // llm vars
    private char[,] visualGrid;

    // logging
    string logPath;

    IEnumerator Start()
    {
        // find canvas
        canvas = GameObject.Find("Canvas");
        // grab gameobjects
        if (!GameObject.Find("GameState").TryGetComponent<GameState>(out gameState))
        {
            Debug.Log("GameState not found");
            yield break;
        }
        gameState = GameObject.Find("GameState").GetComponent<GameState>();
        gameState.gameController = gameObject;

        // when the game starts, create a new log file under Assets/Log using current date
        string date = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logPath = "Assets/Log/" + date + ".txt";
        System.IO.File.WriteAllText(logPath, "Game Log " + date + "\n");

        // init grid
        grid = new GameObject[xGrid, zGrid];
        // Generate Perlin noise
        float[,] heights = new float[xGrid, zGrid]; // Replace with your actual dimensions
        for (int x = 0; x < xGrid; x++)
        {
            for (int z = 0; z < zGrid; z++)
            {
                float scale = 0.05f; // Adjust the scale factor for lower resolution
                // if seed is not initialized, initialize it
                if (seed == 0)
                {
                    seed = UnityEngine.Random.Range(0f, 100f);
                }
                float height = Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale) * 4.0f;
                height = Mathf.Clamp(height, 0.5f, 4.0f);
                height = Mathf.Round(height);
                heights[x, z] = height;
            }
        }

        // Smooth the heights
        float[,] smoothedHeights = new float[xGrid, zGrid];
        for (int x = 0; x < xGrid; x++)
        {
            for (int z = 0; z < zGrid; z++)
            {
                // only smooth some of the blocks
                if (!(x % 3 == 0 && z % 3 == 0))
                {
                    smoothedHeights[x, z] = heights[x, z];
                    continue;
                }
                float total = 0.0f;
                int count = 0;

                // Sum the values of the neighbors
                for (int nx = -1; nx <= 1; nx++)
                {
                    for (int nz = -1; nz <= 1; nz++)
                    {
                        int ix = x + nx;
                        int iz = z + nz;

                        // Check if the neighbor is within the array bounds
                        if (ix >= 0 && ix < xGrid && iz >= 0 && iz < zGrid)
                        {
                            total += heights[ix, iz];
                            count++;
                        }
                    }
                }

                // Calculate the average and store it in the smoothed array
                smoothedHeights[x, z] = (total / count);
            }
        }
        // Use the smoothed heights to set the cube heights
        // Generate Cubes
        for (int x = 0; x < xGrid; x++)
        {
            for (int z = 0; z < zGrid; z++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grid[x,z] = cube;
                cube.transform.parent = worldAnchor.transform;
                cube.transform.position = new Vector3(x, 10, z);
                cube.transform.localScale = new Vector3(1, 0.5f, 1);

                // Use the smoothed height for this cube
                float height = smoothedHeights[x, z];
                // round to nearest int
                height = Mathf.Round(height);
                cube.transform.localScale = new Vector3(1, height, 1);
                cube.GetComponent<Renderer>().material = Resources.Load("Materials/Smooth", typeof(Material)) as Material;
                if ((x + z) % 2 == 0)
                {
                    cube.GetComponent<Renderer>().material.color = Color.white;
                }
                else
                {
                    cube.GetComponent<Renderer>().material.color = Color.grey;
                }
                cube.AddComponent<Entity>();
                cube.GetComponent<Entity>().x = x;
                cube.GetComponent<Entity>().z = z;
                cube.GetComponent<Entity>().entityType = Entity.EntityType.Terrain;
                cube.GetComponent<Entity>().classType = Entity.ClassType.ClassA;

                // Spawn the Prefab MvQuad ontop of the cube
                GameObject mvQuad = Instantiate(Resources.Load("Prefabs/MvQuad", typeof(GameObject))) as GameObject;
                mvQuad.transform.parent = cube.transform;

                // set it to the height of the cube
                mvQuad.transform.position = new Vector3(x, height/2+10.01f, z);
                mvQuad.SetActive(false);

                // display coordinates on the cubes surface
                GameObject textObject = new GameObject("Text");
                textObject.transform.parent = cube.transform;
                TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
                textMesh.text = $"{x}.{z}";
                textMesh.fontSize = 2;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.color = Color.black;
                textMesh.fontStyle = FontStyles.Bold;
                // set the position of the text to the top of the cube
                textObject.transform.position = new Vector3(x-0.3f, 10.01f + height/2, z-0.3f);
                // rotation -> face upwards
                textObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                // lock the scale to 1
                textObject.transform.localScale = new Vector3(1, 1, 1);
                

                // cube starts falling
                cube.AddComponent<DropDown>();
                cube.GetComponent<DropDown>().floor = height / 2; // so start from the same level floor

                yield return ExecuteAfterTime(0.025f);
            }
        }

        HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

        for (int i = 0; i < spawnedUnits; i++)
        {
            Vector3 randomPosition;
            do
            {
                randomPosition = new Vector3(UnityEngine.Random.Range(0, xGrid), 10, UnityEngine.Random.Range(0, zGrid));
            } while (occupiedPositions.Contains(randomPosition));

            occupiedPositions.Add(randomPosition);

            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.parent = worldAnchor.transform;
            capsule.transform.position = randomPosition;
            capsule.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            capsule.AddComponent<DropDown>();
            capsule.GetComponent<DropDown>().floor = 0.5f + grid[(int)randomPosition.x, (int)randomPosition.z].transform.localScale.y;
            capsule.AddComponent<Entity>();
            capsule.tag = "NPC";
            Color color;
            if (i % 2 == 0)
            {
                // Blue spectrum
                color = Color.HSVToRGB(UnityEngine.Random.Range(0.55f, 0.65f), UnityEngine.Random.Range(0.9f, 1.0f), 1.0f);
            }
            else
            {
                // Red spectrum
                color = Color.HSVToRGB(UnityEngine.Random.Range(0.0f, 0.05f), UnityEngine.Random.Range(0.8f, 1.0f), 1.0f);
            }
            capsule.GetComponent<Entity>().color = color;
            capsule.GetComponent<Entity>().x = (int)randomPosition.x;
            capsule.GetComponent<Entity>().z = (int)randomPosition.z;
            capsule.GetComponent<Entity>().entityType = Entity.EntityType.Unit;
            capsule.GetComponent<Entity>().classType = Entity.ClassType.ClassA;
            // 0 blue, 1 red
            capsule.GetComponent<Entity>().team = i%2;
            capsule.GetComponent<Entity>().refresh();

            yield return ExecuteAfterTime(0.025f);
        }

        // game is prepared (gamestate listens for this change)
        gameState.gridReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameState.gridReady)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // if the raycast hits something
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // if the hit object has an Entity script attached
                if (hit.collider.gameObject.GetComponent<Entity>() != null)
                {
                    OnClickEntity(hit);
                }
                else
                {
                    Deselect();
                }
            }
        }
        // if user presses Q or E, the gamegrid rotates 90° over 1 second
        if (Input.GetKeyDown(KeyCode.Q) && gameState.gridReady)
        {
            StartCoroutine(Rotate(Vector3.up * 90, 1));
        }
        if (Input.GetKeyDown(KeyCode.E) && gameState.gridReady)
        {
            StartCoroutine(Rotate(Vector3.up * -90, 1));
        }
    }

    IEnumerator Rotate(Vector3 byAngles, float inTime)    {
        gameState.gridReady = false;
        var fromAngle = worldAnchor.transform.rotation;
        var toAngle = Quaternion.Euler(worldAnchor.transform.eulerAngles + byAngles);
        // ensure toAngle is a multiple of 90°
        toAngle.eulerAngles = new Vector3(0, Mathf.Round(toAngle.eulerAngles.y / 90) * 90, 0);
        // switch where target xCord and zCord are set for worldAnchor based on toAngle
        // this is needed because rotating worldAnchor created some shift in our isometric perspective
        float xCord = 0;
        float zCord = 0;
        switch ((int)toAngle.eulerAngles.y)
        {
            case 0:
                xCord = 0;
                zCord = 0;
                break;
            case 90:
                xCord = 0;
                zCord = 9f;
                break;
            case 180:
                xCord = 9f;
                zCord = 9f;
                break;
            case 270:
                xCord = 9f;
                zCord = 0;
                break;
        }

        for (var t = 0f; t < 1; t += Time.fixedDeltaTime / inTime)
        {
            // rotate worldAnchor
            worldAnchor.transform.rotation = Quaternion.Slerp(fromAngle, toAngle, t);
            // move worldAnchor to xCord and zCord
            worldAnchor.transform.position = Vector3.MoveTowards(worldAnchor.transform.position, new Vector3(xCord, 0, zCord), Time.fixedDeltaTime * 10);
            // in the last frame, always snap to the target rotation and position
            if (t + Time.fixedDeltaTime / inTime >= 1)
            {
                worldAnchor.transform.rotation = toAngle;
                worldAnchor.transform.position = new Vector3(xCord, 0, zCord);
            }
            yield return null;
        }
        gameState.gridReady = true;
    }
    IEnumerator ExecuteAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
    }

    /// <summary>
    /// Handles the logic when an entity is clicked.
    /// </summary>
    /// <param name="hit">The RaycastHit information of the clicked entity.</param>
    private void OnClickEntity(RaycastHit hit) {
        // early exit if no valid entity
        if (hit.collider.gameObject.GetComponent<Entity>().entityType != Entity.EntityType.Unit &&
            hit.collider.gameObject.GetComponent<Entity>().entityType != Entity.EntityType.Terrain) {
            Deselect();
            return;
        }
        GameObject previousEntitySelected = entitySelected;
        entitySelected = hit.collider.gameObject;
        // during a turn, clicking on a unit will attack it if its within 1 tile range
        if (entitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit && previousEntitySelected != null && previousEntitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit) {
            Debug.Log("Unit attacked by another unit");
            // if entitySelected is not on the same team as the current team, attack it
            if (entitySelected.GetComponent<Entity>().team != previousEntitySelected.GetComponent<Entity>().team) {
                // if entitySelected is within 1 tile range, attack it
                if (Mathf.Abs(entitySelected.GetComponent<Entity>().x - previousEntitySelected.GetComponent<Entity>().x) <= 1 &&
                    Mathf.Abs(entitySelected.GetComponent<Entity>().z - previousEntitySelected.GetComponent<Entity>().z) <= 1) {
                    // attack entitySelected
                    previousEntitySelected.GetComponent<Entity>().Attack(entitySelected);
                    gameState.actionsTaken++;
                    // deselect entitySelected
                    Deselect();
                    // deselect previousEntitySelected
                    previousEntitySelected.GetComponent<Entity>().selected = false;
                    previousEntitySelected.GetComponent<Entity>().refresh();
                    // add 1 action to gameState
                    return;
                }
            }
        }
        // if entitySelected is a terrain and has an active moveindicator, move the entitySelected to that position
        if (entitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Terrain &&
            entitySelected.transform.GetChild(0).gameObject.activeSelf && previousEntitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit)
        {
            // move previousEntitySelected to the position of the terrain
            previousEntitySelected.transform.position = new Vector3(entitySelected.GetComponent<Entity>().x, entitySelected.transform.localScale.y / 2 + 5.01f, entitySelected.GetComponent<Entity>().z);
            // update x and z of previousEntitySelected
            previousEntitySelected.GetComponent<Entity>().x = entitySelected.GetComponent<Entity>().x;
            previousEntitySelected.GetComponent<Entity>().z = entitySelected.GetComponent<Entity>().z;
            previousEntitySelected.GetComponent<DropDown>().floor = entitySelected.transform.localScale.y / 2 + entitySelected.transform.position.y + 0.5f;
            previousEntitySelected.GetComponent<DropDown>().enabled = true;
            // deselect previousEntitySelected
            previousEntitySelected.GetComponent<Entity>().selected = false;
            previousEntitySelected.GetComponent<Entity>().refresh();
            // deselect terrain
            DeselectTerrain();
            // deselect entitySelected
            entitySelected = null;
            selected = false;
            UpdateUI();
            return;
        }
        // deselect all other entities
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (GameObject npc in npcs)
        {
            // reset shader and selected of other units if applicable
            if (npc.GetComponent<Entity>().selected && !(npc == entitySelected))
            {
                npc.GetComponent<Entity>().selected = false;
                npc.GetComponent<Entity>().refresh();
            }
        }
        DeselectTerrain();
        // toggle selected status of entitySelected
        if (!entitySelected.GetComponent<Entity>().selected)
        {
            Select();
        }
        else
        {
            Deselect();
        }
        UpdateUI();
    }

    private void Deselect() {
        if (entitySelected != null)
        {
            if (entitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit)
            {
                entitySelected.GetComponent<Entity>().selected = false;
                entitySelected.GetComponent<Entity>().refresh();
            }
        }
        DeselectTerrain();
        entitySelected = null;
        selected = false;
        UpdateUI();
    }

    private void Select() {
        entitySelected.GetComponent<Entity>().selected = true;
        entitySelected.GetComponent<Entity>().refresh();
        selected = true;
    }

    private void DeselectTerrain() {
        // set each grid tiles moveindicator if entity.type terrain to false
        foreach (GameObject tile in grid)
        {
            if (tile.GetComponent<Entity>().entityType == Entity.EntityType.Terrain)
            {
                tile.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    private void UpdateUI()
    {
        GameObject unitUI = GameObject.Find("UnitUI");
        if (unitUI != null)
        {
            if (unitUI.TryGetComponent<UIDocument>(out var uiDocument))
            {
                // fetch info of currently selected Unit
                if (entitySelected == null)
                {
                    uiDocument.enabled = false;
                    return;
                }
                if (entitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit)
                {
                    // find "UI_Camera" (projecting to render texture) and move it infront of the selected Unit
                    GameObject uiCamera = GameObject.Find("UI_Camera");
                    uiCamera.transform.position = entitySelected.transform.position + new Vector3(-1.5f, 1.5f, -1.5f);
                    uiCamera.transform.LookAt(entitySelected.transform);

                    uiDocument.enabled = selected;
                    uiDocument.rootVisualElement.Q<Label>("Name").text = entitySelected.GetComponent<Entity>().unitName;
                    // if team = 0, set Team Blue, else Team Red
                    if (entitySelected.GetComponent<Entity>().team == 0)
                    {
                        uiDocument.rootVisualElement.Q<Label>("Team").text = "Team Blue";
                    }
                    else
                    {
                        uiDocument.rootVisualElement.Q<Label>("Team").text = "Team Red";
                    }
                    uiDocument.rootVisualElement.Q<Label>("HPcurr").text = entitySelected.GetComponent<Entity>().HPcurr.ToString();
                    uiDocument.rootVisualElement.Q<Label>("HPmax").text = entitySelected.GetComponent<Entity>().HPmax.ToString();
                    uiDocument.rootVisualElement.Q<Label>("MPcurr").text = entitySelected.GetComponent<Entity>().MPcurr.ToString();
                    uiDocument.rootVisualElement.Q<Label>("MPmax").text = entitySelected.GetComponent<Entity>().MPmax.ToString();
                    uiDocument.rootVisualElement.Q<Label>("Class").text = entitySelected.GetComponent<Entity>().classType.ToString();
                }
            }
        }
    }

    public void toggleShowUI()
    {
        showUI = !showUI;
        canvas.transform.Find("Toggle_UI").GetComponent<UnityEngine.UI.Toggle>().SetIsOnWithoutNotify(showUI);
        if (!showUI)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.name != "Toggle_UI")
                {
                    child.gameObject.SetActive(false);
                }
            }
            foreach (GameObject cube in grid)
            {
                cube.transform.Find("Text").gameObject.SetActive(false);
            }
        } else {
            foreach (Transform child in canvas.transform)
            {
                if (child.name != "Toggle_UI")
                {
                    child.gameObject.SetActive(true);
                }
            }
            foreach (GameObject cube in grid)
            {
                cube.transform.Find("Text").gameObject.SetActive(true);
            }
        }
    }

    public void cameraDistance(string distance)
    {
        var cameraGO = GameObject.Find("Main Camera").GetComponent<Camera>();
        var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (distance == "Default")
        {
            camera.orthographicSize = 7.0f;
            cameraGO.transform.position = new Vector3(cameraGO.transform.position.x, 7.5f, cameraGO.transform.position.z);
            GameObject.Find("Background").transform.localScale = new Vector3(25.0f, 25.0f, 1);
        }
        else if (distance == "Far1")
        {
            // further by factor 1.25
            camera.orthographicSize = 8.75f;
            cameraGO.transform.position = new Vector3(cameraGO.transform.position.x, 8, cameraGO.transform.position.z);
            GameObject.Find("Background").transform.localScale = new Vector3(35.0f, 35.0f, 1);
        }
        else if (distance == "Far2")
        {
            // further by factor 1.5
            camera.orthographicSize = 10.5f;
            cameraGO.transform.position = new Vector3(cameraGO.transform.position.x, 9, cameraGO.transform.position.z);
            GameObject.Find("Background").transform.localScale = new Vector3(40.0f, 40.0f, 1);
        }
        else if (distance == "Close")
        {
            // closer by factor 0.75
            camera.orthographicSize = 5.25f;
            cameraGO.transform.position = new Vector3(cameraGO.transform.position.x, 4, cameraGO.transform.position.z);
            GameObject.Find("Background").transform.localScale = new Vector3(20.0f, 20.0f, 1);

        }
    }

    public void clickLoad0() {
        clickLoad(4, 2, 2);
        cameraDistance("Close");
    }

    public void clickLoad1() {
        clickLoad(5, 5, 4);
        cameraDistance("Default");
    }

    public void clickLoad2() {
        clickLoad(10, 10, 6);
        cameraDistance("Default");
    }

    public void clickLoad3() {
        clickLoad(12, 12, 8);
        cameraDistance("Far1");
    }

    public void clickLoad4() {
        clickLoad(14, 14, 10);
        cameraDistance("Far2");
    }

    /// <summary>
    /// Clears the board of all units and terrain, sets the grid size, spawns units, and starts the game.
    /// </summary>
    /// <param name="x">The x-coordinate of the grid size.</param>
    /// <param name="z">The z-coordinate of the grid size.</param>
    /// <param name="units">The number of units to spawn.</param>
    public void clickLoad(int x, int z, int units) {
        // clear the board of all units and terrain
        foreach (GameObject npc in GameObject.FindGameObjectsWithTag("NPC"))
        {
            Destroy(npc);
        }
        foreach (GameObject tile in grid)
        {
            Destroy(tile);
        }
        clearPanel();
        Deselect();
        xGrid = x;
        zGrid = z;
        spawnedUnits = units;
        // Start() in GameController and GameState
        StartCoroutine(Start());
        gameState.gridReady = false;
        gameState.Start();        
        writeToPanel("Template\nxGrid: " + xGrid + "\nzGrid: " + zGrid + "\nUnits: " + spawnedUnits);
        System.IO.File.WriteAllText("Assets/Prompts/rules.txt", Regex.Replace(System.IO.File.ReadAllText("Assets/Prompts/rules.txt"), @"\d+x\d+\sgrid", xGrid + "x" + zGrid + " grid"));     
    }

    public void clickReadInput()
    {
        StartCoroutine(readInput());
    }

    // chat.cs will write the llms aswer into input.txt
    // this function reads the input.txt, evaluates whether the instructions are valid according to the games ruleset and current state
    // and executes the instructions
    /// <summary>
    /// Reads instructions from the input.txt file and executes them if valid.
    /// </summary>
    public IEnumerator readInput()
    {
        // read instructions from IO/input.txt
        AddLog("Reading input.txt");
        string filePath = "Assets/IO/input.txt";
        string[] lines = System.IO.File.ReadAllLines(filePath);
        string instruction1 = "";
        string instruction2 = "";
        bool firstMatch = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            AddLog(line);
            // Perform regex on the line to extract the instructions
            string pattern = @"([A-Z]):(\d+\.\d+):(\d+\.\d+)";
            // can be multiple matches per line so use Collection
            MatchCollection matches = Regex.Matches(line, pattern);
            foreach (Match match in matches)
            {
                if (!firstMatch)
                {
                    firstMatch = true;
                    clearPanel();
                    writeToPanel("Instructions found in Assets/IO/input.txt");
                }
                string instruction = match.Value;
                if (instruction1 == "")
                {
                    instruction1 = instruction;
                    writeToPanel("Instruction 1: " + instruction1);
                    AddLog("Instruction 1: " + instruction1);
                }
                else
                {
                    instruction2 = instruction;
                    writeToPanel("Instruction 2: " + instruction2);
                    AddLog("Instruction 2: " + instruction2);
                    break; // max 2 instructions, stop scanning
                }
            }
        }
        if (!firstMatch)
        {
            clearPanel();
            writeToPanel("No instructions found in Assets/IO/input.txt");
            yield break;
        }
        // evaluate the instructions against the current existing game state
        // if the instructions are valid, execute them
        // if the instructions are invalid, print an error message in the panel
        if (evaluateInstructions(instruction1))
        {
            StartCoroutine(executeInstruction(instruction1));
        }
        yield return new WaitForSeconds(4);
        if (evaluateInstructions(instruction2))
        {
            StartCoroutine(executeInstruction(instruction2));
        }
    }

    // returns true if the instruction is valid
    /// <summary>
    /// Evaluates the given instruction and checks if it is a valid move or attack instruction.
    /// </summary>
    /// <param name="instruction">The instruction to evaluate in the format "M:1.0:1.0" or "A:1.0:1.0".</param>
    /// <returns>True if the instruction is valid, False otherwise.</returns>
    public bool evaluateInstructions(string instruction) {
        // split the instruction which comes as format M:1.0:1.0
        // into M or A for Move or Attack
        // and 1.0 and 1.0 for the Origin and Target coordinates
        try
        {
            string[] parts = instruction.Split(':');
            // validate that the origin coordinates is used by a unit
            // split parts[1] at . and save the first part as x and the second part as z
            string[] origin = parts[1].Split('.');
            string[] target = parts[2].Split('.');

            bool matchOrigin = false;
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
            foreach (GameObject npc in npcs)
            {
                // check if the origin coordinates match the x and z of the npc
                if (npc.GetComponent<Entity>().x == int.Parse(origin[0]) && npc.GetComponent<Entity>().z == int.Parse(origin[1]))
                {
                    matchOrigin = true;
                }
            }
            if (!matchOrigin)
            {
                writeToPanel("Error: Origin coordinates do not match any unit. "+instruction);
                AddLog("Error: Origin coordinates do not match any unit. "+instruction);
                return false;
            }
            if (parts[0] == "M")
            {
                // validate that the target coordinates are not occupied by a unit
                bool occupiedTarget = false;
                foreach (GameObject npc in npcs)
                {
                    // check if the target coordinates match the x and z of the npc
                    if (npc.GetComponent<Entity>().x == int.Parse(target[0]) && npc.GetComponent<Entity>().z == int.Parse(target[1]))
                    {
                        occupiedTarget = true;
                    }
                }
                if (occupiedTarget)
                {
                    writeToPanel("Error: Target coordinates are occupied by a unit. "+instruction);
                    AddLog("Error: Target coordinates are occupied by a unit. "+instruction);
                    return false;
                }
                // proceed with movement
                // max movement range is 3 tiles that can be walked in any direction
                // check using manhattan distance
                if (Mathf.Abs(int.Parse(target[0]) - int.Parse(origin[0])) + Mathf.Abs(int.Parse(target[1]) - int.Parse(origin[1])) > 3)
                {
                    writeToPanel("Error: Target coordinates are out of range. "+instruction);
                    AddLog("Error: Target coordinates are out of range. "+instruction);
                    return false;
                }
                // instruction is valid, print success to panel
                writeToPanel("Valid Move Instruction received: "+instruction);
                AddLog("Valid Move Instruction received: "+instruction);
                return true;
            }
            else if (parts[0] == "A")
            {
                // validate that the target coordinates are used by a unit
                bool occupiedTarget = false;
                foreach (GameObject npc in npcs)
                {
                    // check if the target coordinates match the x and z of the npc
                    if (npc.GetComponent<Entity>().x == int.Parse(target[0]) && npc.GetComponent<Entity>().z == int.Parse(target[1]))
                    {
                        occupiedTarget = true;
                    }
                }
                if (!occupiedTarget)
                {
                    writeToPanel("Error: Target coordinates are not occupied by any unit. "+instruction);
                    AddLog("Error: Target coordinates are not occupied by any unit. "+instruction);
                    return false;
                }
                // proceed with attack
                // max attack range is 1 tile in any cardinal direction
                if (Mathf.Abs(int.Parse(target[0]) - int.Parse(origin[0])) + Mathf.Abs(int.Parse(target[1]) - int.Parse(origin[1])) > 1)
                {
                    writeToPanel("Error: Attack Target is out of range. "+instruction);
                    AddLog("Error: Attack Target is out of range. "+instruction);
                    return false;
                }
                // instruction is valid, print success to panel
                writeToPanel("Valid Attack Instruction received: "+instruction);
                AddLog("Valid Attack Instruction received: "+instruction);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            writeToPanel("Error: Bad instruction format. " + instruction);
            AddLog("Error: Bad instruction format. " + instruction);
        }
        return false;
    }

    /// <summary>
    /// Executes the given instruction.
    /// </summary>
    /// <param name="instruction">The instruction to execute.</param>
    public IEnumerator executeInstruction(string instruction)
    {
        Deselect();
        UpdateUI();
        // split the instruction which comes as format M:1.0:1.0
        // into M or A for Move or Attack
        // and 1.0 and 1.0 for the Origin and Target coordinates
        string[] parts = instruction.Split(':');
        // split parts[1] at . and save the first part as x and the second part as z
        string[] origin = parts[1].Split('.');
        string[] target = parts[2].Split('.');
        if (parts[0] == "M")
        {
            // find unit with matching origin coordinates
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
            foreach (GameObject npc in npcs)
            {
                // check if the origin coordinates match the x and z of the npc
                if (npc.GetComponent<Entity>().x == int.Parse(origin[0]) && npc.GetComponent<Entity>().z == int.Parse(origin[1]))
                {
                    // set entitySelected to the npc
                    entitySelected = npc;
                    Select();
                    UpdateUI();
                    // wait for 1 second to display the moveindicator to user
                    yield return new WaitForSeconds(2);
                    // set previousEntitySelected to the npc
                    GameObject previousEntitySelected = entitySelected;
                    // set entitySelected to the target terrain
                    entitySelected = grid[int.Parse(target[0]), int.Parse(target[1])];
                    if (entitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Terrain &&
                        entitySelected.transform.GetChild(0).gameObject.activeSelf && previousEntitySelected.GetComponent<Entity>().entityType == Entity.EntityType.Unit)
                    {
                        // move previousEntitySelected to the position of the terrain
                        previousEntitySelected.transform.position = new Vector3(entitySelected.GetComponent<Entity>().x, entitySelected.transform.localScale.y / 2 + 5.01f, entitySelected.GetComponent<Entity>().z);
                        // update x and z of previousEntitySelected
                        previousEntitySelected.GetComponent<Entity>().x = entitySelected.GetComponent<Entity>().x;
                        previousEntitySelected.GetComponent<Entity>().z = entitySelected.GetComponent<Entity>().z;
                        previousEntitySelected.GetComponent<DropDown>().floor = entitySelected.transform.localScale.y / 2 + entitySelected.transform.position.y + 0.5f;
                        previousEntitySelected.GetComponent<DropDown>().enabled = true;
                        // deselect previousEntitySelected
                        previousEntitySelected.GetComponent<Entity>().selected = false;
                        previousEntitySelected.GetComponent<Entity>().refresh();
                        // deselect terrain
                        DeselectTerrain();
                        // deselect entitySelected
                        entitySelected = null;
                        selected = false;
                        UpdateUI();
                        yield break;
                    }
                } 
            }
        } else if (parts[0] == "A")
        {
            // find unit with matching origin coordinates
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
            foreach (GameObject npc in npcs)
            {
                // check if the origin coordinates match the x and z of the npc
                if (npc.GetComponent<Entity>().x == int.Parse(origin[0]) && npc.GetComponent<Entity>().z == int.Parse(origin[1]))
                {
                    // set entitySelected to the npc
                    entitySelected = npc;
                    Select();
                    UpdateUI();
                    // wait for 1 second to display the moveindicator to user
                    yield return new WaitForSeconds(2);
                    // set previousEntitySelected to the npc
                    GameObject previousEntitySelected = entitySelected;
                    // set entitySelected to the target unit
                    foreach (GameObject npc2 in npcs)
                    {
                        // check if the target coordinates match the x and z of the npc
                        if (npc2.GetComponent<Entity>().x == int.Parse(target[0]) && npc2.GetComponent<Entity>().z == int.Parse(target[1]))
                        {
                            entitySelected = npc2;
                            // range and team checks have been made in evaluateInstructions, so proceed with attack
                            previousEntitySelected.GetComponent<Entity>().Attack(entitySelected);
                            gameState.actionsTaken++;
                            // deselect entitySelected
                            Deselect();
                            // deselect previousEntitySelected
                            previousEntitySelected.GetComponent<Entity>().selected = false;
                            previousEntitySelected.GetComponent<Entity>().refresh();
                            // add 1 action to gameState
                            yield break;
                        }
                    }
                }
            }
        }
    }

    public void writeToPanel(string text)
    {
        canvas.transform.Find("Image_BG").Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text += "\n" + text;
    }

    public void clearPanel()
    {
        canvas.transform.Find("Image_BG").Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "";
    }

    /// <summary>
    /// Prints the current game state by creating a visual grid of dots and displaying the units for each team.
    /// </summary>
    public void PrintGameState()
    {
        // create a $xGrid by $zGrid grid of dots and store it in an array of chars
        visualGrid = new char[xGrid, zGrid];
        for (int i = 0; i < xGrid; i++)
        {
            for (int j = 0; j < zGrid; j++)
            {
                visualGrid[i, j] = '.';
            }
        }
        // for each unit, set the corresponding position in the visualGrid either P for team 1 or O for team 2
        // also create a list of units for each team in the format:
        // Player 1 Units:
        // P: (3,7) HP1
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        string temp;
        string listP1 = "Player 1 Units:";
        string listP2 = "Player 2 Units:";
        foreach (GameObject npc in npcs)
        {
            if (npc.GetComponent<Entity>().entityType == Entity.EntityType.Unit)
            {
                if (npc.GetComponent<Entity>().team == 0)
                {
                    visualGrid[npc.GetComponent<Entity>().x, npc.GetComponent<Entity>().z] = 'P';
                    temp = "P: (" + npc.GetComponent<Entity>().x + "," + npc.GetComponent<Entity>().z + ") HP" + npc.GetComponent<Entity>().HPcurr;
                    listP1 += "\n" + temp;
                }
                else
                {
                    visualGrid[npc.GetComponent<Entity>().x, npc.GetComponent<Entity>().z] = 'O';
                    temp = "O: (" + npc.GetComponent<Entity>().x + "," + npc.GetComponent<Entity>().z + ") HP" + npc.GetComponent<Entity>().HPcurr;
                    listP2 += "\n" + temp;
                }
            }
        }
        // writeToPanel the visualGrid and to the IO/output_visualGrid.txt
        // clear file
        System.IO.File.WriteAllText("Assets/IO/output_visualGrid.txt", "");
        // clear panel
        clearPanel();

        for (int i = 0; i < xGrid; i++)
        {
            string line = "";
            for (int j = 0; j < zGrid; j++)
            {
                line += visualGrid[i, j];
            }
            writeToPanel(line);
            // write to file
            System.IO.File.AppendAllText("Assets/IO/output_visualGrid.txt", line + "\n");
        }
        writeToPanel(listP1);
        writeToPanel(listP2);
        // write to file
        System.IO.File.AppendAllText("Assets/IO/output_visualGrid.txt", listP1 + "\n" + listP2);

        // also prepare the output.txt, which is a sandwich of Assets/Prompts/rules.txt, then Assets/IO/output_visualGrid.txt, then Assets/Prompts/question.txt
        string rules = System.IO.File.ReadAllText("Assets/Prompts/rules.txt");
        string output = System.IO.File.ReadAllText("Assets/IO/output_visualGrid.txt");
        string question = System.IO.File.ReadAllText("Assets/Prompts/question.txt");
        // depending on gameState.gameStateTeam prepend "You are player X and it's your turn. "
        if (gameState.gameStateTeam == 0)
        {
            question = "You are player 1 and it's your turn. " + question;
        }
        else
        {
            question = "You are player 2 and it's your turn. " + question;
        }
        System.IO.File.WriteAllText("Assets/IO/output.txt", "");
        System.IO.File.AppendAllText("Assets/IO/output.txt", rules + "\n" + output + "\n" + question);
    }

    // should be moved to clickInfer()
    // belongs to UI which is all tracked in GameController but should logically be in Chat.cs
    public void clickInferLocal()
    {
        // addlog Infering from Local Model + output.txt content
        AddLog("Infering from Local Model");
        string output = System.IO.File.ReadAllText("Assets/IO/output.txt");
        AddLog(output);
    }

    public void clickInferGPT()
    {
        AddLog("Infering from OpenAI GPT");
        string output = System.IO.File.ReadAllText("Assets/IO/output.txt");
        AddLog(output);
    }

    public void clickInferGemini()
    {
        AddLog("Infering from Google Gemini");
        string output = System.IO.File.ReadAllText("Assets/IO/output.txt");
        AddLog(output);
    }

    public void clickInferAA()
    {
        AddLog("Infering from Aleph Alpha");
        string output = System.IO.File.ReadAllText("Assets/IO/output.txt");
        AddLog(output);
    }

    public void AddLog(string text)
    {
        System.IO.File.AppendAllText(logPath, text + "\n");
    }
}
