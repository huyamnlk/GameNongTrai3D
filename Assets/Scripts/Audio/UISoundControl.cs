using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
	public class UISoundControl : MonoBehaviour 
	{
		public void Start()
		{
			UISlider slider = gameObject.GetComponent<UISlider>();
			if (slider == null) return;

			AudioManager am = AudioManager.EnsureInstance();

			if (gameObject.name.Equals("FX Slider"))
			{
				float fxVal = am != null ? am.sfxVolume : (GameController.Instance() != null ? GameController.Instance().fxLevel : 0.8f);
				slider.value = fxVal;
			}

			if (gameObject.name.Equals("Music Slider"))
			{
				float bgmVal = am != null ? (am.bgmMuted ? 0f : am.bgmVolume) : (GameController.Instance() != null ? GameController.Instance().musicLevel : 0.6f);
				slider.value = bgmVal;
			}
		}

		public void FXLevelChange()
		{
			UISlider slider = gameObject.GetComponent<UISlider>();
			if (slider == null) return;

			float val = slider.value;
			if (GameController.Instance() != null)
			{
				GameController.Instance().fxLevel = val;
			}

			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.SetSFXVolume(val);
				AudioManager.Instance.SetSFXMuted(val <= 0.02f);
			}

			AudioListener.volume = Mathf.Clamp01(val);
		}

		public void MusicLevelChange()
		{
			UISlider slider = gameObject.GetComponent<UISlider>();
			if (slider == null) return;

			float val = slider.value;
			if (GameController.Instance() != null)
			{
				GameController.Instance().musicLevel = val;
			}

			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.SetBGMVolume(val);
				AudioManager.Instance.SetBGMMuted(val <= 0.02f);
			}
		}
	}
}