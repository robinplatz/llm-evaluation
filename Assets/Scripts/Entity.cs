using UnityEngine;
 
public class Entity : MonoBehaviour
{
    public enum EntityType
    {
        Terrain,
        Unit
    }

    public enum ClassType
    {
        ClassA,
        ClassB
    }

    public EntityType entityType;
    public ClassType classType;
    public Color color;
    public bool selected = false;
    public int team = 0;
    public int actions = 2;
    public string unitName = "Name";
    public int movement = 4;
    public int attackRange = 1;
    public int attackDamage = 1;
    public int initiative = 1;
    public int x = 0;
    public int z = 0;
    public int HPcurr = 1;
    public int MPcurr = 10;
    public int HPmax = 10;
    public int MPmax = 10;	
    
    // Start is called before the first frame update
    void Start()
    {
        initiative = Random.Range(1, 100);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void refresh()
    {
        if (entityType == EntityType.Unit) {
            if (!selected) {
                // set the material to the Standard material
                GetComponent<Renderer>().material = Resources.Load<Material>("StandardMaterial");
                GetComponent<Renderer>().material.color = color;
            } else {
                GetComponent<Renderer>().material = Resources.Load<Material>("Materials/HaloMaterial");
                GetComponent<Renderer>().material.SetColor("_Color", color);
                ShowMoveIndicator();
            }
            return;
        }
    }

    /// <summary>
    /// Shows the move indicator for the entity.
    /// </summary>
    public void ShowMoveIndicator()
    {
        // Find the game controller
        GameObject gameController = GameObject.Find("GameController");
        if (gameController == null)
        {
            Debug.Log("GameController not found");
            return;
        }

        // Get the grid from the game controller
        GameController gameControllerScript = gameController.GetComponent<GameController>();
        if (gameControllerScript == null)
        {
            Debug.Log("GameController script not found");
            return;
        }
        GameObject[,] grid = gameControllerScript.grid;
        if (grid == null)
        {
            Debug.Log("Grid not found");
            return;
        }

        // Call the recursive function to show move indicators
        ShowMoveIndicatorRecursive(x, z, movement, grid);
    }

    /// <summary>
    /// Recursively shows the move indicator for a given position on the grid.
    /// </summary>
    /// <param name="startX">The starting X position.</param>
    /// <param name="startZ">The starting Z position.</param>
    /// <param name="remainingMovement">The remaining movement points.</param>
    /// <param name="grid">The grid of game objects.</param>
    private void ShowMoveIndicatorRecursive(int startX, int startZ, int remainingMovement, GameObject[,] grid)
    {
        if (remainingMovement <= 0)
        {
            return;
        }

        // Check if the current position is within the grid bounds
        if (startX < 0 || startX >= grid.GetLength(0) || startZ < 0 || startZ >= grid.GetLength(1))
        {
            return;
        }

        // Check if there is a unit on the current position
        bool hasUnit = false;
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (GameObject npc in npcs)
        {
            if (npc.GetComponent<Entity>().x == startX && npc.GetComponent<Entity>().z == startZ)
            {
                hasUnit = true;
                break;
            }
        }

        // Show move indicator for the current position if there is no unit
        if (!hasUnit)
        {
            grid[startX, startZ].transform.GetChild(0).gameObject.SetActive(true);
        }

        // Recursive travel in all directions
        ShowMoveIndicatorRecursive(startX, startZ + 1, remainingMovement - 1, grid); // Up
        ShowMoveIndicatorRecursive(startX, startZ - 1, remainingMovement - 1, grid); // Down
        ShowMoveIndicatorRecursive(startX - 1, startZ, remainingMovement - 1, grid); // Left
        ShowMoveIndicatorRecursive(startX + 1, startZ, remainingMovement - 1, grid); // Right
    }

    public void Attack(GameObject target)
    {
        // Get the target entity
        Entity targetEntity = target.GetComponent<Entity>();
        if (targetEntity == null)
        {
            Debug.Log("Target entity not found");
            return;
        }
        // Apply damage to the target entity
        targetEntity.HPcurr -= attackDamage;
        // spawn the prefab particle system Assets/Prefab/Explosion at the target location, then destroy it after 2 seconds
        GameObject explosion = Instantiate(Resources.Load<GameObject>("Prefabs/Explosion"), target.transform.position, Quaternion.identity);
        Destroy(explosion, 2.0f);
        
        if (targetEntity.HPcurr <= 0)
        {
            // Destroy the target entity if it is dead
            Destroy(target);
        }
    }

}
