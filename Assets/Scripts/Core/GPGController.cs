using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

namespace IrishFarmSim
{
	public class GPGController : MonoBehaviour 
	{
		public static void LoginIntoGPG()
		{
			Debug.Log("Checking Login");
			Social.localUser.Authenticate((bool result) => {
				Debug.Log("Logged in?: " + result);
			});
		}

		public static void PostScore(double amount){
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Submitting Score");
				Social.ReportScore((long)amount, "CgkIuaLFq6kIEAIQBg", (bool result) => {
					Debug.Log("Score submitted?: " + result);
				});
			}
		}

		public static void CheckAchiev_1()
		{
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_1");
				Social.ReportProgress(GoogleCon.achievement_buy_your_first_cow, 100.0f, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}

		public static void CheckAchiev_2()
		{
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_2");
				Social.ReportProgress(GoogleCon.achievement_sell_your_first_cow, 100.0f, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}

		public static void CheckAchiev_3(int cowsBought)
		{
			if (cowsBought < 10) return;
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_3 - Cows Bought: " + cowsBought);
				Social.ReportProgress(GoogleCon.achievement_buy_10_cows, 100.0f, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}

		public static void CheckAchiev_4(double cash)
		{
			if (cash < 100000) return;
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_4 - Cash: " + cash);
				Social.ReportProgress(GoogleCon.achievement_gain_100000_in_cash, 100.0f, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}

		public static void CheckAchiev_5(double cash)
		{
			if (cash < 50000) return;
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_5 - Cash: " + cash);
				Social.ReportProgress(GoogleCon.achievement_gain_50000_in_cash, 100.0f, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}

		public static void CheckAchiev_6(int herdSize)
		{
			if (herdSize < 10) return;
			if (Social.localUser != null && Social.localUser.authenticated)
			{
				Debug.Log("Checking CheckAchiev_6 - Herd Size: " + herdSize);
				int step = (herdSize >= 20) ? 2 : 1;
				PlayGamesPlatform.Instance.IncrementAchievement(
					GoogleCon.achievement_have_20_cows_in_your_herd, step, (bool result) => {
					Debug.Log("Achievement unlocked?: " + result);
				});
			}
		}
	}
}