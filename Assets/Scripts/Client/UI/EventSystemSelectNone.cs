using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemSelectNone : MonoBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private List<GameObject> ExcludedObjects;
    
    void Update()
    {
        if (!ExcludedObjects.Contains(eventSystem.currentSelectedGameObject))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
