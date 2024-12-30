using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Ammunition.Logic;
using Verse;

namespace Ammunition.Settings
{
	public class AmmoSettingsIO
	{
		
		internal static void SaveAmmoDefault()
		{
			try
			{
				if (Settings.CategoryWeaponDictionary == null ||
				    Settings.ExemptionWeaponDictionary == null ||
				    Settings.BagSettingsDictionary == null)
				{
					return;
				}

				var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ammoDefault.lts");
				var file = new SaveFile
				{
					Categories = Settings.CategoryWeaponDictionary,
					Exemptions = Settings.ExemptionWeaponDictionary,
					Bags = Settings.BagSettingsDictionary
				};

				var bf = new BinaryFormatter();
				using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				{
					bf.Serialize(stream, file);
				}

				// Optional: Create a human-readable version
				var pathReadable = Path.Combine(GenFilePaths.ConfigFolderPath, "ammoLTSReadable.txt");
				using (var writer = new StreamWriter(pathReadable))
				{
					foreach (var category in Settings.CategoryWeaponDictionary)
					{
						writer.WriteLine($"Category: <{category.Key}>");
						foreach (var weaponDef in category.Value.Where(kvp => kvp.Value))
						{
							writer.WriteLine($"<{weaponDef.Key}>");
						}
						writer.WriteLine(); // Add a blank line between categories
					}
				}
			}
			catch (Exception ex)
			{
				if (Settings.DebugLogs)
				{
					Log.Error("Error saving ammo defaults file! " + ex.Message + "\n" + ex.StackTrace);
				}
			}
		}

		internal static void LoadAmmoDefault()
		{
			try
			{
				var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ammoDefault.lts");
				if (!File.Exists(path))
				{
					return;
				}

				SaveFile file;
				var bf = new BinaryFormatter();
				using (var stream = new FileStream(path, FileMode.Open))
				{
					stream.Seek(0, SeekOrigin.Begin);
					file = (SaveFile)bf.Deserialize(stream);
				}

				// Clear and populate ExemptionWeaponDictionary
				Settings.ExemptionWeaponDictionary.Clear();
				foreach (var kvp in file.Exemptions)
				{
					Settings.ExemptionWeaponDictionary.Add(kvp.Key, kvp.Value);
				}

				// Clear and populate CategoryWeaponDictionary
				Settings.CategoryWeaponDictionary.Clear();
				foreach (var kvp in file.Categories)
				{
					Settings.CategoryWeaponDictionary.Add(kvp.Key, kvp.Value);
				}
				
				// Clear and populate BagSettingsDictionary
				Settings.BagSettingsDictionary.Clear();
				foreach (var kvp in file.Bags)
				{
					Settings.BagSettingsDictionary.Add(kvp.Key, kvp.Value);
				}
			}
			catch (Exception ex)
			{
				if (Settings.DebugLogs)
				{
					Log.Error("Error loading ammo defaults file! " + ex.Message + "\n" + ex.StackTrace);
				}
			}
		}
		
		public static void Save()
		{
			try
			{
				var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ammo.lts");

				var file = new SaveFile
				{
					// Copy current dictionaries to the save file
					Categories = new Dictionary<string, Dictionary<string, bool>>(Settings.CategoryWeaponDictionary),
					Exemptions = new Dictionary<string, bool>(Settings.ExemptionWeaponDictionary),
					Bags = new Dictionary<string, BagSettings>(Settings.BagSettingsDictionary)
				};
				var bf = new BinaryFormatter();
				using (var stream = new FileStream(path, FileMode.Create))
				{
					bf.Serialize(stream, file);
				}
			}
			catch (Exception ex)
			{
				if (Settings.DebugLogs)
				{
					Log.Error("Error saving ammo settings! " + ex.Message);
				}
			}
		}
		
		public static void Load()
		{
			try
			{
				var path = Path.Combine(GenFilePaths.ConfigFolderPath, "ammo.lts");
				if (!File.Exists(path))
				{
					return; // If the file doesn't exist, we don't load anything.
				}

				var file = new SaveFile();
				var bf = new BinaryFormatter();
				try
				{
					using (var stream = new FileStream(path, FileMode.Open))
					{
						stream.Seek(0, SeekOrigin.Begin);
						file = (SaveFile)bf.Deserialize(stream);
					}
				}
				catch (Exception ex)
				{
					Log.Error("Error during deserialization: " + ex.Message);
				}

				// Clear existing dictionaries to ensure no stale data
				Settings.ExemptionWeaponDictionary.Clear();
				foreach (var kvp in file.Exemptions)
				{
					Settings.ExemptionWeaponDictionary.Add(kvp.Key, kvp.Value);
				}

				Settings.CategoryWeaponDictionary.Clear();
				foreach (var kvp in file.Categories)
				{
					Settings.CategoryWeaponDictionary.Add(kvp.Key, kvp.Value);
				}

				Settings.BagSettingsDictionary.Clear();
				foreach (var kvp in file.Bags)
				{
					Settings.BagSettingsDictionary.Add(kvp.Key, kvp.Value);
				}
			}
			catch (Exception ex)
			{
				if (Settings.DebugLogs)
				{
					Log.Error("Error loading ammo settings! " + ex.Message + "\n" + ex.StackTrace);
				}
			}
		}
	}
}