using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{

    [SerializeField] ResourceQuantity[] startingResources;
    [SerializeField] ResourceRegistrySO resourceRegistrySO;
    public Dictionary<ResourceKey, int> resourceAmountDictionary { get; private set; }

    public void OverrideDict(Dictionary<ResourceKey, int> dict)
    {
        resourceAmountDictionary = dict;
        OnResourceValueChange.Invoke(this, EventArgs.Empty);
    }

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

    void Start()
    {
        AddResourceValues(startingResources);
    }

    public bool AddResourceValue(ResourceKey resourceKey, int resourceAmount)
    {
        if (resourceAmountDictionary.ContainsKey(resourceKey))
        {
            resourceAmountDictionary[resourceKey] += resourceAmount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        else return false;
    }

    public bool AddResourceValue(string resource, int resourceAmount)
    {
        var resourceKey = new ResourceKey
        {
            name = resource
        };
        return AddResourceValue(resourceKey, resourceAmount);
    }

    public bool AddResourceValue(ResourceQuantity resourceQuantity)
    {
        if (resourceAmountDictionary.ContainsKey(resourceQuantity.resourceSO.resourceKey))
        {
            resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] += resourceQuantity.amount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        else return false;
    }

    public bool AddResourceValues(ResourceQuantity[] resourceQuantities)
    {
        foreach (var resource in resourceQuantities)
        {
            if (!resourceAmountDictionary.ContainsKey(resource.resourceSO.resourceKey))
            {
                return false;
            }
        }

        foreach (var resourceQuantity in resourceQuantities)
        {
            resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] += resourceQuantity.amount;
        }
        OnResourceValueChange.Invoke(this, EventArgs.Empty);
        return true;
    }

    public int GetResourceAmount(ResourceKey resourceKey)
    {
        return resourceAmountDictionary[resourceKey];
    }

    public int GetResourceValue(ResourceSO resourceSO)
    {
        return GetResourceAmount(resourceSO.resourceKey);
    }

    public bool CanSpendResourceValue(ResourceQuantity resourceQuantity)
    {
        return resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] >= resourceQuantity.amount;
    }

    public bool CanSpendResourceValues(ResourceQuantity[] resourceQuantities)
    {
        foreach (var resourceQuantity in resourceQuantities)
        {
            if (resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] < resourceQuantity.amount)
            {
                return false;
            }
        }
        return true;
    }

    public bool SpendResourceValue(ResourceQuantity resourceQuantity)
    {
        if (CanSpendResourceValue(resourceQuantity))
        {
            resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] -= resourceQuantity.amount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool SpendResourceValues(ResourceQuantity[] resourceQuantities)
    {
        if (CanSpendResourceValues(resourceQuantities))
        {
            foreach (var resourceQuantity in resourceQuantities)
            {
                resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] -= resourceQuantity.amount;
            }
            OnResourceValueChange.Invoke(this, EventArgs.Empty);

            return true;
        }
        else
        {
            return false;
        }
    }
}
