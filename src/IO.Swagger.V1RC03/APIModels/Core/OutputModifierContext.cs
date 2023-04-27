
using IO.Swagger.V1RC03.Services;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    public class OutputModifierContext
    {
        private string _level, _content, _extent;
        private DateTime _diff;
        private bool _includeChildren = true;
        private List<string> idShortPaths;
        public Submodel submodel = null;
        public AssetAdministrationShellEnvironmentService aasEnvService= null;
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
                    _extent = "withoutBLOBValue";
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

        public DateTime Diff
        {
            get => _diff;
        }

        public List<string> IdShortPaths { get => idShortPaths; set => idShortPaths = value; }
        public string ParentPath { get; internal set; }

        public OutputModifierContext(string level, string content, string extent, string diff = null)
        {
            Level = level;
            Content = content;
            Extent = extent;

            _diff = new DateTime();
            if (diff != null && diff != "")
            {
                try
                {
                    _diff = DateTime.Parse(diff).ToUniversalTime();
                }
                catch { }
            }

            //if (Level.Equals("core", StringComparison.OrdinalIgnoreCase) && Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            //{
            //    IncludeChildren = false;
            //}

            if (Content.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                idShortPaths = new List<string>();
            }
        }

        internal bool IsDefault()
        {
            return Level.Equals("deep", StringComparison.OrdinalIgnoreCase) &&
                Content.Equals("normal", StringComparison.OrdinalIgnoreCase) &&
                Extent.Equals("withoutBLOBValue", StringComparison.OrdinalIgnoreCase);
        }
    }
}
