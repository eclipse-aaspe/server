/*
 This class has beed added with respect to the specifications "Details of Asset Administration Shell Part 1 V3RC02" published on 30.05.2022
 
 */


//namespace AdminShellNS
using static AdminShell_V30.AdminShellV30;

namespace AdminShell_V30
{
    public class Resource
    {

        [MetaModelName("Resource.path")]
        [TextSearchable]
        [CountForHash]
        public string path = "";

        [MetaModelName("Resource.contentType")]
        [TextSearchable]
        [CountForHash]
        public string contentType = "";

        //Default Constructor
        public Resource()
        {
        }

        public Resource(Resource src)
        {
            if (src == null)
                return;

            path = src.path;
            contentType = src.contentType;
        }



        public Resource(string path, string contentType)
        {
            this.path = path;
            this.contentType = contentType;
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Resource)obj);
        }

        /// <summary>
        /// Returns true if Resource instances are equal
        /// </summary>
        /// <param name="other">Instance of Resource to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Resource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    path == other.path ||
                    path != null &&
                    path.Equals(other.path)
                ) &&
                (
                    contentType == other.contentType ||
                    contentType != null &&
                    contentType.Equals(other.contentType)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                if (path != null)
                    hashCode = hashCode * 59 + path.GetHashCode();
                if (contentType != null)
                    hashCode = hashCode * 59 + contentType.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

        public static bool operator ==(Resource left, Resource right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Resource left, Resource right)
        {
            return !Equals(left, right);
        }

        #endregion Operators
    }
}
