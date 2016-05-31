﻿using UnityEngine;
using System.Collections;

public class LabelCorrector : MonoBehaviour
{
    public GameObject target;

	// Use this for initialization
	void Start ()
	{
	}
	
	// Update is called once per frame
	void Update ()
	{
	    transform.LookAt(target.transform, target.transform.up);
	    transform.Rotate(new Vector3(0f, 1f, 0f), 180);
	}
}
