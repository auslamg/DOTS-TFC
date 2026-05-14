using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for player resources, handling storage, modification, validation, and spending logic.
/// </summary>
/// <remarks>
/// Provides a singleton access point and raises events when resource values change.
/// Resource values are stored in a dictionary keyed by <see cref="ResourceKey"/>.
/// </remarks>
public class ResourceManager : MonoBehaviour
{
    [SerializeField] ResourceQuantity[] startingResources;
    [SerializeField] ResourceRegistrySO resourceRegistrySO;

    /// <summary>
    /// Internal storage of all resource amounts indexed by resource key.
    /// </summary>
    public Dictionary<ResourceKey, int> resourceAmountDictionary { get; private set; }

    /// <summary>
    /// Replaces the current resource dictionary with an external one and triggers a change event.
    /// </summary>
    /// <param name="dict">New resource dictionary.</param>
    public void OverrideDict(Dictionary<ResourceKey, int> dict)
    {
        resourceAmountDictionary = dict;
        OnResourceValueChange.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Global singleton instance of the ResourceManager.
    /// </summary>
    public static ResourceManager Instance { get; private set; }

    /// <summary>
    /// Invoked whenever any resource value is modified.
    /// </summary>
    public event EventHandler OnResourceValueChange;

    /// <summary>
    /// Ensures singleton instance validity.
    /// </summary>
    void InitializeSingleton()
    {
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

    /// <summary>
    /// Unity lifecycle method. Initializes singleton and resource dictionary with registry-defined keys.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
        resourceAmountDictionary = new Dictionary<ResourceKey, int>();

        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            resourceAmountDictionary[resourceSO.resourceKey] = 0;
        }
    }

    /// <summary>
    /// Initializes starting resources after scene load.
    /// </summary>
    private void Start()
    {
        AddResourceValues(startingResources);
    }

    /// <summary>
    /// Adds a specified amount to a resource.
    /// </summary>
    /// <param name="resourceKey">Target resource key.</param>
    /// <param name="resourceAmount">Amount to add.</param>
    /// <returns>True if the resource exists and was updated.</returns>
    public bool AddResourceValue(ResourceKey resourceKey, int resourceAmount)
    {
        if (resourceAmountDictionary.ContainsKey(resourceKey))
        {
            resourceAmountDictionary[resourceKey] += resourceAmount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a specified amount to a resource identified by string name.
    /// </summary>
    /// <param name="resource">Resource name.</param>
    /// <param name="resourceAmount">Amount to add.</param>
    /// <returns>True if the resource exists and was updated.</returns>
    public bool AddResourceValue(string resource, int resourceAmount)
    {
        var resourceKey = new ResourceKey
        {
            name = resource
        };
        return AddResourceValue(resourceKey, resourceAmount);
    }

    /// <summary>
    /// Adds a structured resource quantity.
    /// </summary>
    /// <param name="resourceQuantity">Resource data container.</param>
    /// <returns>True if successfully added.</returns>
    public bool AddResourceValue(ResourceQuantity resourceQuantity)
    {
        if (resourceAmountDictionary.ContainsKey(resourceQuantity.resourceSO.resourceKey))
        {
            resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] += resourceQuantity.amount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds multiple resource quantities in a single operation.
    /// </summary>
    /// <param name="resourceQuantities">Array of resources to add.</param>
    /// <returns>True if all resources exist and were successfully added.</returns>
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

    /// <summary>
    /// Retrieves the current amount of a resource.
    /// </summary>
    /// <param name="resourceKey">Target resource key.</param>
    /// <returns>Current stored amount.</returns>
    public int GetResourceAmount(ResourceKey resourceKey)
    {
        return resourceAmountDictionary[resourceKey];
    }

    /// <summary>
    /// Retrieves the current amount of a resource using a resource definition.
    /// </summary>
    /// <param name="resourceSO">Resource definition.</param>
    /// <returns>Current stored amount.</returns>
    public int GetResourceValue(ResourceSO resourceSO)
    {
        return GetResourceAmount(resourceSO.resourceKey);
    }

    /// <summary>
    /// Checks whether a single resource cost can be paid.
    /// </summary>
    /// <param name="resourceQuantity">Cost to evaluate.</param>
    /// <returns>True if sufficient resources are available.</returns>
    public bool CanSpendResourceValue(ResourceQuantity resourceQuantity)
    {
        return resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] >= resourceQuantity.amount;
    }

    /// <summary>
    /// Checks whether multiple resource costs can be paid.
    /// </summary>
    /// <param name="resourceQuantities">Costs to evaluate.</param>
    /// <returns>True if all resources are sufficient.</returns>
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

    /// <summary>
    /// Attempts to spend a single resource cost.
    /// </summary>
    /// <param name="resourceQuantity">Cost to deduct.</param>
    /// <returns>True if the transaction succeeded.</returns>
    public bool SpendResourceValue(ResourceQuantity resourceQuantity)
    {
        if (CanSpendResourceValue(resourceQuantity))
        {
            resourceAmountDictionary[resourceQuantity.resourceSO.resourceKey] -= resourceQuantity.amount;
            OnResourceValueChange.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to spend multiple resource costs in a single transaction.
    /// </summary>
    /// <param name="resourceQuantities">Costs to deduct.</param>
    /// <returns>True if all costs were successfully applied.</returns>
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
        return false;
    }
}