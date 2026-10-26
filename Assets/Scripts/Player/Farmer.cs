using System;

namespace IrishFarmSim
{
	[System.Serializable]
	public class Farmer
	{
		public string name { get; set; }
		private double _cash = 500.0;
		public double cash
		{
			get
			{
				if (PlayerProgression.Instance != null) return PlayerProgression.Instance.Gold;
				return _cash;
			}
			set
			{
				_cash = value;
				if (PlayerProgression.Instance != null && PlayerProgression.Instance.Gold != (int)value)
				{
					PlayerProgression.Instance.SetProgression(PlayerProgression.Instance.Level, PlayerProgression.Instance.CurrentXP, (int)value);
				}
			}
		}
		public int grain { get; set; }
		public int hay { get; set; }
		public int pellet { get; set; }
		public int cowsBought { get; set; }
		public string gameDifficulty { get; set; }
		public float fxLevel { get; set; }

		public Farmer(string name, double cash, int grain, int hay, int pellet, int cowsBought, string gameDifficulty, float fxLevel)
		{
			this.name = name;
			this._cash = cash;
			this.grain = grain;
			this.hay = hay;
			this.pellet = pellet;
			this.cowsBought = cowsBought;
			this.gameDifficulty = gameDifficulty;
			this.fxLevel = fxLevel;
		}
	}
}