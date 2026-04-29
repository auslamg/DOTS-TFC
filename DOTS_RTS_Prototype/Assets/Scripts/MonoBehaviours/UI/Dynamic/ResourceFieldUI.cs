using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceFieldUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI textMesh;

    public void Initialize(ResourceSO resourceSO)
    {
        icon.sprite = resourceSO.icon;
        textMesh.text = "0";
    }

    public void UpdateAmount(int amount)
    {
        textMesh.text = amount.ToString();
    }
}
