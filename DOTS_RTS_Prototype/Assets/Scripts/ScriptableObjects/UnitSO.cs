using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSO", menuName = "Units/UnitSO")]
public class UnitSO : ScriptableObject
{
    [SerializeField] string unitName;
    [SerializeField] UnitType unitType;
    public float trainingTime;


    [SerializeField, HideInInspector]
    private UnitKey cachedKey;

    public UnitKey unitKey => cachedKey;

    private void OnValidate()
    {
        cachedKey = new UnitKey
        {
            name = unitName,
            unitType = unitType
        };
    }

    //TODO: REFACTOR INTO REGISTRY OR SIMILAR
    public Entity GetPrefabEntity(EntitiesReferences er)
    {
        return unitKey.name.ToString() switch
        {
            "Hostile" => er.enemyPrefabEntity,
            "Soldier" => er.soldierPrefabEntity,
            "Scout" => er.scoutPrefabEntity,
            _ => er.soldierPrefabEntity,
        };
    }
}

public struct UnitKey : IEquatable<UnitKey>
{
    public FixedString64Bytes name;
    public UnitType unitType;
    public override bool Equals(object obj)
    {
        if (!(obj is UnitKey))
            return false;

        UnitKey other = (UnitKey)obj;
        return this.name == other.name;
    }

    public bool Equals(UnitKey other)
    {
        return name.Equals(other.name);
    }

    public override string ToString()
    {
        return name + "[UnitType:" + unitType + "]";
    }

    public static bool operator ==(UnitKey key1, UnitKey key2)
    {
        return key1.Equals(key2);
    }

    public static bool operator !=(UnitKey key1, UnitKey key2)
    {
        return !key1.Equals(key2);
    }
}

public enum UnitType
{
    None,
    Peaceful,
    Melee,
    Ranged,
}
