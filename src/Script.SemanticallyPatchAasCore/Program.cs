using Console = System.Console;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using Path = System.IO.Path;
using System.Collections.Generic;
using Syntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CommandLine;
using System.Linq;


namespace Script.SemanticallyPatchAasCore
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        interface IPatch
        {
            public int Position { get; }
            public int End { get; }
            public string Text { get; }
        }

        class InsertPrefix : IPatch
        {
            public int Position { get; }
            public string Text { get; }

            public int End => Position;

            public InsertPrefix(int position, string text)
            {
                Position = position;
                Text = text;
            }
        }

        class InsertSuffix : IPatch
        {
            public int Position { get; }
            public string Text { get; }

            public int End => Position;

            public InsertSuffix(int position, string text)
            {
                Position = position;
                Text = text;
            }
        }

        class Replace : IPatch
        {
            public int Position { get; }
            public int End { get; }
            public string Text { get; }

            Replace(int position, int end, string text)
            {
                Position = position;
                End = end;
                Text = text;
            }

            public Replace Clone()
            {
                return new Replace(Position, End, Text);
            }
        }

        private static string ApplyPatches(List<IPatch> patches, string text)
        {
            var sortedPatches = new List<IPatch>(patches);
            sortedPatches.Sort(
                (that, other) => that.Position - other.Position
            );

            #region CheckNoOverlapBetweenReplaces

            Replace? prevReplace = null;

            foreach (var replace in sortedPatches.OfType<Replace>())
            {
                if (prevReplace != null)
                {
                    if (prevReplace.End > replace.Position)
                    {
                        throw new System.InvalidOperationException(
                            "The two replace operations overlap: " +
                            $"({replace.Position}, {replace.End}, {replace.Text}) " +
                            $"and ({prevReplace.Position}, {prevReplace.End}, {prevReplace.Text})"
                        );
                    }
                }

                prevReplace = replace;
            }

            #endregion

            #region MergeInserts

            var mergedInserts = new List<IPatch>();

            int lastInsertPrefixPosition = -1;
            string lastInsertPrefixText = "";

            int lastInsertSuffixPosition = -1;
            string lastInsertSuffixText = "";

            foreach (var patch in sortedPatches)
            {
                switch (patch)
                {
                    case InsertPrefix insertPrefix:
                        if (lastInsertPrefixPosition == insertPrefix.Position)
                        {
                            // NOTE (mristin):
                            // This has quadratic time complexity, but merges are seldom so we see no need
                            // to make the code more complex than this.
                            lastInsertPrefixText =
                                $"{insertPrefix.Text}{lastInsertPrefixText}";
                        }
                        else
                        {
                            if (lastInsertPrefixPosition != -1)
                            {
                                mergedInserts.Add(
                                    new InsertPrefix(lastInsertPrefixPosition,
                                        lastInsertPrefixText)
                                );
                            }

                            lastInsertPrefixPosition = insertPrefix.Position;
                            lastInsertPrefixText = insertPrefix.Text;
                        }

                        break;
                    case InsertSuffix insertSuffix:
                        if (lastInsertSuffixPosition == insertSuffix.Position)
                        {
                            // NOTE (mristin):
                            // This has quadratic time complexity, but merges are seldom so we see no need
                            // to make the code more complex than this.
                            lastInsertSuffixText =
                                $"{insertSuffix.Text}{lastInsertSuffixText}";
                        }
                        else
                        {
                            if (lastInsertSuffixPosition != -1)
                            {
                                mergedInserts.Add(
                                    new InsertSuffix(lastInsertSuffixPosition,
                                        lastInsertSuffixText)
                                );
                            }

                            lastInsertSuffixPosition = insertSuffix.Position;
                            lastInsertSuffixText = insertSuffix.Text;
                        }

                        break;
                        // We pass here, as there is nothing to merge for other types.
                }
            }

            if (lastInsertPrefixPosition != -1)
            {
                mergedInserts.Add(
                    new InsertPrefix(lastInsertPrefixPosition, lastInsertPrefixText)
                );
            }

            if (lastInsertSuffixPosition != -1)
            {
                mergedInserts.Add(
                    new InsertSuffix(lastInsertSuffixPosition, lastInsertSuffixText)
                );
            }

            #endregion

            #region MergeSortOnMergedInsertsAndReplaces

            using var replacesEnumerator =
                sortedPatches.OfType<Replace>().GetEnumerator();
            using var insertsEnumerator = mergedInserts.GetEnumerator();

            var replacesHasNext = replacesEnumerator.MoveNext();
            var insertsHasNext = insertsEnumerator.MoveNext();

            var mergedPatches = new List<IPatch>(patches.Count);

            while (true)
            {
                if (!replacesHasNext && !insertsHasNext)
                {
                    break;
                }
                else if (replacesHasNext && !insertsHasNext)
                {
                    mergedPatches.Add(replacesEnumerator.Current);

                    replacesHasNext = replacesEnumerator.MoveNext();
                }
                else if (!replacesHasNext && insertsHasNext)
                {
                    mergedPatches.Add(insertsEnumerator.Current);

                    insertsHasNext = insertsEnumerator.MoveNext();
                }
                else
                {
                    if (insertsEnumerator.Current.Position <
                        replacesEnumerator.Current.Position)
                    {
                        mergedPatches.Add(insertsEnumerator.Current);
                        insertsHasNext = insertsEnumerator.MoveNext();
                    }
                    else
                    {
                        // NOTE (mristin):
                        // We clone here as we do not want the original `patches` to remain
                        // immutable.
                        mergedPatches.Add(replacesEnumerator.Current.Clone());
                        replacesHasNext = replacesEnumerator.MoveNext();
                    }
                }
            }

            #endregion

            var parts = new List<string>();

            IPatch? previousPatch = null;
            foreach (var patch in mergedPatches)
            {
                if (previousPatch == null)
                {
                    parts.Add(
                        text.Substring(0, patch.Position)
                    );
                }
                else
                {
                    parts.Add(
                        text.Substring(
                            previousPatch.End,
                            patch.Position - previousPatch.End
                        )
                    );
                }

                parts.Add(patch.Text);
                previousPatch = patch;
            }

            if (previousPatch != null)
            {
                parts.Add(
                    text.Substring(
                        previousPatch.End,
                        text.Length - previousPatch.End
                    )
                );
            }

            return string.Join("", parts);
        }

        private static bool PatchTypes(string srcTypesPath, string tgtTypesPath)
        {
            string text = File.ReadAllText(srcTypesPath);
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                text
            );

            var root = (Syntax.CompilationUnitSyntax)tree.GetRoot();

            var patches = new List<IPatch>();

            #region FindTheLastUsingAndAddNewtownsoft

            {
                Syntax.UsingDirectiveSyntax? lastUsingDirective = null;
                foreach (
                    var usingDirective
                    in root.DescendantNodes().OfType<Syntax.UsingDirectiveSyntax>()
                )
                {
                    if (lastUsingDirective == null
                        || usingDirective.GetLocation().SourceSpan.Start >
                        lastUsingDirective.GetLocation().SourceSpan.Start
                    )
                    {
                        lastUsingDirective = usingDirective;
                    }
                }

                if (lastUsingDirective == null)
                {
                    Console.Error.WriteLine(
                        $"No using directives found in: {srcTypesPath}"
                    );
                    return false;
                }

                patches.Add(
                    new InsertSuffix(
                        lastUsingDirective.HasTrailingTrivia
                            ? lastUsingDirective.GetTrailingTrivia().Span.End
                            : lastUsingDirective.GetLocation().SourceSpan.End,
                        "\nusing Newtonsoft.Json;  // can't alias\n"
                    )
                );
            }

            #endregion

            #region AddTimestampToAllReferables

            {
                var referable =
                    root.DescendantNodes()
                        .OfType<Syntax.InterfaceDeclarationSyntax>()
                        .FirstOrDefault(
                            interfaceDeclaration =>
                                interfaceDeclaration.Identifier.Text ==
                                "IReferable"
                        );

                if (referable == null)
                {
                    Console.Error.WriteLine(
                        $"No interface IReferable found in: {srcTypesPath}"
                    );
                    return false;
                }

                string snippet =
                    "\n" +
                    "        #region Parent\n" +
                    "        [JsonIgnore]\n" +
                    "        public IClass? Parent { get; set; }\n" +
                    "        #endregion\n" +
                    "\n" +
                    "        #region TimeStamp\n" +
                    "        [JsonIgnore]\n" +
                    "        public System.DateTime TimeStampCreate { get; set; }\n" +
                    "        [JsonIgnore]\n" +
                    "        public System.DateTime TimeStamp { get; set; }\n" +
                    "        [JsonIgnore]\n" +
                    "        public System.DateTime TimeStampTree { get; set; }\n" +
                    "        #endregion\n\n";

                patches.Add(
                    new InsertSuffix(
                        referable.OpenBraceToken.Span.End,
                        snippet
                    )
                );

                var interfaceChildren = new Dictionary<string, List<string>>();
                foreach (
                    var interfaceDeclaration
                    in root.DescendantNodes()
                        .OfType<Syntax.InterfaceDeclarationSyntax>()
                )
                {
                    var interfaceName = interfaceDeclaration.Identifier.Text;

                    interfaceChildren[interfaceName] = new List<string>();
                }

                foreach (
                    var interfaceDeclaration
                    in root.DescendantNodes()
                        .OfType<Syntax.InterfaceDeclarationSyntax>()
                )
                {
                    var interfaceName = interfaceDeclaration.Identifier.Text;

                    if (interfaceDeclaration.BaseList != null)
                    {
                        foreach (var baseType in interfaceDeclaration.BaseList.Types)
                        {
                            var parentName = baseType.Type.GetText().ToString().Trim();
                            if (!interfaceChildren.ContainsKey(parentName))
                            {
                                throw new System.InvalidOperationException(
                                    "Unexpected no interfaceChildren entry " +
                                    $"for {parentName}; expected all interfaceChildren " +
                                    "entries to be initialized for all interfaces before"
                                );
                            }

                            interfaceChildren[parentName].Add(interfaceName);
                        }
                    }
                }

                if (!interfaceChildren.ContainsKey("IReferable"))
                {
                    Console.Error.WriteLine(
                        $"No interface IReferable found in: {srcTypesPath}"
                    );
                    return false;
                }

                var referableInterfaces = new HashSet<string>();
                var queue = new Queue<string>();
                queue.Enqueue("IReferable");

                while (queue.Count > 0)
                {
                    var interfaceName = queue.Dequeue();
                    referableInterfaces.Add(interfaceName);

                    foreach (string child in interfaceChildren[interfaceName])
                    {
                        queue.Enqueue(child);
                    }
                }

                foreach (
                    var classDeclaration
                    in root.DescendantNodes().OfType<Syntax.ClassDeclarationSyntax>()
                )
                {
                    bool isReferable = false;
                    if (classDeclaration.BaseList != null)
                    {
                        foreach (var baseType in classDeclaration.BaseList.Types)
                        {
                            string baseTypeName =
                                baseType.Type.GetText().ToString().Trim();

                            if (referableInterfaces.Contains(baseTypeName))
                            {
                                isReferable = true;
                            }
                        }
                    }

                    if (!isReferable)
                    {
                        continue;
                    }

                    patches.Add(
                        new InsertSuffix(
                            classDeclaration.OpenBraceToken.Span.End,
                            snippet
                        )
                    );
                }
            }

            #endregion

            string patched = ApplyPatches(patches, text);

            try
            {
                File.WriteAllText(tgtTypesPath, patched);
            }
            catch (System.Exception exception)
            {
                Console.Error.WriteLine(
                    $"Failed to write to {tgtTypesPath}: {exception}");
                return false;
            }

            return true;
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class Arguments
        {
#pragma warning disable 8618
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            // ReSharper disable CollectionNeverUpdated.Global
            public string SourceProject { get; set; }
            public string TargetProject { get; set; }
            // ReSharper restore CollectionNeverUpdated.Global
            // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore 8618
        }

        private static string? FindCsprojFile(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (file.EndsWith(".csproj"))
                {
                    return file;
                }
            }

            return null;
        }

        private static int SemanticallyPatch(Arguments a)
        {
            if (!Directory.Exists(a.SourceProject))
            {
                Console.Error.WriteLine(
                    $"The --source does not exist or is not a directory: {a.SourceProject}"
                );
                return 1;
            }

            if (FindCsprojFile(a.SourceProject) == null)
            {
                Console.Error.WriteLine(
                    $"There is no *.csproj in --source: {a.SourceProject}"
                );
                return 1;
            }

            if (!Directory.Exists(a.TargetProject))
            {
                if (File.Exists(a.TargetProject))
                {
                    Console.Error.WriteLine(
                        $"The --target exists, but it is a file, while we expected a directory: {a.TargetProject}"
                    );
                    return 1;
                }

                Directory.CreateDirectory(a.TargetProject);
            }

            string srcTypesPath = Path.Join(a.SourceProject, "types.cs");
            if (!File.Exists(srcTypesPath))
            {
                Console.Error.WriteLine(
                    $"The types.cs could not be found in --source: {a.SourceProject}"
                );
                return 1;
            }

            string tgtTypesPath = Path.Join(a.TargetProject, "types.cs");

            bool success = PatchTypes(srcTypesPath, tgtTypesPath);

            var filenames = new List<string>()
            {
                "constants.cs",
                "copying.cs",
                "jsonization.cs",
                "reporting.cs",
                "stringification.cs",
                "verification.cs",
                "visitation.cs",
                "xmlization.cs"
            };
            foreach (var filename in filenames)
            {
                string src = Path.Join(a.SourceProject, filename);
                string tgt = Path.Join(a.TargetProject, filename);

                try
                {
                    File.Copy(src, tgt, true);
                }
                catch (System.Exception exception)
                {
                    Console.Error.WriteLine(
                        $"Failed to copy {src} to {tgt}: {exception}"
                    );
                    success = false;
                }
            }

            if (!success)
            {
                return 1;
            }

            return 0;
        }

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var sourceOption = new Option<string>(
                    new[] { "--source", "-s" },
                    "Path to the original project containing aas-core-csharp")
            { IsRequired = true };

            var targetOption = new Option<string>(
                    new[] { "--target", "-t" },
                    "Path to the project where the semantically patched code " +
                    "should live"
                )
            { IsRequired = true };

            var rootCommand = new RootCommand(
                "Semantically patch the aas-core-csharp library for aasx-server."
            ) { sourceOption, targetOption };


            rootCommand.SetHandler(
                (source, target) =>
                {
                    Environment.ExitCode = SemanticallyPatch(
                        new Arguments()
                        { SourceProject = source, TargetProject = target }
                    );
                }, sourceOption, targetOption);

            await rootCommand.InvokeAsync(args);
        }
    }
}