using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDataSO", menuName = "Units/UnitDataSO")]
public class UnitDataSO : ScriptableObject
{
    [SerializeField] string unitName;
    public UnitType unitType;
    public float trainingTime;


    [SerializeField, HideInInspector]
    private UnitKey cachedKey;
    public UnitKey unitKey => cachedKey;

    private void OnValidate()
    {
        cachedKey = new UnitKey
        {
            name = unitName
        };
    }

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

public struct UnitKey : IEquatable<UnitKey>, IComparable<UnitKey>
{
    public FixedString64Bytes name;
    public bool Equals(UnitKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is UnitKey other && Equals(other);
    }
    public int CompareTo(UnitKey other)
    {
        int cmp = name.CompareTo(other.name);
        return cmp;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + name.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(UnitKey left, UnitKey right) => left.Equals(right);
    public static bool operator !=(UnitKey left, UnitKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }
}

public enum UnitType
{
    None,
    Peaceful,
    Melee,
    Ranged,
}
