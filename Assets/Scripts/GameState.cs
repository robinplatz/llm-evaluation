using System.Collections;
using System.Collections.Generic;
    using System.Linq;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public enum GameStateScene
    {
        MainMenu,
        Game
    }
    public GameObject gameController;
    public GameStateScene gameStateScene;
    // team of the unit that is currently taking actions
    public int gameStateTeam;
    // increases every time a unit has no actions left
    public int gameStateTurn;
    public int actionsTaken;
    public bool gridReady = false;
    // if a unit is currently moving or attacking
    public bool actionMode = false;

    // queue of units that are waiting to take actions
    public Queue<Entity> entityQueue;

    // Start is called before the first frame update
    public void Start()
    {
        // sane inits
        gameStateScene = GameStateScene.Game;
        entityQueue = new Queue<Entity>();
        StartCoroutine(WaitForGrid());       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // wait for the grid to be ready, then fill the queue with all Units
    // called once in the beginning of the game
    IEnumerator WaitForGrid()
    {
        while (!gridReady) {
            yield return new WaitForSeconds(0.5f);
        }
        // fill the queue with all Units
        List<Entity> entityList = new List<Entity>();
        foreach (Entity entity in GameObject.FindObjectsOfType<Entity>()) {
            if (entity.entityType == Entity.EntityType.Unit) {
                entityList.Add(entity);
            }
        }
        // sort the list by initiative
        entityList = entityList.OrderBy(entity => entity.initiative).ToList();
        // convert the list to a queue
        entityQueue = new Queue<Entity>(entityList);
        // set the first unit in the queue to be selected
        entityQueue.Peek().selected = true;
        entityQueue.Peek().refresh();
        // set the team of the first unit in the queue to be the current team
        gameStateTeam = entityQueue.Peek().team;
        // gameController knows which unit is selected
        gameController.GetComponent<GameController>().entitySelected = entityQueue.Peek().gameObject;
        gameController.GetComponent<GameController>().selected = true;
    }

    // called when a unit finished taking an action, can pass turn to the next unit after 2 actions are taken
    public void ActionTaken()
    {
        actionsTaken++;
        if (actionsTaken >= 2) {
            actionsTaken = 0;
            NextTurn();
        }
    }

    // pass turn to next unit in the queue
    public void NextTurn()
    {
        // set the current unit to be unselected (maybe redundant)
        entityQueue.Peek().selected = false;
        // add the current unit to the end of the queue
        entityQueue.Enqueue(entityQueue.Dequeue());
        // set the next unit in the queue to be selected
        entityQueue.Peek().selected = true;
        entityQueue.Peek().refresh();
        // set the team of the next unit in the queue to be the current team
        gameStateTeam = entityQueue.Peek().team;
        // increase the turn counter
        gameStateTurn++;
        // gameController knows which unit is selected
        gameController.GetComponent<GameController>().entitySelected = entityQueue.Peek().gameObject;
        gameController.GetComponent<GameController>().selected = true;
    }

}
