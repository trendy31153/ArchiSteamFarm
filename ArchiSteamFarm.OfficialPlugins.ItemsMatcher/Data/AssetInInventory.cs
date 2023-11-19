//     _                _      _  ____   _                           _____
//    / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
//   / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
//  / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
// /_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|
// |
// Copyright 2015-2023 Łukasz "JustArchi" Domeradzki
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
using ArchiSteamFarm.Steam.Data;
using Newtonsoft.Json;

namespace ArchiSteamFarm.OfficialPlugins.ItemsMatcher.Data;

internal class AssetInInventory : AssetForMatching, IEquatable<AssetInInventory> {
	[JsonProperty("d", Required = Required.Always)]
	internal readonly ulong AssetID;

	internal AssetInInventory(Asset asset) : base(asset) {
		ArgumentNullException.ThrowIfNull(asset);

		AssetID = asset.AssetID;
	}

	[JsonConstructor]
	private AssetInInventory() { }

	public bool Equals(AssetInInventory? other) {
		if (ReferenceEquals(null, other)) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return (AssetID == other.AssetID) && base.Equals(other);
	}

	public override bool Equals(object? obj) {
		if (ReferenceEquals(null, obj)) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		return obj is AssetInInventory other && Equals(other);
	}

	public override int GetHashCode() => HashCode.Combine(AssetID, Amount, ClassID, Rarity, RealAppID, Tradable, Type);

	internal Asset ToAsset() => new(Asset.SteamAppID, Asset.SteamCommunityContextID, ClassID, Amount, tradable: Tradable, assetID: AssetID, realAppID: RealAppID, type: Type, rarity: Rarity);
}
