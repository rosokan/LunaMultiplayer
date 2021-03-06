﻿using LunaClient.ModuleStore.Structures;
using LunaClient.Utilities;
using LunaCommon.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LunaClient.ModuleStore
{
    /// <summary>
    /// This storage class stores all the fields that have the "ispersistent" as true. And also the customizations to them
    /// When we run trough all the part modules looking for changes we will get the fields to check from this class.
    /// Also we will use the customization values to decide if we apply the change or we send it accordingly
    /// </summary>
    public class FieldModuleStore
    {
        private static readonly FieldDefinition DefaultFieldDefinition = new FieldDefinition
        {
            FieldName = string.Empty,
            Ignore = false,
            IgnoreSpectating = false,
            Interval = 2500,
            SetValueInModule = false,
            CallLoad = true,
            CallOnAwake = true,
            CallOnLoad = true,
            CallOnStart = true
        };

        private static readonly string CustomPartSyncFolder = CommonUtil.CombinePaths(MainSystem.KspPath, "GameData", "LunaMultiplayer", "PartSync");

        /// <summary>
        /// Here we store all the part modules loaded and its fields that have the "ispersistent" as true.
        /// </summary>
        public static readonly Dictionary<string, FieldModuleDefinition> ModuleFieldsDictionary = new Dictionary<string, FieldModuleDefinition>();

        /// <summary>
        /// Here we store our customized part module fields
        /// </summary>
        public static Dictionary<string, ModuleDefinition> CustomizedModuleFieldsBehaviours = new Dictionary<string, ModuleDefinition>();

        /// <summary>
        /// Here we store the inheritance chain of the types up to PartModule
        /// For example. 
        /// ModuleEngineFX inherits from ModuleEngine and ModuleEngine inherits from PartModule
        /// So for the value of ModuleEngineFX we get an array containing ModuleEngineFX and ModuleEngine
        /// </summary>
        public static Dictionary<string, string[]> InheritanceTypeChain = new Dictionary<string, string[]>();

        /// <summary>
        /// Check all part modules that inherit from PartModule. Then it gets all the fields of those classes that have the "ispersistent" as true.
        /// </summary>
        public static void ReadLoadedPartModules()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var partModules = assembly.GetTypes().Where(myType => myType.IsClass && myType.IsSubclassOf(typeof(PartModule)));
                    foreach (var partModule in partModules)
                    {
                        var persistentFields = partModule.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                            .Where(f => f.GetCustomAttributes(typeof(KSPField), true).Any(attr => ((KSPField)attr).isPersistant)).ToArray();

                        if (persistentFields.Any())
                        {
                            ModuleFieldsDictionary.Add(partModule.Name, new FieldModuleDefinition(partModule, persistentFields));
                        }

                        InheritanceTypeChain.Add(partModule.Name, GetInheritChain(partModule));
                    }
                }
                catch (Exception ex)
                {
                    LunaLog.LogError($"Exception loading types from assembly {assembly.FullName}: {ex.Message}");
                }
            }

            LunaLog.Log($"Loaded {ModuleFieldsDictionary.Keys.Count} modules and a total of {ModuleFieldsDictionary.Values.Count} fields");
        }

        /// <summary>
        /// Reads the module customizations xml
        /// </summary>
        public static void ReadCustomizationXml()
        {
            var moduleValues = new List<ModuleDefinition>();

            foreach (var file in Directory.GetFiles(CustomPartSyncFolder, "*.xml"))
            {
                var moduleDefinition = LunaXmlSerializer.ReadXmlFromPath<ModuleDefinition>(file);
                moduleDefinition.ModuleName = Path.GetFileNameWithoutExtension(file);

                if (moduleDefinition.Fields.Select(m => m.FieldName).Distinct().Count() != moduleDefinition.Fields.Count)
                {
                    LunaLog.LogError($"Duplicate fields found in file: {file}. The module will be ignored");
                    continue;
                }

                moduleValues.Add(moduleDefinition);
            }

            CustomizedModuleFieldsBehaviours = moduleValues.ToDictionary(m => m.ModuleName, v => v);
        }
        
        /// <summary>
        /// Returns the customization for a field. if it doesn't exist, it returns a default value
        /// </summary>
        public static FieldDefinition GetCustomFieldDefinition(string moduleName, string fieldName)
        {
            return CustomizedModuleFieldsBehaviours.TryGetValue(moduleName, out var customization) ?
                customization.Fields.FirstOrDefault(f => f.FieldName == fieldName) ?? DefaultFieldDefinition
                : DefaultFieldDefinition;
        }

        /// <summary>
        /// Gets the inheritance chain of a type up to PartModule.
        /// For example. 
        /// ModuleEngineFX inherits from ModuleEngine and ModuleEngine inherits from PartModule
        /// So for the value of ModuleEngineFX we get an array containing ModuleEngineFX and ModuleEngine
        /// </summary>
        private static string[] GetInheritChain(Type partModuleType)
        {
            var list = new List<string>();
            if (partModuleType != null)
            {
                var current = partModuleType;
                while (current != typeof(PartModule) && current != null)
                {
                    list.Add(current.Name);
                    current = current.BaseType;
                }
            }

            return list.ToArray();
        }
    }
}
