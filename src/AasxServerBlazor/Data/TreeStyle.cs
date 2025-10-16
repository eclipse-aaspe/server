/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/


namespace MW.Blazor
{
    public class TreeStyle
    {
        public static readonly TreeStyle Bootstrap = new()
        {
            // change to bootstrap icons
            // ExpandNodeIconClass = "far fa-plus-square cursor-pointer",
            // CollapseNodeIconClass = "far fa-minus-square cursor-pointer",
            // ExpandNodeIconClass = "bi bi-plus-square cursor-pointer",
            // CollapseNodeIconClass = "bi bi-dash-square cursor-pointer",
            ExpandNodeIconClass = "bi bi-plus-square fs-2 bold-icon",
            CollapseNodeIconClass = "bi bi-dash-square fs-2 bold-icon",
            NodeTitleClass = "p-1 cursor-pointer",
            NodeTitleSelectedClass = "bg-primary text-white"
        };

        public string ExpandNodeIconClass { get; set; }
        public string CollapseNodeIconClass { get; set; }
        public string NodeTitleClass { get; set; }
        public string NodeTitleSelectedClass { get; set; }
    }
}
