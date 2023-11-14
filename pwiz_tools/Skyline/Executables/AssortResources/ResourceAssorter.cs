﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.Tools;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CSharp;

namespace AssortResources
{
    public class ResourceAssorter
    {
        private Dictionary<string, HashSet<string>> _referencedResourcesByFolder = new Dictionary<string, HashSet<string>>();

        public ResourceAssorter(string csProjPath, string resourceFilePath, params string[] otherProjectPaths)
        {
            CsProjFile = CsProjFile.FromProjFilePath(csProjPath);
            ResourceFile = ResourceFile.FromPath(resourceFilePath);
            OtherProjectFiles = new List<CsProjFile>();
            foreach (var otherProjectPath in otherProjectPaths)
            {
                OtherProjectFiles.Add(CsProjFile.FromProjFilePath(otherProjectPath));
            }
        }


        public CsProjFile CsProjFile { get; }
        public List<CsProjFile> OtherProjectFiles { get; }

        public ResourceFile ResourceFile { get; }

        public IEnumerable<string> GetReferencedResourcesInFolder(IEnumerable<string> files)
        {
            var result = new HashSet<string>();
            foreach (var file in files)
            {
                result.UnionWith(GetReferencedResourcesInFile(file));
            }

            return result;
        }

        public IEnumerable<string> GetReferencedResourcesInFile(string absolutePath)
        {
            if (File.Exists(absolutePath))
            {
                foreach (var reference in ResourceFile.ResourceIdentifiers.GetResourceReferences(File.ReadAllText(absolutePath)))
                {
                    yield return reference.ResourceIdentifier;
                }
            }
        }

        public void DoWork()
        {
            var folders = CsProjFile.ListAllSourceFiles()
                .ToLookup(path => Path.GetDirectoryName(path) ?? string.Empty);
            foreach (var folder in folders)
            {
                ProcessFolder(folder);
            }

            var otherReferences = new HashSet<string>();
            foreach (var otherProjectFile in OtherProjectFiles)
            {
                foreach (var sourceFile in otherProjectFile.ListAllSourceFiles())
                {
                    otherReferences.UnionWith(GetReferencedResourcesInFile(sourceFile));
                }
            }
            var uniqueReferences = new Dictionary<string, List<string>>();
            foreach (var resourceName in ResourceFile.ResourceIdentifiers.Resources.Keys)
            {
                if (otherReferences.Contains(resourceName))
                {
                    continue;
                }
                int count = 0;
                string foundFolderPath = null;
                foreach (var entry in _referencedResourcesByFolder)
                {
                    if (entry.Value.Contains(resourceName))
                    {
                        count++;
                        if (count == 1)
                        {
                            foundFolderPath = entry.Key;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (count == 1)
                {
                    if (!uniqueReferences.TryGetValue(foundFolderPath!, out var list))
                    {
                        list = new List<string>();
                        uniqueReferences.Add(foundFolderPath!, list);
                    }
                    list.Add(resourceName);
                }
            }

            foreach (var entry in uniqueReferences)
            {
                MakeResourceFile(entry.Key, folders[entry.Key], entry.Value.ToHashSet());
            }

            var stringsToDelete = uniqueReferences.SelectMany(entry => entry.Value).ToHashSet();
            RemoveResources(ResourceFile.FilePath, stringsToDelete);
            foreach (var language in ResourceFile.Languages.Values)
            {
                RemoveResources(language.FilePath, stringsToDelete);
            }
            var resxDocument = XDocument.Load(ResourceFile.FilePath);
            foreach (var el in resxDocument.Root!.Elements("data").ToList())
            {
                string? name = (string?)el.Attribute("name");
                if (name != null && stringsToDelete.Contains(name))
                {
                    el.Remove();
                }
            }
            WriteDocument(resxDocument, ResourceFile.FilePath);
            RunCustomTool(ResourceFile.FilePath);
            WriteDocument(CsProjFile.Document, CsProjFile.ProjFilePath);
        }

        public void RemoveResources(string filePath, HashSet<string> namesToDelete)
        {
            var resxDocument = XDocument.Load(filePath);
            foreach (var el in resxDocument.Root!.Elements("data").ToList())
            {
                string? name = (string?)el.Attribute("name");
                if (name != null && namesToDelete.Contains(name))
                {
                    el.Remove();
                }
            }
            WriteDocument(resxDocument, filePath);
        }

        public void ProcessFolder(IGrouping<string, string> folder)
        {
            var referencedResources = GetReferencedResourcesInFolder(folder).ToHashSet();
            if (referencedResources.Count > 0)
            {
                _referencedResourcesByFolder.Add(folder.Key, referencedResources);
            }
        }

        public void MakeResourceFile(string folderPath, IEnumerable<string> files, HashSet<string> resourceIdentifiers)
        {
            string folderName;
            if (string.IsNullOrEmpty(folderPath))
            {
                folderName = Path.GetFileNameWithoutExtension(CsProjFile.ProjectFolder);
            }
            else
            {
                folderName = Path.GetFileNameWithoutExtension(folderPath);
            }
            var newResourcesName = folderName + ResourceFile.ResourceIdentifiers.ResourceFileName;
            var resourceFilePath = Path.Combine(folderPath, newResourcesName + ".resx");
            AddResources(resourceFilePath, ResourceFile.ResourceIdentifiers, resourceIdentifiers);
            foreach (var languageEntry in ResourceFile.Languages)
            {
                var languageFilePath = Path.Combine(folderPath, newResourcesName + "." + languageEntry.Key + ".resx");
                AddResources(languageFilePath, languageEntry.Value, resourceIdentifiers);
            }
            RunCustomTool(resourceFilePath);
            CsProjFile.AddResourceFile(resourceFilePath, ResourceFile.Languages.Keys);
            foreach (var sourceFile in files)
            {
                if (File.Exists(sourceFile))
                {
                    var code = File.ReadAllText(sourceFile);
                    var newCode = ResourceFile.ResourceIdentifiers.ReplaceReferences(code, newResourcesName, resourceIdentifiers);
                    if (newCode != code)
                    {
                        File.WriteAllText(sourceFile, newCode, Encoding.UTF8);
                    }
                }
            }
            Console.Error.WriteLine("Moved {0} resources into {1}", resourceIdentifiers.Count, resourceFilePath);
        }

        public void AddResources(string resourceFilePath, ResourceIdentifiers resourceIdentifiers, IEnumerable<string> identifiers)
        {
            XDocument document;
            if (File.Exists(resourceFilePath))
            {
                document = XDocument.Load(resourceFilePath);
            }
            else
            {
                document = GetBlankResourceDocument();
            }

            var names = document.Root!.Elements("data").Select(e => (string?)e.Attribute("name")).OfType<string>()
                .ToHashSet();
            foreach (var resourceIdentifier in identifiers.OrderBy(x => x))
            {
                if (names.Add(resourceIdentifier))
                {
                    if (resourceIdentifiers.Resources.TryGetValue(resourceIdentifier, out var resource))
                    {
                        document.Root.Add(resource);
                    }
                }
            }
            WriteDocument(document, resourceFilePath);
        }

        public static XDocument GetBlankResourceDocument()
        {
            var type = typeof(ResourceAssorter);
            using (var stream = type.Assembly.GetManifestResourceStream(type, "BlankResourcesFile.txt"))
            {
                return XDocument.Load(stream!);
            }
        }

        public void WriteDocument(XDocument document, string path)
        {
            using (var xmlWriter = XmlWriter.Create(path, new XmlWriterSettings()
                   {
                       Indent = true,
                       Encoding = new UTF8Encoding(false, true)
                   }))
            {
                document.Save(xmlWriter);
            }
        }

        public void RunCustomTool(string filePath)
        {
            string baseName = Path.GetFileNameWithoutExtension(filePath);
            var namespaceName = CsProjFile.GetNamespace(filePath);
            Directory.SetCurrentDirectory(Path.GetDirectoryName(filePath));
            var csharpProvider = new CSharpCodeProvider();
            CodeCompileUnit codeCompileUnit = StronglyTypedResourceBuilder.Create(filePath, baseName, namespaceName, csharpProvider, false, out _);
            var designerCsFileName = Path.Combine(Path.GetDirectoryName(filePath), baseName + ".designer.cs");
            using (var writer = new StreamWriter(designerCsFileName))
            using (var indentedTextWriter = new IndentedTextWriter(writer, "    "))
            {
                csharpProvider.GenerateCodeFromCompileUnit(codeCompileUnit, indentedTextWriter, new CodeGeneratorOptions());
            }
        }
    }
}
