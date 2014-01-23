﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class OIT : MonoBehaviour {
	public Shader accumShader;
	public Shader revealageShader;
	public Shader postEffectShader;
	public bool	oitEnabled;

	private Camera _renderCam;
	private GameObject _renderGO;

	private int _layerOpaque;
	private int _layerTransparent;

	private RenderTexture _opaqueTex;
	private RenderTexture _accumTex;
	private RenderTexture _revealageTex;

	private Material _postEffectMat;

	void OnEnable() {
		_renderGO = new GameObject();
		_renderGO.transform.parent = transform;
		_renderCam = _renderGO.AddComponent<Camera>();
		_renderCam.CopyFrom(camera);
		_renderCam.enabled = false;
		_renderCam.clearFlags = CameraClearFlags.Nothing;
		_layerOpaque = LayerMask.NameToLayer("Default");
		_layerTransparent = LayerMask.NameToLayer("TransparentFX");
		_postEffectMat = new Material(postEffectShader);
	}

	void OnPreRender() {
		var width = Screen.width;
		var height = Screen.height;
		_opaqueTex = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
		_accumTex = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);
		_revealageTex = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf);

		_renderCam.targetTexture = _opaqueTex;
		_renderCam.backgroundColor = Color.black;
		_renderCam.clearFlags = CameraClearFlags.SolidColor;
		_renderCam.cullingMask = 1 << _layerOpaque;
		_renderCam.Render();

		_renderCam.targetTexture = _accumTex;
		_renderCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
		_renderCam.clearFlags = CameraClearFlags.SolidColor;
		_renderCam.cullingMask = 0;
		_renderCam.Render();
		_renderCam.SetTargetBuffers(_accumTex.colorBuffer, _opaqueTex.depthBuffer);
		_renderCam.clearFlags = CameraClearFlags.Nothing;
		_renderCam.cullingMask = 1 << _layerTransparent;
		_renderCam.RenderWithShader(accumShader, null);

		_renderCam.targetTexture = _revealageTex;
		_renderCam.backgroundColor = new Color(1f, 1f, 1f, 1f);
		_renderCam.clearFlags = CameraClearFlags.SolidColor;
		_renderCam.cullingMask = 0;
		_renderCam.Render();
		_renderCam.SetTargetBuffers(_revealageTex.colorBuffer, _opaqueTex.depthBuffer);
		_renderCam.clearFlags = CameraClearFlags.Nothing;
		_renderCam.cullingMask = 1 << _layerTransparent;
		_renderCam.RenderWithShader(revealageShader, null);
	}
	
	void OnRenderImage(RenderTexture src, RenderTexture dst) {
		if (!oitEnabled) {
			Graphics.Blit(src, dst);
			return;
		}
		_postEffectMat.SetTexture("_AccumTex", _accumTex);
		_postEffectMat.SetTexture("_RevealageTex", _revealageTex);
		Graphics.Blit(_opaqueTex, dst, _postEffectMat);
	}

	void OnPostRender() {
		RenderTexture.ReleaseTemporary(_opaqueTex);
		RenderTexture.ReleaseTemporary(_accumTex);
		RenderTexture.ReleaseTemporary(_revealageTex);
	}

	void OnGUI() {
		oitEnabled = GUILayout.Toggle(oitEnabled, "Enable OIT");
	}

	void OnDisable() {
		Destroy(_renderGO);
		Destroy(_postEffectMat);
	}
}