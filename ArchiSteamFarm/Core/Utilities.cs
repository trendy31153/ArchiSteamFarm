// ----------------------------------------------------------------------------------------------
//     _                _      _  ____   _                           _____
//    / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
//   / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
//  / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
// /_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|
// ----------------------------------------------------------------------------------------------
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
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Resources;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.XPath;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Storage;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Microsoft.IdentityModel.JsonWebTokens;
using SteamKit2;
using Zxcvbn;

namespace ArchiSteamFarm.Core;

public static class Utilities {
	private const byte MaxSharingViolationTries = 15;
	private const uint SharingViolationHResult = 0x80070020;
	private const byte TimeoutForLongRunningTasksInSeconds = 60;

	private static readonly FrozenSet<char> DirectorySeparators = new HashSet<char>(2) { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.ToFrozenSet();

	// normally we'd just use words like "steam" and "farm", but the library we're currently using is a bit iffy about banned words, so we need to also add combinations such as "steamfarm"
	private static readonly FrozenSet<string> ForbiddenPasswordPhrases = new HashSet<string>(10, StringComparer.InvariantCultureIgnoreCase) { "archisteamfarm", "archi", "steam", "farm", "archisteam", "archifarm", "steamfarm", "asf", "asffarm", "password" }.ToFrozenSet(StringComparer.InvariantCultureIgnoreCase);

	[PublicAPI]
	public static string GenerateChecksumFor(byte[] source) {
		ArgumentNullException.ThrowIfNull(source);

		byte[] hash = SHA512.HashData(source);

		return Convert.ToHexString(hash);
	}

	[PublicAPI]
	public static string GetArgsAsText(string[] args, byte argsToSkip, string delimiter) {
		ArgumentNullException.ThrowIfNull(args);

		if (args.Length <= argsToSkip) {
			throw new InvalidOperationException($"{nameof(args.Length)} && {nameof(argsToSkip)}");
		}

		ArgumentException.ThrowIfNullOrEmpty(delimiter);

		return string.Join(delimiter, args.Skip(argsToSkip));
	}

	[PublicAPI]
	public static string GetArgsAsText(string text, byte argsToSkip) {
		ArgumentException.ThrowIfNullOrEmpty(text);

		string[] args = text.Split(Array.Empty<char>(), argsToSkip + 1, StringSplitOptions.RemoveEmptyEntries);

		return args[^1];
	}

	[PublicAPI]
	public static string? GetCookieValue(this CookieContainer cookieContainer, Uri uri, string name) {
		ArgumentNullException.ThrowIfNull(cookieContainer);
		ArgumentNullException.ThrowIfNull(uri);
		ArgumentException.ThrowIfNullOrEmpty(name);

		CookieCollection cookies = cookieContainer.GetCookies(uri);

		return cookies.FirstOrDefault(cookie => cookie.Name == name)?.Value;
	}

	[PublicAPI]
	public static ulong GetUnixTime() => (ulong) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

	[PublicAPI]
	public static async void InBackground(Action action, bool longRunning = false) {
		ArgumentNullException.ThrowIfNull(action);

		TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

		if (longRunning) {
			options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
		}

		await Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default).ConfigureAwait(false);
	}

	[PublicAPI]
	public static void InBackground<T>(Func<T> function, bool longRunning = false) {
		ArgumentNullException.ThrowIfNull(function);

		InBackground(void () => function(), longRunning);
	}

	[PublicAPI]
	public static async Task<IList<T>> InParallel<T>(IEnumerable<Task<T>> tasks) {
		ArgumentNullException.ThrowIfNull(tasks);

		switch (ASF.GlobalConfig?.OptimizationMode) {
			case GlobalConfig.EOptimizationMode.MinMemoryUsage:
				List<T> results = [];

				foreach (Task<T> task in tasks) {
					results.Add(await task.ConfigureAwait(false));
				}

				return results;
			default:
				return await Task.WhenAll(tasks).ConfigureAwait(false);
		}
	}

	[PublicAPI]
	public static async Task InParallel(IEnumerable<Task> tasks) {
		ArgumentNullException.ThrowIfNull(tasks);

		switch (ASF.GlobalConfig?.OptimizationMode) {
			case GlobalConfig.EOptimizationMode.MinMemoryUsage:
				foreach (Task task in tasks) {
					await task.ConfigureAwait(false);
				}

				break;
			default:
				await Task.WhenAll(tasks).ConfigureAwait(false);

				break;
		}
	}

	[PublicAPI]
	public static bool IsClientErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError;

	[PublicAPI]
	public static bool IsRedirectionCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.Ambiguous and < HttpStatusCode.BadRequest;

	[PublicAPI]
	public static bool IsServerErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.InternalServerError and < (HttpStatusCode) 600;

	[PublicAPI]
	public static bool IsSuccessCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;

	[PublicAPI]
	public static bool IsValidCdKey(string key) {
		ArgumentException.ThrowIfNullOrEmpty(key);

		return GeneratedRegexes.CdKey().IsMatch(key);
	}

	[PublicAPI]
	public static bool IsValidHexadecimalText(string text) {
		ArgumentException.ThrowIfNullOrEmpty(text);

		return (text.Length % 2 == 0) && text.All(Uri.IsHexDigit);
	}

	[PublicAPI]
	public static IList<INode> SelectNodes(this IDocument document, string xpath) {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectNodes(xpath);
	}

	[PublicAPI]
	public static IEnumerable<T> SelectNodes<T>(this IDocument document, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectNodes(xpath).OfType<T>();
	}

	[PublicAPI]
	public static IEnumerable<T> SelectNodes<T>(this IElement element, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(element);

		return element.SelectNodes(xpath).OfType<T>();
	}

	[PublicAPI]
	public static INode? SelectSingleNode(this IDocument document, string xpath) {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectSingleNode(xpath);
	}

	[PublicAPI]
	public static T? SelectSingleNode<T>(this IDocument document, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectSingleNode(xpath) as T;
	}

	[PublicAPI]
	public static T? SelectSingleNode<T>(this IElement element, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(element);

		return element.SelectSingleNode(xpath) as T;
	}

	[PublicAPI]
	public static IEnumerable<T> ToEnumerable<T>(this T item) {
		yield return item;
	}

	[PublicAPI]
	public static string ToHumanReadable(this TimeSpan timeSpan) => timeSpan.Humanize(3, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second);

	[PublicAPI]
	public static Task<T> ToLongRunningTask<T>(this AsyncJob<T> job) where T : CallbackMsg {
		ArgumentNullException.ThrowIfNull(job);

		job.Timeout = TimeSpan.FromSeconds(TimeoutForLongRunningTasksInSeconds);

		return job.ToTask();
	}

	[PublicAPI]
	public static Task<AsyncJobMultiple<T>.ResultSet> ToLongRunningTask<T>(this AsyncJobMultiple<T> job) where T : CallbackMsg {
		ArgumentNullException.ThrowIfNull(job);

		job.Timeout = TimeSpan.FromSeconds(TimeoutForLongRunningTasksInSeconds);

		return job.ToTask();
	}

	[PublicAPI]
	public static bool TryReadJsonWebToken(string token, [NotNullWhen(true)] out JsonWebToken? result) {
		ArgumentException.ThrowIfNullOrEmpty(token);

		try {
			result = new JsonWebToken(token);
		} catch (Exception e) {
			ASF.ArchiLogger.LogGenericDebuggingException(e);

			result = null;

			return false;
		}

		return true;
	}

	internal static async Task DeletePotentiallyUsedDirectory(string directory) {
		ArgumentException.ThrowIfNullOrEmpty(directory);

		for (byte i = 1; (i <= MaxSharingViolationTries) && Directory.Exists(directory); i++) {
			if (i > 1) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			try {
				Directory.Delete(directory, true);
			} catch (IOException e) when ((i < MaxSharingViolationTries) && ((uint) e.HResult == SharingViolationHResult)) {
				// It's entirely possible that old process is still running, we allow this to happen and add additional delay
				ASF.ArchiLogger.LogGenericDebuggingException(e);

				continue;
			}

			return;
		}
	}

	internal static ulong MathAdd(ulong first, int second) {
		if (second >= 0) {
			return first + (uint) second;
		}

		return first - (uint) -second;
	}

	internal static void OnProgressChanged(string fileName, byte progressPercentage) {
		ArgumentException.ThrowIfNullOrEmpty(fileName);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(progressPercentage, 100);

		const byte printEveryPercentage = 10;

		if (progressPercentage % printEveryPercentage != 0) {
			return;
		}

		ASF.ArchiLogger.LogGenericDebug($"{fileName} {progressPercentage}%...");
	}

	internal static (bool IsWeak, string? Reason) TestPasswordStrength(string password, IEnumerable<string>? additionallyForbiddenPhrases = null) {
		ArgumentException.ThrowIfNullOrEmpty(password);

		HashSet<string> forbiddenPhrases = ForbiddenPasswordPhrases.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

		if (additionallyForbiddenPhrases != null) {
			forbiddenPhrases.UnionWith(additionallyForbiddenPhrases);
		}

		Result result = Zxcvbn.Core.EvaluatePassword(password, forbiddenPhrases);

		IList<string>? suggestions = result.Feedback.Suggestions;

		if (!string.IsNullOrEmpty(result.Feedback.Warning)) {
			suggestions ??= new List<string>(1);

			suggestions.Insert(0, result.Feedback.Warning);
		}

		if (suggestions != null) {
			for (byte i = 0; i < suggestions.Count; i++) {
				string suggestion = suggestions[i];

				if ((suggestion.Length == 0) || (suggestion[^1] == '.')) {
					continue;
				}

				suggestions[i] = $"{suggestion}.";
			}
		}

		return (result.Score < 4, suggestions is { Count: > 0 } ? string.Join(' ', suggestions.Where(static suggestion => suggestion.Length > 0)) : null);
	}

	internal static async Task<bool> UpdateFromArchive(ZipArchive zipArchive, string targetDirectory) {
		ArgumentNullException.ThrowIfNull(zipArchive);
		ArgumentException.ThrowIfNullOrEmpty(targetDirectory);

		try {
			// Firstly, ensure once again our directories are purged and ready to work with
			foreach (string directory in Directory.EnumerateDirectories(targetDirectory, $"{SharedInfo.UpdateDirectoryNewPrefix}*")) {
				ASF.ArchiLogger.LogGenericInfo(Strings.UpdateCleanup);

				Directory.Delete(directory, true);

				ASF.ArchiLogger.LogGenericInfo(Strings.Done);
			}

			foreach (string directory in Directory.EnumerateDirectories(targetDirectory, $"{SharedInfo.UpdateDirectoryOldPrefix}*")) {
				ASF.ArchiLogger.LogGenericInfo(Strings.UpdateCleanup);

				await DeletePotentiallyUsedDirectory(directory).ConfigureAwait(false);

				ASF.ArchiLogger.LogGenericInfo(Strings.Done);
			}

			string oldBackupDirectory = Path.Combine(targetDirectory, SharedInfo.UpdateDirectoryOld);

			if (Directory.Exists(oldBackupDirectory)) {
				ASF.ArchiLogger.LogGenericInfo(Strings.UpdateCleanup);

				await DeletePotentiallyUsedDirectory(oldBackupDirectory).ConfigureAwait(false);

				ASF.ArchiLogger.LogGenericInfo(Strings.Done);
			}
		} catch (Exception e) {
			ASF.ArchiLogger.LogGenericException(e);

			return false;
		}

		// Create temporary directories, we append current unix timestamp to ensure that our folders are unique, which helps to avoid delete-create race conditions for the same path
		ulong unixTime = GetUnixTime();

		string backupDirectoryName = $"{SharedInfo.UpdateDirectoryOldPrefix}{unixTime}";
		string updateDirectoryName = $"{SharedInfo.UpdateDirectoryNewPrefix}{unixTime}";

		string backupDirectory = Path.Combine(targetDirectory, backupDirectoryName);
		string updateDirectory = Path.Combine(targetDirectory, updateDirectoryName);

		// Now enumerate over files in the zip archive and extract them to entirely new location, this decreases chance of corruptions if user kills the process during this stage
		Directory.CreateDirectory(updateDirectory);

		foreach (ZipArchiveEntry zipFile in zipArchive.Entries) {
			switch (zipFile.Name) {
				case null:
				case "":
				case ".gitkeep":
					// We're not interested in extracting placeholder files
					continue;
			}

			string file = Path.GetFullPath(Path.Combine(updateDirectory, zipFile.FullName));

			if (!file.StartsWith(updateDirectory, StringComparison.Ordinal)) {
				throw new InvalidOperationException(nameof(file));
			}

			// Check if this file requires its own folder
			if (zipFile.Name != zipFile.FullName) {
				string? directory = Path.GetDirectoryName(file);

				if (string.IsNullOrEmpty(directory)) {
					throw new InvalidOperationException(nameof(directory));
				}

				Directory.CreateDirectory(directory);
			}

			zipFile.ExtractToFile(file);
		}

		// Now, critical section begins, we're going to move all files from target directory to a backup directory
		Directory.CreateDirectory(backupDirectory);

		foreach (string file in Directory.EnumerateFiles(targetDirectory, "*", SearchOption.AllDirectories)) {
			string fileName = Path.GetFileName(file);

			if (string.IsNullOrEmpty(fileName)) {
				throw new InvalidOperationException(nameof(fileName));
			}

			string relativeFilePath = Path.GetRelativePath(targetDirectory, file);

			if (string.IsNullOrEmpty(relativeFilePath)) {
				throw new InvalidOperationException(nameof(relativeFilePath));
			}

			string? relativeDirectoryName = Path.GetDirectoryName(relativeFilePath);

			switch (relativeDirectoryName) {
				case null:
					throw new InvalidOperationException(nameof(relativeDirectoryName));
				case "":
					// No directory, root folder
					switch (fileName) {
						case Logging.NLogConfigurationFile:
						case SharedInfo.LogFile:
							// Files with those names in root directory we want to keep
							continue;
					}

					break;
				case SharedInfo.ArchivalLogsDirectory:
				case SharedInfo.ConfigDirectory:
				case SharedInfo.DebugDirectory:
				case SharedInfo.PluginsDirectory:
				case SharedInfo.UpdateDirectoryOld:
					// Files in those constant directories we want to keep in their current place
					continue;
				default:
					// Files in those non-constant directories we want to keep in their current place
					if ((relativeDirectoryName == backupDirectoryName) || (relativeDirectoryName == updateDirectoryName)) {
						continue;
					}

					// Files in subdirectories of those directories we want to keep as well
					if (RelativeDirectoryStartsWith(relativeDirectoryName, SharedInfo.ArchivalLogsDirectory, SharedInfo.ConfigDirectory, SharedInfo.DebugDirectory, SharedInfo.PluginsDirectory, SharedInfo.UpdateDirectoryOld, backupDirectoryName, updateDirectoryName)) {
						continue;
					}

					break;
			}

			// We're going to move this file out of the current place, overwriting existing one if needed
			string targetBackupDirectory;

			if (relativeDirectoryName.Length > 0) {
				// File inside a subdirectory
				targetBackupDirectory = Path.Combine(backupDirectory, relativeDirectoryName);

				Directory.CreateDirectory(targetBackupDirectory);
			} else {
				// File in root directory
				targetBackupDirectory = backupDirectory;
			}

			string targetBackupFile = Path.Combine(targetBackupDirectory, fileName);

			File.Move(file, targetBackupFile, true);
		}

		// Finally, we can move the newly extracted files to target directory
		foreach (string file in Directory.EnumerateFiles(updateDirectory, "*", SearchOption.AllDirectories)) {
			string fileName = Path.GetFileName(file);

			if (string.IsNullOrEmpty(fileName)) {
				throw new InvalidOperationException(nameof(fileName));
			}

			string relativeFilePath = Path.GetRelativePath(updateDirectory, file);

			if (string.IsNullOrEmpty(relativeFilePath)) {
				throw new InvalidOperationException(nameof(relativeFilePath));
			}

			string? relativeDirectoryName = Path.GetDirectoryName(relativeFilePath);

			if (relativeDirectoryName == null) {
				throw new InvalidOperationException(nameof(relativeDirectoryName));
			}

			// We're going to move this file out of the current place, overwriting existing one if needed
			string targetUpdateDirectory;

			if (relativeDirectoryName.Length > 0) {
				// File inside a subdirectory
				targetUpdateDirectory = Path.Combine(targetDirectory, relativeDirectoryName);

				Directory.CreateDirectory(targetUpdateDirectory);
			} else {
				// File in root directory
				targetUpdateDirectory = targetDirectory;
			}

			string targetUpdateFile = Path.Combine(targetUpdateDirectory, fileName);

			File.Move(file, targetUpdateFile, true);
		}

		// Critical section has finished, we can now cleanup the update directory, backup directory must wait for the process restart
		Directory.Delete(updateDirectory, true);

		// The update process is done
		return true;
	}

	internal static void WarnAboutIncompleteTranslation(ResourceManager resourceManager) {
		ArgumentNullException.ThrowIfNull(resourceManager);

		// Skip translation progress for English and invariant (such as "C") cultures
		switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) {
			case "en" or "iv" or "qps":
				return;
		}

		// We can't dispose this resource set, as we can't be sure if it isn't used somewhere else, rely on GC in this case
		ResourceSet? defaultResourceSet = resourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en-US"), true, true);

		if (defaultResourceSet == null) {
			ASF.ArchiLogger.LogNullError(defaultResourceSet);

			return;
		}

		HashSet<DictionaryEntry> defaultStringObjects = defaultResourceSet.Cast<DictionaryEntry>().ToHashSet();

		if (defaultStringObjects.Count == 0) {
			// This means we don't have entries for English, so there is nothing to check against
			// Can happen e.g. for plugins with no strings declared which are calling this function
			return;
		}

		// We can't dispose this resource set, as we can't be sure if it isn't used somewhere else, rely on GC in this case
		ResourceSet? currentResourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

		if (currentResourceSet == null) {
			ASF.ArchiLogger.LogNullError(currentResourceSet);

			return;
		}

		HashSet<DictionaryEntry> currentStringObjects = currentResourceSet.Cast<DictionaryEntry>().ToHashSet();

		if (currentStringObjects.Count >= defaultStringObjects.Count) {
			// Either we have 100% finished translation, or we're missing it entirely and using en-US
			HashSet<DictionaryEntry> testStringObjects = currentStringObjects.ToHashSet();
			testStringObjects.ExceptWith(defaultStringObjects);

			// If we got 0 as final result, this is the missing language
			// Otherwise it's just a small amount of strings that happen to be the same
			if (testStringObjects.Count == 0) {
				currentStringObjects = testStringObjects;
			}
		}

		if (currentStringObjects.Count < defaultStringObjects.Count) {
			float translationCompleteness = currentStringObjects.Count / (float) defaultStringObjects.Count;
			ASF.ArchiLogger.LogGenericInfo(string.Format(CultureInfo.CurrentCulture, Strings.TranslationIncomplete, $"{CultureInfo.CurrentUICulture.Name} ({CultureInfo.CurrentUICulture.EnglishName})", translationCompleteness.ToString("P1", CultureInfo.CurrentCulture)));
		}
	}

	private static bool RelativeDirectoryStartsWith(string directory, params string[] prefixes) {
		ArgumentException.ThrowIfNullOrEmpty(directory);

		if ((prefixes == null) || (prefixes.Length == 0)) {
			throw new ArgumentNullException(nameof(prefixes));
		}

		return prefixes.Any(prefix => (directory.Length > prefix.Length) && DirectorySeparators.Contains(directory[prefix.Length]) && directory.StartsWith(prefix, StringComparison.Ordinal));
	}
}
