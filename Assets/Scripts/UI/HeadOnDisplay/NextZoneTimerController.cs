﻿using Playmode.Pickables;
using Playmode.Util.Values;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextZoneTimerController : MonoBehaviour {

	private float countDownTimer = 0f;

	private Text text;
	private CameraController cameraController;

	private void Awake()
	{
		cameraController = GameObject.FindWithTag(Tags.MainCamera).GetComponent<CameraController>();
		text = GetComponent<Text>();

		ResetTimer();
	}

	private void OnEnable()
	{
		StartCoroutine(TimerDropCountdown());
	}

	private IEnumerator TimerDropCountdown()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f);
			countDownTimer--;
		}
	}

	private void ResetTimer()
	{
		countDownTimer = cameraController.zoneChangeDelay;
	}

	private void Update()
	{
		if (cameraController.zoneIsShrinking)
		{
			text.text = "Next zone in: Shrinking";
		}
		else
		{
			text.text = "Next zone in: " + countDownTimer.ToString();		
		}
		if (countDownTimer <= 0)
		{
			ResetTimer();
		}
		
	}
}