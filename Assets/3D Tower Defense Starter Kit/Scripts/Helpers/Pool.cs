/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//interacts with PoolManager - this class handles all spawning/despawning of active/inactive
//instances ingame by forwarding calls to the corresponding pool option class
public class Pool : MonoBehaviour
{
    //container transform for parenting new instances
    [HideInInspector]
    public Transform container;

    //list of pool options - definable via the inspector
    public List<PoolOptions> _PoolOptions = new List<PoolOptions>();
    //a list of all active pool instances for quick lookup - see this[index]{} and Count{}
    //could also be removed as it's not necessary for spawning or despawning
    private List<GameObject> _AllActive = new List<GameObject>();

    //initialize this pool
    void Awake()
    {
        //add this pool to the PoolManager dictionary
        PoolManager.Add(this);
        //set container object to this transform
        container = transform;
        //load specified amount of objects before playtime
        PreLoad();
    }


    //called before playtime, first initialization of new instances
    public void PreLoad()
    {
        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //instantiate defined preload amount but don't exceed the maximum amount of objects
            for (int i = poolOptions.totalCount; i < poolOptions.preLoad; i++)
            {
                //call PreLoad() within class PoolOptions
                Transform trans = poolOptions.PreLoad(poolOptions.prefab, Vector3.zero, Quaternion.identity).transform;
                //parent the new instance to this transform
                trans.SetParent(container);
            }
        }
    }


    //find corresponding pool option of submitted prefab and forward call to Activate() of class PoolOptions
    //in case there is no pool option for this prefab, this method will create a new one
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //we found the corresponding pool option prefab
            if (poolOptions.prefab == prefab)
            {
                //activate (or instantiate) a new instance of this prefab
                GameObject obj = poolOptions.Activate(position, rotation);

                //Activate() returned null - we can't instantiate a new instance
                //the possible amount of instances are active and limited
                if (obj == null) return null;

                //add returned instance to the list of all instances
                this._AllActive.Add(obj);

                //get instance transform
                Transform trans = obj.transform;
                //in case it wasn't parented to this transform, parent it now
                if (trans.parent != container)
                    trans.parent = container;

                //submit instance
                return obj;
            }
        }

        //no PoolOption contains the passed in prefab, debug a warning
        Debug.Log("Prefab not found: " + prefab.name + " added to Pool " + this.name);
        //create new PoolOption for the prefab gameobject
        PoolOptions newPoolOptions = new PoolOptions();
        //assign it as base prefab
        newPoolOptions.prefab = prefab;
        //add the new PoolOption to the list of PoolOptions of this pool
        _PoolOptions.Add(newPoolOptions);

        //instantiate a new instance of that prefab
        GameObject newObj = newPoolOptions.Activate(position, rotation);
        //parent it to this transform
        newObj.transform.parent = container;
        //add returned instance to the list of all instances
        this._AllActive.Add(newObj);

        //submit instance
        return newObj;
    }


    //deactivate instance passed in
    public void Despawn(GameObject instance)
    {
        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //seach in active instances for this instance
            if (poolOptions.active.Contains(instance))
            {
                //in case it was unparented during runtime, reparent it now
                if (instance.transform.parent != container)
                    instance.transform.parent = container;

                //let Deactivate() of PoolOption disable this instance
                poolOptions.Deactivate(instance);

                //remove instance from our active list of this component
                this._AllActive.Remove(instance);
                //skip further code
                return;
            }
        }

        //no PoolOption contains the passed in instance, debug a warning
        Debug.LogWarning("Can't despawn - Prefab not found: " + instance.name + " in Pool " + this.name);
    }


    //timed deactivation of an instance 
    public void Despawn(GameObject instance, float time)
    {
        //ahead: we need to start a coroutine which waits for 'time' seconds
        //and then deactivates the instance, but StartCoroutine() nor Invoke()
        //takes 2 parameters, so we use an own class to store these values

        //create new class PoolTimeObject
        PoolTimeObject timeObject = new PoolTimeObject();
        //assign time and instance variable of this class
        timeObject.instance = instance;
        timeObject.time = time;

        //start timed deactivation and pass in the created PoolTimeObject class
        StartCoroutine("DespawnInTime", timeObject);
    }


    //similiar to Despawn(), but also yields for 'PoolTimeObject.time' seconds
    //started by Despawn(instance, time) above
    IEnumerator DespawnInTime(PoolTimeObject timeObject)
    {
        //cache instance to deactivate
        GameObject instance = timeObject.instance;
        //wait for defined seconds
        yield return new WaitForSeconds(timeObject.time);

        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //seach in active instances for this instance
            if (poolOptions.active.Contains(instance))
            {
                //in case it was unparented during runtime, reparent it now
                if (instance.transform.parent != container)
                    instance.transform.parent = container;

                //let Deactivate() of PoolOption disable this instance
                poolOptions.Deactivate(instance);

                //remove instance from our active list of this component
                this._AllActive.Remove(instance);
            }
        }
    }


    //destroy (uses the garbage collector) all inactive instances of this pool
    //the parameter 'limitToPreLoad' decides if only instances
    //above the preload value of that pool should be destroyed
    public void DestroyUnused(bool limitToPreLoad)
    {
        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //let ClearUpUnused() of class PoolOptions handle further actions
            poolOptions.ClearUpUnused(limitToPreLoad);
        }
    }


    //destroy (uses the garbace collector) a specific amount of inactive instances
    //of a pool option
    public void DestroyPrefabCount(GameObject prefab, int count)
    {
        //loop through all pool options of this component
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions poolOptions = this._PoolOptions[cnt];
            //we found the corresponding pool option prefab
            if (poolOptions.prefab == prefab)
            {
                //let DestroyCount() of class PoolOptions handle futher actions
                poolOptions.DestroyCount(count);
                return;
            }
        }

        //no PoolOption contains the passed in prefab, debug a warning
        Debug.LogError("Prefab to destroy count of Pool " + this.name + " not found: " + prefab.name);
    }


    //when this pool gets destroyed
    public void OnDestroy()
    {
        //clean up lists containing references
        //obviously not necessary, but only to make sure
        _AllActive.Clear();
        
        //clean up lists per pool option
        for (int cnt = 0; cnt < this._PoolOptions.Count; cnt++)
        {
            PoolOptions pool = this._PoolOptions[cnt];
            pool.ClearUp();
        }
    }


    //this method returns the gameobject of list '_AllActive' placed at 'index'
    //helper function, not used anywhere
    public GameObject this[int index]
    {
        get { return this._AllActive[index]; }
    }


    //this method returns the count of all active instances of this pool component
    //helper function, not used anywhere
    public int Count
    {
        get { return this._AllActive.Count; }
    }
}


//class to store parameters used on timed deactivation - Destroy(instance,time)
//parameter object for the started coroutine 
[System.Serializable]
public class PoolTimeObject
{
    //instance to deactivate
    public GameObject instance;
    //wait time until deactivation
    public float time;
}


//class which stores all options per pool, a pool can contain multiple pool options
//- observes active and inactive instances and handles activation and deactivation
//as well as the limitation of new instances and later destruction
[System.Serializable]
public class PoolOptions
{
    //list of active and inactive instances of this pool option
    internal List<GameObject> active = new List<GameObject>();
    internal List<GameObject> inactive = new List<GameObject>();
    //instance prefab
    public GameObject prefab;
    //amount to instantiate at game start
    public int preLoad = 0;
    //should the instantiation of new instances be limited
    public bool limit;
    //count of maximum instantiated instances (active+inactive)
    public int maxCount;
    //index for an unique number per instance on Rename() 
    private int index = 0;


    //here goes activation of inactive or instantiation of new gameobjects
    internal GameObject Activate(Vector3 pos, Quaternion rot)
    {
        //initialize instance
        GameObject obj;
        //initialize transform of that instance
        Transform trans;

        //there are inactive objects available for activation
        if (inactive.Count != 0)
        {
            //get first inactive object in the list
            obj = inactive[0];
            //we want to activate it, remove it from the inactive list
            inactive.RemoveAt(0);

            //get instance transform
            trans = obj.transform;
        }
        else
        {
            //we don't have any inactive objects available,
            //we have to instantiate a new one
            //check if the limited count allows new instantiations
            //if not, return nothing
            if (limit && active.Count >= maxCount)
                return null;

            //instantiation possible, instantiate new instance of the prefab
            obj = (GameObject)Object.Instantiate(prefab, pos, rot);
            //get instance transform
            trans = obj.transform;
            //rename it to an unique heading for easier editor overview
            Rename(trans);
        }

        //set position and rotation passed in
        trans.position = pos;
        trans.rotation = rot;

        //add object to the list of active instances
        active.Add(obj);

        //activate object including child objects
        obj.SetActive(true);

        //call the method OnSpawn() on every component and children of this object
        obj.BroadcastMessage("OnSpawn", SendMessageOptions.DontRequireReceiver);

        //return activated/instantiated instance
        return obj;
    }


    //handle deactivation of active instances
    internal void Deactivate(GameObject obj)
    {
        //we want to deactivate it, remove it from the active list
        active.Remove(obj);
        //add object to the list of inactive instances instead
        inactive.Add(obj);

        //call the method OnDespawn() on every component and children of this object
        obj.BroadcastMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);

        //deactivate object including child objects
        obj.SetActive(false);
    }


    //preload defined amount of instances at game start
    internal GameObject PreLoad(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab in pool empty! Please check references.");
            return null;
        }

        //instantiate new instance of the prefab
        GameObject obj = (GameObject)Object.Instantiate(prefab, position, rotation);
        //rename it to an unique heading for easier editor overview
        Rename(obj.transform);

        //deactivate object including child objects
        obj.SetActive(false);
        //add object to the list of inactive instances
        inactive.Add(obj);

        //return inactive instance
        return obj;
    }


    //create an unique name for each instance at instantiation
    //to differ them from each other in the editor
    private void Rename(Transform instance)
    {
        //count total instances and assign the next free number
        //convert it in the range of hundreds,
        //there shouldn't be thousands of instances at any time
        //e.g. TestEnemy(Clone)001
        instance.name += (index + 1).ToString("#000");
        index++;
    }


    //count all instances of this pool option
    internal int totalCount
    {
        get
        {
            //initialize count value
            int count = 0;
            //add active and inactive count
            count += this.active.Count;
            count += this.inactive.Count;
            //return final count
            return count;
        }
    }


    //method to empty instance lists, used on OnDestroy()
    //which gets called by class Pool when this pool gameobject is destroyed
    internal void ClearUp()
    {
        active.Clear();
        inactive.Clear();
    }


    //destroy (uses the garbage collector) all inactive instances of this pool
    //called by class Pool on DestroyUnused()
    //the parameter 'limitToPreLoad' decides if only instances
    //above the preload value of this pool option should be destroyed
    internal void ClearUpUnused(bool limitToPreLoad)
    {
        //only destroy instances above the limit amount
        if (limitToPreLoad)
        {
            //start from the last inactive instance and count down
            //until the index reached the limit amount
            for (int i = inactive.Count - 1; i >= preLoad; i--)
            {
                //destroy the object at 'i'
                Object.Destroy(inactive[i]);
            }
            //remove the range of destroyed objects (now null references) from the list
            if(inactive.Count > preLoad)
                inactive.RemoveRange(preLoad, inactive.Count - preLoad);
        }
        else
        {
            //limitToPreLoad is false, destroy all inactive instances
            foreach (GameObject obj in inactive)
            {
                Object.Destroy(obj);
            }
            //reset the list
            inactive.Clear();
        }
    }


    //destroy (uses the garbace collector) a specific amount of inactive instances
    //of this pool option - called by class Pool on DestroyPrefabCount()
    internal void DestroyCount(int count)
    {
        //the amount which was passed in exceeds the amount of inactive instances
        if (count > inactive.Count)
        {
            //debug a warning that this call is equal to ClearUpUnused(false) and return
            Debug.LogWarning("Destroy Count value: " + count + " is greater than inactive Count: " +
                             inactive.Count + ". Destroying all available inactive objects of type: " +
                             prefab.name + ". Use ClearUpUnused(false) instead to achieve the same.");
            ClearUpUnused(false);
            return;
        }

        //starting from the end, count down the index and destroy each inactive instance
        //until we destroyed the amount passed in
        for (int i = inactive.Count - 1; i >= inactive.Count - count; i--)
        {
            Object.Destroy(inactive[i]);
        }
        //remove the range of destroyed objects (now null references) from the list
        inactive.RemoveRange(inactive.Count - count, count);
    }
}
