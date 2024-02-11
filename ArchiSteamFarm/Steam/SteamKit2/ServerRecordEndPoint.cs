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
using System.ComponentModel;
using System.Text.Json.Serialization;
using SteamKit2;

namespace ArchiSteamFarm.Steam.SteamKit2;

internal sealed class ServerRecordEndPoint : IEquatable<ServerRecordEndPoint> {
	[JsonInclude]
	[JsonRequired]
	internal readonly string Host = "";

	[JsonInclude]
	[JsonRequired]
	internal readonly ushort Port;

	[JsonInclude]
	[JsonRequired]
	internal readonly ProtocolTypes ProtocolTypes;

	internal ServerRecordEndPoint(string host, ushort port, ProtocolTypes protocolTypes) {
		ArgumentException.ThrowIfNullOrEmpty(host);
		ArgumentOutOfRangeException.ThrowIfZero(port);

		if (protocolTypes == 0) {
			throw new InvalidEnumArgumentException(nameof(protocolTypes), (int) protocolTypes, typeof(ProtocolTypes));
		}

		Host = host;
		Port = port;
		ProtocolTypes = protocolTypes;
	}

	[JsonConstructor]
	private ServerRecordEndPoint() { }

	public bool Equals(ServerRecordEndPoint? other) => (other != null) && (ReferenceEquals(other, this) || ((Host == other.Host) && (Port == other.Port) && (ProtocolTypes == other.ProtocolTypes)));
	public override bool Equals(object? obj) => (obj != null) && ((obj == this) || (obj is ServerRecordEndPoint serverRecord && Equals(serverRecord)));
	public override int GetHashCode() => HashCode.Combine(Host, Port, ProtocolTypes);
}
