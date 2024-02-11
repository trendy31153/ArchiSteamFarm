//     _                _      _  ____   _                           _____
//    / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
//   / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
//  / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
// /_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|
// |
// Copyright 2015-2024 Łukasz "JustArchi" Domeradzki
// Contact: JustArchi@JustArchi.net
// |
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// |
// http://www.apache.org/licenses/LICENSE-2.0
// |
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Text.Json.Serialization;
using ArchiSteamFarm.Steam.Data;

namespace ArchiSteamFarm.OfficialPlugins.ItemsMatcher.Data;

internal class AssetInInventory : AssetForMatching {
	[JsonInclude]
	[JsonPropertyName("d")]
	[JsonRequired]
	internal readonly ulong AssetID;

	[JsonConstructor]
	protected AssetInInventory() { }

	internal AssetInInventory(Asset asset) : base(asset) {
		ArgumentNullException.ThrowIfNull(asset);

		AssetID = asset.AssetID;
	}

	internal Asset ToAsset() => new(Asset.SteamAppID, Asset.SteamCommunityContextID, ClassID, Amount, tradable: Tradable, assetID: AssetID, realAppID: RealAppID, type: Type, rarity: Rarity);
}
