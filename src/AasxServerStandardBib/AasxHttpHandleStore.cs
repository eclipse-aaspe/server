using System;
using System.Collections.Generic;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes a handle to a Identification or Reference to be used in HTTP REST APIs
    /// </summary>
    public abstract class AasxHttpHandle
    {
        [JsonPropertyName("key")] public string Key { get; set; }

        [JsonIgnore] public DateTime ExpiresInternal { get; set; }

        [JsonPropertyName("expires")] public string Expires { get; set; } // http-date, see https://stackoverflow.com/questions/21120882/the-date-time-format-used-in-http-headers
    }

    /// <summary>
    /// Describes a handle to a Identification to be used in HTTP REST APIs
    /// </summary>
    public class AasxHttpHandleIdentification : AasxHttpHandle
    {
        private static int counter = 1;

        public string identification = null;

        public AasxHttpHandleIdentification(string src, string keyPreset = null)
        {
            if (keyPreset == null)
                this.Key = $"@ID{counter++:00000000}";
            else
                this.Key = keyPreset;
            this.ExpiresInternal = DateTime.UtcNow.AddMinutes(60);
            this.Expires         = this.ExpiresInternal.ToString("R");
            //this.identification = new IIdentifiable(src);
            this.identification = src;
        }
    }

    /// <summary>
    /// This store stores AasxHttpHandle items in order to provide 'shortcuts' to AAS Identifications and References in HTTP REST APIs
    /// </summary>
    public class AasxHttpHandleStore
    {
        private Dictionary<string, AasxHttpHandle> storeItems = new Dictionary<string, AasxHttpHandle>();

        public void Add(AasxHttpHandle handle)
        {
            if (handle == null)
                return;
            storeItems.Add(handle.Key, handle);
        }

        public AasxHttpHandle Resolve(string Key)
        {
            if (storeItems.ContainsKey(Key))
                return storeItems[Key];
            return null;
        }

        public T ResolveSpecific<T>(string Key, List<T> specialHandles = null) where T : AasxHttpHandle
        {
            // trivial
            if (Key == null)
                return null;
            Key = Key.Trim();
            if (Key == "" || !Key.StartsWith("@"))
                return null;

            // search in specialHandles
            if (specialHandles != null)
                foreach (var sh in specialHandles)
                    if (sh.Key.Trim().ToLower() == Key.Trim().ToLower())
                        return sh;

            // search in store
            if (storeItems.ContainsKey(Key))
                return storeItems[Key] as T;
            return null;
        }

        public List<T> FindAll<T>() where T : class
        {
            var res = new List<T>();
            foreach (var x in storeItems.Values)
                if (x is T)
                    res.Add(x as T);
            return res;
        }
    }
}