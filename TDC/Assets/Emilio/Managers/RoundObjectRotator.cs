using System.Collections.Generic;
using UnityEngine;
using System;

public class RoundObjectRotator : MonoBehaviour
{
    [System.Serializable]
    public class ObjectGroup
    {
        public string groupName;
        public List<GameObject> objects;
        [HideInInspector] public int currentIndex = 0;
    }

    [Header("Groups to rotate per round")]
    [SerializeField] private List<ObjectGroup> groups = new List<ObjectGroup>();

    // ----------------------------------------------------------------------
    // LLAMAR A ESTE METODO AL EMPEZAR CADA RONDA
    // ----------------------------------------------------------------------
    public void OnRoundStarted()
    {
        foreach (var group in groups)
        {
            RotateIfCurrentInactive(group);
        }
    }

    // ----------------------------------------------------------------------
    // LOGICA
    // ----------------------------------------------------------------------
    private void RotateIfCurrentInactive(ObjectGroup group)
    {
        if (group.objects == null || group.objects.Count == 0)
            return;

        // Seguridad indice
        if (group.currentIndex < 0 || group.currentIndex >= group.objects.Count)
            group.currentIndex = 0;

        // Si el actual sigue activo, no hacemos nada
        if (group.objects[group.currentIndex] != null &&
            group.objects[group.currentIndex].activeInHierarchy)
            return;

        // Apagar todos
        for (int i = 0; i < group.objects.Count; i++)
        {
            if (group.objects[i] != null)
                group.objects[i].SetActive(false);
        }

        // Buscar siguiente valido
        int startIndex = group.currentIndex;
        do
        {
            group.currentIndex = (group.currentIndex + 1) % group.objects.Count;
        }
        while (group.objects[group.currentIndex] == null &&
               group.currentIndex != startIndex);

        // Activar nuevo
        if (group.objects[group.currentIndex] != null)
            group.objects[group.currentIndex].SetActive(true);

        Debug.Log($"[RoundObjectRotator] {group.groupName} -> Activado index {group.currentIndex}");
    }
}