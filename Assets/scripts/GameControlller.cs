using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlller : MonoBehaviour
{
    // list of entity prefabs
    public List<GameObject> entityPrefabs;

    public int numEntities = 20;

    // Start is called before the first frame update
    void Start()
    {
        // ***** SPAWN *****
        // spawn entities
        foreach (GameObject entityPrefab in entityPrefabs)
        {
            for (int i = 0; i < numEntities; i++)
            {
                Instantiate(entityPrefab, new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), 0), Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // if all entities are the same type, end the game
        GameObject[] entities = GameObject.FindGameObjectsWithTag("entity");
        string type = entities[0].GetComponent<Entity>().type;
        bool sameType = true;
        foreach (GameObject entity in entities)
        {
            if (entity.GetComponent<Entity>().type != type)
            {
                sameType = false;
                break;
            }
        }
        if (sameType)
        {
            Debug.Log("Game Over");
            
        }

    }
}
