using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class ChangeInstructions : MonoBehaviour
{
    [SerializeField] private List<string> instructions = new List<string>();

    [SerializeField] private TextMeshProUGUI texto;

    [SerializeField] private float timeBetweenText;

    [SerializeField] private float timeToShowText;

    void Start()
    {
        Invoke("PrintInstructions", timeToShowText);
    }

    
    void Update()
    {
        
    }

    private void PrintInstructions()
    {
        StartCoroutine(ChangeText());
    }

    public IEnumerator ChangeText()
    {
        foreach (var instruction in instructions)
        {
            texto.text = instruction;
            yield return new WaitForSeconds(timeBetweenText);
        }
    }
}
