using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PrefabRegistrySO", menuName = "Editor/PrefabRegistrySO")]
public class PrefabRegistrySO : ScriptableObject
{
    public List<GameObject> prefabGOs;

    [SerializeField, HideInInspector]
    private HashSet<EntityPrefabKey> cachedKeySet;
    public HashSet<EntityPrefabKey> entityReferenceKeySet => cachedKeySet;

    private void OnEnable()
    {
        Construct();
    }

    private void Construct()
    {
        if (cachedKeySet == null)
        {
            cachedKeySet = new HashSet<EntityPrefabKey>();
        }
        else if (prefabGOs.Count > 0)
        {
            foreach (var prefab in prefabGOs)
            {
                cachedKeySet.Add(new EntityPrefabKey
                {
                    name = prefab.name
                });
            }
        }
        prefabGOs = prefabGOs.OrderBy((GameObject go) => go.name).ToHashSet().ToList();
    }

    private bool IsVerified()
    {
        return
            cachedKeySet != null &&
            cachedKeySet.Count == prefabGOs.Count;
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
