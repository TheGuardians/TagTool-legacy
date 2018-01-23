using System;
using System.Collections.Generic;
using BlamCore.Cache;
using BlamCore.Commands;
using TagTool.Common;

namespace TagTool.Tags
{
    class ListUnusedTagsCommand : Command
    {
        public GameCacheContext CacheContext { get; }

        public ListUnusedTagsCommand(GameCacheContext cacheContext)
            : base(CommandFlags.None,

                  "ListUnusedTags",
                  "Lists all unreferenced tags in the current tag cache",

                  "ListUnusedTags",

                  "Lists all unreferenced tags in the current tag cache")
        {
            CacheContext = cacheContext;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count != 0)
                return false;

            var depCounts = new Dictionary<int, int>();

            foreach (var tag in CacheContext.TagCache.Index)
            {
                if (tag == null)
                    continue;

                foreach (var dep in tag.Dependencies)
                {
                    var depTag = CacheContext.GetTag(dep);

                    if (depTag == null)
                        continue;

                    if (!depCounts.ContainsKey(dep))
                        depCounts[dep] = 0;

                    depCounts[dep]++;
                }
            }

            for (var i = 0; i < CacheContext.TagCache.Index.Count; i++)
            {
                var tag = CacheContext.GetTag(i);

                if (tag == null || tag.IsInGroup("cfgt") || tag.IsInGroup("scnr"))
                    continue;

                if (depCounts.ContainsKey(i))
                    continue;

                var tagName = CacheContext.TagNames.ContainsKey(tag.Index) ?
                    CacheContext.TagNames[tag.Index] :
                    "<unnamed>";

                Console.Write($"{tagName} ");
                TagPrinter.PrintTagShort(tag);
            }

            return true;
        }
    }
}