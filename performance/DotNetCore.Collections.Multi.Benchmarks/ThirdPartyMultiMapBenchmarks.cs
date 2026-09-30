#if NETFRAMEWORK
using System;
using BenchmarkDotNet.Attributes;
using Dangr.Core.Collections;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    // F6-34: the second head-to-head axis. F6-26 put this repository's ordered types next to a
    // reference implementation that ships a netstandard1.0 asset, so it could run inside the
    // net8.0 harness. The reference on this axis is different: it ships a net461 asset only, so
    // this file is compiled into the Framework target of this project and is excluded from
    // net8.0. That is why the whole file sits behind #if NETFRAMEWORK.
    //
    // Identifiers here deliberately do not repeat the reference package's own name or any
    // abbreviation of it. The package name survives only where it cannot be avoided - the
    // project reference and the using directive - and those two files are the same approved
    // exception the F6-26 file already relies on. Everything that reaches a reader of the
    // desensitized summary (class names, the filter, this file's name) stays neutral.
    //
    // HOW TO RUN IT
    //
    //   dotnet build performance/DotNetCore.Collections.Multi.Benchmarks -c Release -f net461
    //   ./performance/DotNetCore.Collections.Multi.Benchmarks/bin/Release/net461/DotNetCore.Collections.Multi.Benchmarks.exe --filter "*HeadToHead_MultiMap*"
    //
    // The benchmark host itself then runs on the installed .NET Framework runtime, which is the
    // only runtime that can load the reference assembly.
    //
    // WHAT IS COMPARABLE, AND WHAT IS NOT - READ BEFORE READING ANY NUMBER
    //
    // The reference package's published surface is three public types, of which exactly one
    // overlaps with this repository: MultiMap<TKey, TValue>. There is therefore one axis here,
    // and the two sides do not agree on it:
    //
    //  * Multiplicity. On this repository MultiDictionary.Add(key, value) adds one occurrence,
    //    every time. On the reference, the first Add for a key makes one entry and every later
    //    Add for that same key makes two: adding 10, then 11, then 12 to one key reads back as
    //    [10, 11, 11, 12, 12]. So after the same call sequence the two arms hold different
    //    amounts of data, and a ratio between them is a ratio between different amounts of work,
    //    not between two implementations of the same operation.
    //
    //  * Shape. The reference's read side is Get(key) for a single value and GetAll(key) for the
    //    values of one key; it has no key count, no per-key count and no enumerator. This
    //    repository's read side is ContainsKey / ValueCount / this[key]. Only Add and
    //    Remove(key, value) have the same call shape on both sides, so only those two are here.
    //
    //  * Contract on a pair that is already gone. This repository's Remove(key, value) returns
    //    bool - false when the pair is not there - and is safe to call again. The reference's
    //    Remove(key, value) returns void and throws NullReferenceException once the pair has
    //    already been removed. That is measured, not inferred: removing the same pair twice is
    //    what killed the reference arm at Distinct=64 on the first run of this file. The Remove
    //    arm therefore never asks either side to remove a pair twice, which is why its sizes start
    //    at 1024 - the length of the loop - and not at 64.
    //
    // These two arms are therefore written to measure the same call shape, not the same work,
    // and the numbers below must be read as "what each side costs for its own behaviour" rather
    // than as a like-for-like ratio. The design-level statement and the reproduction path for the
    // part that is not measurable at all are in the internal working notes.

    /// <summary>
    /// Add(key, value) on both sides, from an already-populated map so the arms exercise the
    /// existing-key path. Keys are drawn from the populated range on both sides.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_MultiMap_Add
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private MultiDictionary<int, int> _map;
        private MultiMap<int, int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _map = new MultiDictionary<int, int>();
            _reference = new MultiMap<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _map.Add(i, i);
                _reference.Add(i, i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public MultiDictionary<int, int> Collections()
        {
            for (var i = 0; i < Ops; i++)
            {
                _map.Add((i * 7) % Distinct, i);
            }

            return _map;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public MultiMap<int, int> Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                _reference.Add((i * 7) % Distinct, i);
            }

            return _reference;
        }
    }

    /// <summary>
    /// Remove(key, value) on both sides. Both drop a single occurrence, so the loop performs the
    /// same number of removals. The two sides do not share a contract for a pair that is already
    /// gone - this repository returns false, the reference throws - so the sizes are chosen so that
    /// the 1024-step loop removes 1024 distinct pairs and never asks either side to remove a pair
    /// twice. Distinct must therefore be at least Ops; 64 would repeat pairs and kill the reference
    /// arm, which is exactly what happened before this constraint was written down.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_MultiMap_Remove
    {
        private const int Ops = 1024;

        [Params(1024, 4096)]
        public int Distinct;

        private MultiDictionary<int, int> _map;
        private MultiMap<int, int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _map = new MultiDictionary<int, int>();
            _reference = new MultiMap<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _map.Add(i, i);
                _reference.Add(i, i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public int Collections()
        {
            var removed = 0;
            for (var i = 0; i < Ops; i++)
            {
                var key = (i * 7) % Distinct;
                if (_map.Remove(key, key))
                {
                    removed++;
                }
            }

            return removed;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public void Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                var key = (i * 7) % Distinct;
                _reference.Remove(key, key);
            }
        }
    }
}
#endif
