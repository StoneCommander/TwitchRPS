using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aoiti.Pathfinding;
using System.Linq;

public class Entity : MonoBehaviour
{
    // type:
    public string type;

    // attack types list
    public List<string> attackTypes;
    // run away types list
    public List<string> runAwayTypes;

    private List<Vector2> attackTargets;
    private List<Vector2> runTargets;

    [Header("Navigator options")]
    [SerializeField] float gridSize = 0.5f; //increase patience or gridSize for larger maps
    [SerializeField] float speed = 0.05f; //increase for faster movement
    
    Pathfinder<Vector2> pathfinder; //the pathfinder object that stores the methods and patience
    [Tooltip("The layers that the navigator can not pass through.")]
    [SerializeField] LayerMask obstacles;
    [Tooltip("Deactivate to make the navigator move along the grid only, except at the end when it reaches to the target point. This shortens the path but costs extra Physics2D.LineCast")] 
    [SerializeField] bool searchShortcut =false; 
    [Tooltip("Deactivate to make the navigator to stop at the nearest point on the grid.")]
    [SerializeField] bool snapToGrid =false; 
    Vector2 targetNode; //target in 2D space
    List <Vector2> path;
    List<Vector2> pathLeftToGo= new List<Vector2>();
    [SerializeField] bool drawDebugLines;

    // Start is called before the first frame update
    void Start()
    {
      pathfinder = new Pathfinder<Vector2>(GetDistance,GetNeighbourNodes,1000); //increase patience or gridSize for larger maps
    }

    // Update is called once per frame
    void Update()
    {
      // ***** TARGET *****

      // get all entity objects
      GameObject[] entities = GameObject.FindGameObjectsWithTag("entity");
      attackTargets = new List<Vector2>();
      runTargets = new List<Vector2>();
      foreach (GameObject entity in entities)
      {
        Entity entityScript = entity.GetComponent<Entity>();
        // check if enitty type is on the attack types list
        if (attackTypes.Contains(entityScript.type))
        {
          print(entity.transform);
          attackTargets.Add(entity.transform.position);
        }
        // check if enitty type is on the run away types list
        else if (runAwayTypes.Contains(entityScript.type))
        {
          print(entity.transform);
          runTargets.Add(entity.transform.position);
        }
      }
      // get the closest attack target
      Vector2 closestAttackTarget = GetClosestTarget(attackTargets);
      // get the closest run target
      Vector2 closestRunTarget = GetClosestTarget(runTargets);


      // get wich one is closer
      if (Vector2.Distance(transform.position, closestAttackTarget) < Vector2.Distance(transform.position, closestRunTarget))
      {
        GetMoveCommand(closestAttackTarget);
      }
      else 
      {
        GetFleeCommand(closestRunTarget,5f);
      }

      // ***** MOVEMENT ***** 

      if (pathLeftToGo.Count > 0) //if the target is not yet reached
      {
          Vector3 dir =  (Vector3)pathLeftToGo[0]-transform.position ;
          transform.position += dir.normalized * speed;
          if (((Vector2)transform.position - pathLeftToGo[0]).sqrMagnitude <speed*speed) 
          {
              transform.position = pathLeftToGo[0];
              pathLeftToGo.RemoveAt(0);
          }
      }

      if (drawDebugLines)
      {
          for (int i=0;i<pathLeftToGo.Count-1;i++) //visualize your path in the sceneview
          {
              Debug.DrawLine(pathLeftToGo[i], pathLeftToGo[i+1]);
          }
      }
      
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        Entity entityScript = collision.gameObject.GetComponent<Entity>();
        // check if enitty type is on the run types list
        if (runAwayTypes.Contains(entityScript.type))
        {
          type = entityScript.type;
          attackTypes = entityScript.attackTypes;
          runAwayTypes = entityScript.runAwayTypes;
          GetComponent<SpriteRenderer>().color = collision.gameObject.GetComponent<SpriteRenderer>().color;
        }
    }
    Vector2 GetClosestTarget(List<Vector2> targets)
    {
      Vector2 closestTarget = Vector2.zero;
      float closestDistance = Mathf.Infinity;
      foreach (Vector2 target in targets)
      {
        float distance = Vector2.Distance(transform.position, target);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closestTarget = target;
        }
      }
      return closestTarget;
    }

    
    void GetMoveCommand(Vector2 target)
    {
        Vector2 closestNode = GetClosestNode(transform.position);
        if (pathfinder.GenerateAstarPath(closestNode, GetClosestNode(target), out path)) //Generate path between two points on grid that are close to the transform position and the assigned target.
        {
            if (searchShortcut && path.Count>0)
                pathLeftToGo = ShortenPath(path);
            else
            {
                pathLeftToGo = new List<Vector2>(path);
                if (!snapToGrid) pathLeftToGo.Add(target);
            }

        }
        
    }

    void GetFleeCommand(Vector2 target, float safeDistance)
    {
      Vector2 closestNode = GetClosestNode(transform.position);
      Vector2 fleeTarget = GetFleeTarget(target, safeDistance);
      if (pathfinder.GenerateAstarPath(closestNode, GetClosestNode(fleeTarget), out path)) //Generate path between two points on grid that are close to the transform position and the assigned target.
      {
        if (searchShortcut && path.Count > 0)
          pathLeftToGo = ShortenPath(path);
        else
        {
          pathLeftToGo = new List<Vector2>(path);
          if (!snapToGrid) pathLeftToGo.Add(fleeTarget);
        }

      }

    }

    Vector2 GetFleeTarget(Vector2 target, float safeDistance)
    {
      Vector2 direction = (Vector2)transform.position - target;
      direction.Normalize();
      Vector2 fleeTarget = (Vector2)transform.position + direction * safeDistance;
      return fleeTarget;
    }


    /// <summary>
    /// Finds closest point on the grid
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    Vector2 GetClosestNode(Vector2 target) 
    {
        return new Vector2(Mathf.Round(target.x/gridSize)*gridSize, Mathf.Round(target.y / gridSize) * gridSize);
    }

    /// <summary>
    /// A distance approximation. 
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    float GetDistance(Vector2 A, Vector2 B) 
    {
        return (A - B).sqrMagnitude; //Uses square magnitude to lessen the CPU time.
    }

    /// <summary>
    /// Finds possible conenctions and the distances to those connections on the grid.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Dictionary<Vector2,float> GetNeighbourNodes(Vector2 pos) 
    {
        Dictionary<Vector2, float> neighbours = new Dictionary<Vector2, float>();
        for (int i=-1;i<2;i++)
        {
            for (int j=-1;j<2;j++)
            {
                if (i == 0 && j == 0) continue;

                Vector2 dir = new Vector2(i, j)*gridSize;
                if (!Physics2D.Linecast(pos,pos+dir, obstacles))
                {
                    neighbours.Add(GetClosestNode( pos + dir), dir.magnitude);
                }
            }

        }
        return neighbours;
    }

    
    List<Vector2> ShortenPath(List<Vector2> path)
    {
        List<Vector2> newPath = new List<Vector2>();
        
        for (int i=0;i<path.Count;i++)
        {
            newPath.Add(path[i]);
            for (int j=path.Count-1;j>i;j-- )
            {
                if (!Physics2D.Linecast(path[i],path[j], obstacles))
                {
                    
                    i = j;
                    break;
                }
            }
            newPath.Add(path[i]);
        }
        newPath.Add(path[path.Count - 1]);
        return newPath;
    }

}
