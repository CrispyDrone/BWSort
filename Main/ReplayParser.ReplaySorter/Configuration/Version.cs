using System;
using System.Linq;

namespace ReplayParser.ReplaySorter.Configuration
{
    public struct Version : IComparable<Version>
    {
        public static Version Parse(string version)
        {
            var versions = version.Split('.');
            var count = versions.Count();

            int major = 0;
            int minor = 0;
            int patch = 0;

            switch (count)
            {
                case 1:
                    major = int.Parse(versions[0]);
                    break;

                case 2:
                    minor = int.Parse(versions[1]);
                    goto case 1;

                case 3:
                    patch = int.Parse(versions[2]);
                    goto case 2;

                default:
                    throw new ArgumentOutOfRangeException("Invalid version number.");
            }

            return new Version(major, minor, patch);
        }

        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public int CompareTo(Version other)
        {
            return (Major * 100 + Minor * 10 + Patch) - (other.Major * 100 + other.Minor * 10 + other.Patch);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Version))
            {
                return false;
            }

            var version = (Version)obj;
            return Major == version.Major &&
                   Minor == version.Minor &&
                   Patch == version.Patch;
        }

        public override int GetHashCode()
        {
            var hashCode = -639545495;
            hashCode = hashCode * -1521134295 + Major.GetHashCode();
            hashCode = hashCode * -1521134295 + Minor.GetHashCode();
            hashCode = hashCode * -1521134295 + Patch.GetHashCode();
            return hashCode;
        }

        #region operator overloading

        public static bool operator <(Version a, Version b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(Version a, Version b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(Version a, Version b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator ==(Version a, Version b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(Version a, Version b)
        {
            return a.CompareTo(b) != 0;
        }

        #endregion
    }

}
