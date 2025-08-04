/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

using Microsoft.EntityFrameworkCore;
using SMDataGenerator.Data;
using SMDataGenerator.Models;
using System.Linq.Dynamic.Core;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Debug? Enter any key");
        var input = Console.ReadLine();
        if (input != "")
        {
            Console.WriteLine("Please attach debugger now");
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Threading.Thread.Sleep(100);
            }
            Console.WriteLine("Debugger attached");
        }

        int smCount = 100;
        int smePerSm = 5;
        int smeChildrenPerSme = 5;
        int maxDepth = 4;

        int SMID = 1;
        int SMEID = 1;
        int VALUEID = 1;
        int valueCounter = 0;

        string[] idShorts = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c => c.ToString()).ToArray();

        using var db = new AppDbContext();

        SMID = db.SMs.Max(x => x.Id) + 1;
        SMEID = db.SMEs.Max(x => x.Id) + 1;
        VALUEID = db.Values.Max(x => x.Id) + 1;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine($"SMID {SMID} SMEID {SMEID} VALUEID {VALUEID}");
            Console.WriteLine("0 Stop");
            Console.WriteLine("1 Delete DB");
            Console.WriteLine("2 ADD SMs");
            Console.WriteLine("3 COUNT SM SME VALUE");
            Console.WriteLine("4 PAGE SM SME VALUE");
            Console.WriteLine("5 /submodels/ID: list of SMEs and values for SM ID");
            Console.WriteLine("6 /submodels/query: list of SM IDs");
            Console.WriteLine("7 EDIT SME");

            input = Console.ReadLine();

            if (input == "0")
            {
                break;
            }
            else if (input == "1")
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
                SMID = 1;
                SMEID = 1;
                VALUEID = 1;
            }
            else if (input == "2")
            {
                Console.WriteLine("SM#?");
                input = Console.ReadLine();
                if (input == "")
                {
                    continue;
                }
                smCount = Convert.ToInt32(input);

                db.ChangeTracker.AutoDetectChangesEnabled = false;

                var watch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < smCount; i++)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        break;
                    }

                    int countSME = 0;
                    var smes = new List<SME>();
                    var values = new List<Value>();

                    var sm = new SM
                    {
                        Id = SMID++,
                        Identifier = Guid.NewGuid().ToString(),
                        A = "A",
                        B = "B",
                        C = "C",
                        D = "D",
                        E = "E",
                        F = "F"
                    };

                    db.SMs.Add(sm);

                    for (int j = 0; j < smePerSm; j++)
                    {
                        countSME++;
                        var rootSme = new SME
                        {
                            Id = SMEID++,
                            SMId = sm.Id,
                            IdShort = idShorts[j % idShorts.Length],
                            IdShortPath = idShorts[j % idShorts.Length],
                            A = "A",
                            B = "B",
                            C = "C",
                            D = "D",
                            E = "E",
                            F = "F"
                        };

                        smes.Add(rootSme);

                        GenerateChildren(sm, rootSme, 1, maxDepth, smeChildrenPerSme, ref SMEID, ref VALUEID, ref valueCounter, ref countSME, ref smes, ref values, idShorts);
                    }

                    db.SMEs.AddRangeAsync(smes);
                    db.Values.AddRangeAsync(values);

                    if (i % 10 == 0)
                    {
                        await db.SaveChangesAsync();
                        db.ChangeTracker.Clear();
                        Console.WriteLine($"SMID {SMID} SMEID {SMEID} VALUEID {VALUEID}");
                    }

                    var elapsed = watch.ElapsedMilliseconds;
                    Console.WriteLine($"{elapsed/1000}s {i} : sm {sm.Identifier} sme# {countSME}");
                }

                await db.SaveChangesAsync();
            }
            else if (input == "3")
            {
                Console.WriteLine("(SM | SME | VALUE) COUNT");
                input = Console.ReadLine();
                if (input != "")
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var count = 0;
                    long elapsed = 0;
                    var split = input.Split(" ");
                    switch(split[0])
                    {
                        case "SM":
                            count = db.SMs.Count();
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms SM COUNT: {count}");
                            break;
                        case "SME":
                            count = db.SMEs.Count();
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms SME COUNT: {count}");
                            break;
                        case "VALUE":
                            count = db.Values.Count();
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms VALUE COUNT: {count}");
                            break;
                    }
                }
            }
            else if (input == "4")
            {
                Console.WriteLine("(SM | SME | VALUE) (CURSOR# LIMIT#)");
                input = Console.ReadLine();
                if (input != "")
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var cursor = 0;
                    var limit = 0;
                    long elapsed = 0;
                    var split = input.Split(" ");
                    switch (split[0])
                    {
                        case "SM":
                            cursor = Convert.ToInt32(split[1]);
                            limit = Convert.ToInt32(split[2]);
                            var listsm = db.SMs.Skip(cursor).Take(limit).ToList();
                            foreach (var l in listsm)
                            {
                                Console.WriteLine($"{l.Identifier}");
                            }
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms");
                            break;
                        case "SME":
                            cursor = Convert.ToInt32(split[1]);
                            limit = Convert.ToInt32(split[2]);
                            var listsme = db.SMEs.Skip(cursor).Take(limit).ToList();
                            foreach (var l in listsme)
                            {
                                Console.WriteLine($"{l.IdShortPath}");
                            }
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms");
                            break;
                        case "VALUE":
                            cursor = Convert.ToInt32(split[1]);
                            limit = Convert.ToInt32(split[2]);
                            var listv = db.Values.Skip(cursor).Take(limit).ToList();
                            foreach (var l in listv)
                            {
                                Console.WriteLine($"{l.value}");
                            }
                            elapsed = watch.ElapsedMilliseconds;
                            Console.WriteLine($"{elapsed}ms");
                            break;
                    }
                }
            }
            else if (input == "5")
            {
                Console.WriteLine("SM ID");
                input = Console.ReadLine();
                if (input != "")
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var sm = db.SMs.FirstOrDefault(sm => sm.Identifier.Equals(input));
                    if (sm != null)
                    {
                        var sme = db.SMEs.Where(sme => sme.SMId == sm.Id);
                        var value = db.Values.Where(sme => sme.SMId == sm.Id);

                        var j = sme.Join(
                            value,
                            "Id",
                            "SMEId",
                            "new ( outer.Id as Id, outer.IdShortPath as IdShortPath, inner.value as value )"
                            );
                        var list = j.ToDynamicList();
                        var smeList = list.Select(sme => sme.Id).ToList();
                        var smeWithoutValue = sme.Where(sme => !smeList.Contains(sme.Id)).ToList();

                        var elapsed = watch.ElapsedMilliseconds;

                        List<String> result = [];
                        foreach (var l in list)
                        {
                            result.Add($"{l.Id} {l.IdShortPath} {l.value}");
                        }
                        foreach (var s in smeWithoutValue)
                        {
                            result.Add($"{s.Id} {s.IdShortPath}");
                        }
                        result.Sort();

                        foreach (var r in result)
                        {
                            Console.WriteLine(r);
                        }

                        Console.WriteLine($"{elapsed}ms");
                    }
                }
            }
            else if (input == "6")
            {
                Console.WriteLine("SM ID contains (or empty):");
                var idContains = Console.ReadLine();
                Console.WriteLine("SME idShortPath equals (or empty):");
                var idshortPathEquals = Console.ReadLine();
                Console.WriteLine("Value equals (or empty):");
                var valueEquals = Console.ReadLine();
                Console.WriteLine("CURSOR# LIMIT#");
                input = Console.ReadLine();
                var split = input.Split(" ");
                var cursor = Convert.ToInt32(split[0]);
                var limit = Convert.ToInt32(split[1]);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                var expression = "";
                IQueryable smQuery = null;
                IQueryable smeQuery = null;
                IQueryable valueQuery = null;
                IQueryable result = null;

                if (idContains != null && idContains != "")
                {
                    smQuery = db.SMs.Where(s => s.Identifier.Contains(idContains));
                    result = smQuery.Select("Id").Distinct();
                }
                if (!string.IsNullOrEmpty(valueEquals))
                {
                    valueQuery = db.Values.Where(s => s.value == valueEquals);
                    if (result == null || smQuery == null)
                    {
                        result = valueQuery.Select("SMId").Distinct();
                    }
                    else
                    {
                        smQuery = smQuery.Join(
                            valueQuery,
                            "Id",
                            "SMId",
                            "new (inner.SMId as Id)"
                            );
                        result = smQuery.Select("Id").Distinct();
                    }
                }
                if (!string.IsNullOrEmpty(idshortPathEquals))
                {
                    smeQuery = db.SMEs.Where(s => s.IdShortPath == idshortPathEquals);
                    if (result == null || smQuery == null)
                    {
                        result = smeQuery.Select("SMId").Distinct();
                    }
                    else
                    {
                        smQuery = smQuery.Join(
                            smeQuery,
                            "Id",
                            "SMId",
                            "new (inner.SMId as Id)"
                            );
                        result = smQuery.Select("Id").Distinct();
                    }
                }

                List<string> list = [];
                long elapsed = 0;
                if (result != null)
                {
                    var smIdList = result.Skip(cursor).Take(limit).Distinct().ToDynamicList<int>();
                    Console.WriteLine($"{elapsed}ms SM IDs found");
                    elapsed = watch.ElapsedMilliseconds;
                    list = db.SMs.Where(s => smIdList.Contains(s.Id)).Select(s => s.Identifier).ToList();
                }
                elapsed = watch.ElapsedMilliseconds;

                foreach (var l in list)
                {
                    Console.WriteLine(l);
                }

                Console.WriteLine($"{elapsed}ms {list.Count} found");
            }
            else if (input == "7")
            {
                Console.WriteLine("SME ID:");
                input = Console.ReadLine();
                var smeId = Convert.ToInt32(input);

                var sme = db.SMEs.FirstOrDefault(sme => sme.Id == smeId);
                if (sme == null)
                {
                    Console.WriteLine("SME does not exist!");
                }
                else
                {
                    var smeValue = db.Values.FirstOrDefault(s => s.SMEId == smeId);
                    if (smeValue == null)
                    {
                        Console.WriteLine("SME Value does not exist!");
                    }
                    else
                    {
                        Console.WriteLine($"IdShort (or empty): {sme.IdShort}");
                        var idshort = Console.ReadLine();
                        if (idshort != null && idshort != "")
                        {
                            sme.IdShort = idshort;
                            int lastDotIndex = sme.IdShortPath.LastIndexOf('.');
                            if (lastDotIndex != -1)
                            {
                                sme.IdShortPath = sme.IdShortPath.Substring(0, lastDotIndex + 1) + idshort;
                            }
                        }
                        Console.WriteLine($"Value (or empty): {smeValue.value}");
                        var value = Console.ReadLine();
                        if (value != null && value != "")
                        {
                            smeValue.value = value;
                        }
                        db.SaveChanges();
                    }
                }
            }
        }
    }
    static void GenerateChildren(
        SM sm,
        SME parent,
        int depth,
        int maxDepth,
        int smeChildrenPerSme,
        ref int SMEID,
        ref int VALUEID,
        ref int valueCounter,
        ref int countSME,
        ref List<SME> smes,
        ref List<Value> values,
        string[] idShorts)
    {
        if (depth >= maxDepth)
        {
            var value = new Value
            {
                Id = VALUEID++,
                SMId = sm.Id,
                SMEId = parent.Id,
                value = (valueCounter++ % 100000).ToString()
            };
            values.Add(value);
            return;
        }

        for (int k = 0; k < smeChildrenPerSme; k++)
        {
            countSME++;
            var idShort = idShorts[k % idShorts.Length];
            var child = new SME
            {
                Id = SMEID++,
                SMId = parent.SMId,
                ParentSMEId = parent.Id,
                IdShort = idShort,
                IdShortPath = $"{parent.IdShortPath}.{idShort}",
                A = "A",
                B = "B",
                C = "C",
                D = "D",
                E = "E",
                F = "F"
            };

            smes.Add(child);
            GenerateChildren(sm, child, depth + 1, maxDepth, smeChildrenPerSme, ref SMEID, ref VALUEID, ref valueCounter, ref countSME, ref smes, ref values, idShorts);
        }
    }
}
