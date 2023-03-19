using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class InstantiateData
{
    private readonly Vector3 GlobalPos;
    private readonly Quaternion GlobalRot;
    private readonly GameObject Prefab;
    private readonly Transform Parent;
    private readonly Action<GameObject> AfterInstantiate;


    public InstantiateData(GameObject prefab, Vector3 globalPos, Quaternion globalRot, Transform parent, Action<GameObject> afterInstantiate)
    {
        GlobalPos = globalPos;
        GlobalRot = globalRot;
        Prefab = prefab;
        Parent = parent;
        AfterInstantiate = afterInstantiate;
    }

    public void Execute()
    {
        var created = Object.Instantiate(Prefab, GlobalPos, GlobalRot);
        created.transform.SetParent(Parent);
        if (AfterInstantiate != null)
        {
            AfterInstantiate.Invoke(created);
        }
    }
}
