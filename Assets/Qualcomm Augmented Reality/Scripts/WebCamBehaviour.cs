/*==============================================================================
Copyright (c) 2012-2014 Qualcomm Connected Experiences, Inc.
All Rights Reserved.
Confidential and Proprietary - Qualcomm Connected Experiences, Inc.
==============================================================================*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// This MonoBehaviour manages the usage of a webcam for Play Mode in Windows or Mac.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WebCamBehaviour : WebCamAbstractBehaviour
{
	void start()
	{
		bool focusModeSet = CameraDevice.Instance.SetFocusMode (CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);  
		if (!focusModeSet) {  
			Debug.Log ("Failed to set focus mode (unsupported mode).");  
		}
	}

}
