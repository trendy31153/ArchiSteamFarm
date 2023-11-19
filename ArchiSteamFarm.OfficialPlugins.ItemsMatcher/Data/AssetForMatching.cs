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

internal class AssetForMatching : IEquatable<AssetForMatching> {
	[JsonProperty("a", Required = Required.Always)]
	internal readonly uint Amount;

	[JsonProperty("c", Required = Required.Always)]
	internal readonly ulong ClassID;

	[JsonProperty("r", Required = Required.Always)]
	internal readonly Asset.ERarity Rarity;

	[JsonProperty("e", Required = Required.Always)]
	internal readonly uint RealAppID;

	[JsonProperty("t", Required = Required.Always)]
	internal readonly bool Tradable;

	[JsonProperty("p", Required = Required.Always)]
	internal readonly Asset.EType Type;

	[JsonConstructor]
	protected AssetForMatching() { }

	internal AssetForMatching(Asset asset) {
		ArgumentNullException.ThrowIfNull(asset);

		Amount = asset.Amount;

		ClassID = asset.ClassID;
		Tradable = asset.Tradable;

		RealAppID = asset.RealAppID;
		Type = asset.Type;
		Rarity = asset.Rarity;
	}

	public bool Equals(AssetForMatching? other) {
		if (ReferenceEquals(null, other)) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return (Amount == other.Amount) && (ClassID == other.ClassID) && (Rarity == other.Rarity) && (RealAppID == other.RealAppID) && (Tradable == other.Tradable) && (Type == other.Type);
	}

	public override bool Equals(object? obj) {
		if (ReferenceEquals(null, obj)) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		return obj is AssetForMatching assetForMatching && Equals(assetForMatching);
	}

	public override int GetHashCode() => HashCode.Combine(Amount, ClassID, Rarity, RealAppID, Tradable, Type);
}
