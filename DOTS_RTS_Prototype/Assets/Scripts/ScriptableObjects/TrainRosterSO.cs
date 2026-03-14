using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainRosterSO", menuName = "Buildings/Trainer/TrainRoster")]
public class TrainRosterSO : ScriptableObject
{
    public List<string> unitKeys;

    [SerializeField, HideInInspector]
    private HashSet<UnitKey> cachedKeySet;
    public HashSet<UnitKey> unitKeySet => cachedKeySet;

    private void OnEnable()
    {
        Construct();
    }

    private void Construct()
    {
        if (cachedKeySet == null)
        {
            cachedKeySet = new HashSet<UnitKey>();
        }
        else if (unitKeys.Count > 0)
        {
            foreach (var unitName in unitKeys)
            {
                cachedKeySet.Add(new UnitKey
                {
                    name = unitName
                });
            }
        }
    }

    private bool IsVerified()
    {
        return
            cachedKeySet != null &&
            cachedKeySet.Count == unitKeys.Count;
    }

    public bool VerifyConstruction()
    {
        if (IsVerified())
        {
            return true;
        }
        else
        {
            Construct();
            return IsVerified();
        }
    }
    
}
