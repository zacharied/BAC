// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

// The visual/unit test project lives in a separate assembly but drives internal types
// (the ruleset's beatmap generator, hit objects, etc.), mirroring how the official
// ppy rulesets expose their internals to their own test assemblies.
[assembly: InternalsVisibleTo("osu.Game.Rulesets.BigAssCircle.Tests")]
