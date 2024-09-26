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

using System.Collections.Generic;

namespace AasxUtils
{
    public abstract class MultiTupleBase
    { }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple2<T> : MultiTupleBase
    {
        public T one;
        public MultiTuple2(T one)
        {
            this.one = one;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple2<T, U> : MultiTupleBase
    {
        public T one;
        public U two;
        public MultiTuple2(T one, U two)
        {
            this.one = one;
            this.two = two;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTuple3<T, U, V> : MultiTupleBase
    {
        public T one;
        public U two;
        public V three;
        public MultiTuple3(T one, U two, V three)
        {
            this.one = one;
            this.two = two;
            this.three = three;
        }
    }

    [JetBrains.Annotations.UsedImplicitly]
    public class MultiTupleDictionary<KEY, MT>
    {
        private Dictionary<KEY, List<MT>> dict = new Dictionary<KEY, List<MT>>();

        public void Add(KEY key, MT mt)
        {
            if (dict.ContainsKey(key))
                dict[key].Add(mt);
            else
            {
                dict.Add(key, new List<MT>());
                dict[key].Add(mt);
            }
        }

        public bool ContainsKey(KEY key)
        {
            return dict.ContainsKey(key);
        }

        public List<MT> this[KEY key]
        {
            get
            {
                if (key == null || !dict.ContainsKey(key))
                    return null;
                return dict[key];
            }
        }
    }
}
