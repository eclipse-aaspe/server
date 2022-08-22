using System;
using System.Collections.Generic;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class OutputModifierContext
    {
        private string _level, _content, _extent;
        private bool _includeChildren = true;
        private List<string> idShortPaths;
        public string Level
        {
            get => _level;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _level = "deep";
                }
                else
                {
                    _level = value;
                }
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _content = "normal";
                }
                else
                {
                    _content = value;
                }
            }
        }

        public string Extent
        {
            get => _extent;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _extent = "withoutBLOBValue ";
                }
                else
                {
                    _extent = value;
                }
            }
        }

        public bool IncludeChildren
        {
            get => _includeChildren;
            internal set
            {
                _includeChildren = value;
            }
        }

        public List<string> IdShortPaths { get => idShortPaths; set => idShortPaths = value; }
        public string ParentPath { get; internal set; }

        public OutputModifierContext(string level, string content, string extent)
        {
            Level = level;
            Content = content;
            Extent = extent;

            if (Level.Equals("core", StringComparison.OrdinalIgnoreCase) && Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                IncludeChildren = false;
            }

            if (Content.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                idShortPaths = new List<string>();
            }
        }
    }
}
