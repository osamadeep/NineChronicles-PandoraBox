using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupPooler : MonoBehaviour
{
    public static PickupPooler instance;
    public Transform PooledObjectsHolder;
    public GameObject pooledObject;
    public int pooledAmount = 10;
    public bool willGrow = true;
    public List<GameObject> pooledObjects;
    void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        pooledObjects = new List<GameObject>();
        for (int i = 0; i < pooledAmount; i++)
        {
            GameObject obj = Instantiate(pooledObject, PooledObjectsHolder);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }
    }


    public GameObject GetpooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }
        if (willGrow)
        {
            GameObject obj = Instantiate(pooledObject, PooledObjectsHolder);
            pooledObjects.Add(obj);
            return obj;
        }
        return null;
    }
}
