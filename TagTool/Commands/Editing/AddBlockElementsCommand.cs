﻿using System;
using System.Collections;
using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Tags;

namespace TagTool.Commands.Editing
{
    class AddBlockElementsCommand : Command
    {
        private CommandContextStack ContextStack { get; }
        private HaloOnlineCacheContext CacheContext { get; }
        private CachedTagInstance Tag { get; }
        private TagStructureInfo Structure { get; set; }
        private object Owner { get; set; }

        public AddBlockElementsCommand(CommandContextStack contextStack, HaloOnlineCacheContext cacheContext, CachedTagInstance tag, TagStructureInfo structure, object owner)
            : base(true,

                  "AddBlockElements",
                  $"Adds/inserts block element(s) to a specific tag block in the current {structure.Types[0].Name} definition.",

                  "AddBlockElements <block name> [amount = 1] [index = *]",

                  $"Adds/inserts block element(s) to a specific tag block in the current {structure.Types[0].Name} definition.")
        {
            ContextStack = contextStack;
            CacheContext = cacheContext;
            Tag = tag;
            Structure = structure;
            Owner = owner;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1 || args.Count > 3)
                return false;

            var fieldName = args[0];
            var fieldNameLow = fieldName.ToLower();

            var previousContext = ContextStack.Context;
            var previousOwner = Owner;
            var previousStructure = Structure;

            if (fieldName.Contains("."))
            {
                var lastIndex = fieldName.LastIndexOf('.');
                var blockName = fieldName.Substring(0, lastIndex);
                fieldName = fieldName.Substring(lastIndex + 1, (fieldName.Length - lastIndex) - 1);
                fieldNameLow = fieldName.ToLower();

                var command = new EditBlockCommand(ContextStack, CacheContext, Tag, Owner);

                if (command.Execute(new List<string> { blockName }).Equals(false))
                {
                    while (ContextStack.Context != previousContext) ContextStack.Pop();
                    Owner = previousOwner;
                    Structure = previousStructure;
                    return false;
                }

                command = (ContextStack.Context.GetCommand("EditBlock") as EditBlockCommand);

                Owner = command.Owner;
                Structure = command.Structure;

                if (Owner == null)
                {
                    while (ContextStack.Context != previousContext) ContextStack.Pop();
                    Owner = previousOwner;
                    Structure = previousStructure;
                    return false;
                }
            }

            int count = 1;
            if ((args.Count > 1 && !int.TryParse(args[1], out count)) || count < 1)
            {
                Console.WriteLine($"Invalid amount specified: {args[1]}");
                return false;
            }

            var index = -1;

            if (args.Count > 2)
            {
                if (args[2] != "*" && (!int.TryParse(args[2], out index) || index < 0))
                {
                    Console.WriteLine($"Invalid index specified: {args[2]}");
                    return false;
                }
            }

			var field = TagStructure.GetTagFieldEnumerable(Structure)
				.Find(f => f.Name == fieldName || f.Name.ToLower() == fieldNameLow);

            var fieldType = field.FieldType;

            if ((field == null) ||
                (!fieldType.IsGenericType) ||
                (fieldType.GetInterface("IList") == null))
            {
                Console.WriteLine("ERROR: {0} does not contain a tag block named \"{1}\".", Structure.Types[0].Name, args[0]);
                while (ContextStack.Context != previousContext) ContextStack.Pop();
                Owner = previousOwner;
                Structure = previousStructure;
                return false;
            }

            var blockValue = field.GetValue(Owner) as IList;

            if (blockValue == null)
            {
                blockValue = Activator.CreateInstance(field.FieldType) as IList;
                field.SetValue(Owner, blockValue);
            }
            
            if (index > blockValue.Count)
            {
                Console.WriteLine($"Invalid index specified: {args[2]}");
                return false;
            }

            var elementType = field.FieldType.GenericTypeArguments[0];
            
            for (var i = 0; i < count; i++)
            {
                var element = CreateElement(elementType);

                if (index == -1)
                    blockValue.Add(element);
                else
                    blockValue.Insert(index + i, element);
            }

            field.SetValue(Owner, blockValue);

            var typeString =
                fieldType.IsGenericType ?
                    $"{fieldType.Name}<{fieldType.GenericTypeArguments[0].Name}>" :
                fieldType.Name;

            var itemString = count < 2 ? "element" : "elements";

            var valueString =
                ((IList)blockValue).Count != 0 ?
                    $"{{...}}[{((IList)blockValue).Count}]" :
                "null";

            Console.WriteLine($"Successfully added {count} {itemString} to {field.Name}: {typeString}");
            Console.WriteLine(valueString);

            while (ContextStack.Context != previousContext) ContextStack.Pop();
            Owner = previousOwner;
            Structure = previousStructure;

            return true;
        }

        private object CreateElement(Type elementType)
        {
            var instance = Activator.CreateInstance(elementType);

            if (!Attribute.IsDefined(elementType, typeof(TagStructureAttribute)) || !elementType.IsSubclassOf(typeof(TagStructure)))
                return instance;

            var element = (TagStructure)instance;

            foreach (var tagFieldInfo in element.GetTagFieldEnumerable(CacheContext.Version))
            {
                var fieldType = tagFieldInfo.FieldType;

                if (fieldType.IsArray && tagFieldInfo.Attribute.Length > 0)
                {
                    var array = (IList)Activator.CreateInstance(tagFieldInfo.FieldType,
                        new object[] { tagFieldInfo.Attribute.Length });

                    for (var i = 0; i < tagFieldInfo.Attribute.Length; i++)
                        array[i] = CreateElement(fieldType.GetElementType());
                }
                else
                {
                    try
                    {
                        tagFieldInfo.SetValue(element, CreateElement(tagFieldInfo.FieldType));
                    }
                    catch
                    {
                        try
                        {
                            tagFieldInfo.SetValue(element, Activator.CreateInstance(tagFieldInfo.FieldType));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return element;
        }
    }
}