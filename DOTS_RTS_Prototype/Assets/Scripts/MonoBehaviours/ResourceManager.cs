using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{

    [SerializeField] ResourceRegistrySO resourceRegistrySO;
    private Dictionary<ResourceKey, int> resourceAmountDictionary;

    /// <summary>
    /// Global access point for the active building placement manager.
    /// </summary>
    public static ResourceManager Instance { get; private set; }

    public event EventHandler OnResourceValueChange;

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void InitializeSingleton()
    {
        // Initialize singleton instance state.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }

    private void Awake()
    {
        InitializeSingleton();
        resourceAmountDictionary = new Dictionary<ResourceKey, int>();

        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            resourceAmountDictionary[resourceSO.resourceKey] = 0;
        }
    }

    public bool AddResourceAmount(ResourceKey resourceKey, int resourceAmount)
    {
        if (resourceAmountDictionary.ContainsKey(resourceKey))
        {
            resourceAmountDictionary[resourceKey] += resourceAmount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        else return false;
    }

    public bool AddResourceAmount(string resource, int resourceAmount)
    {
        var resourceKey = new ResourceKey
        {
            name = resource
        };
        return AddResourceAmount(resourceKey, resourceAmount);
    }
    public int GetResourceValue(ResourceKey resourceKey)
    {
        return resourceAmountDictionary[resourceKey];
    }

    public int GetResourceValue(ResourceSO resourceSO)
    {
        return GetResourceValue(resourceSO.resourceKey);
    }


}
