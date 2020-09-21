using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("desee.Tests")]

namespace desee.EntityObjects
{
    public class HashKeyImage {
        public HashImage Key { get; set; } = new HashImage();
        public HashImage Image { get; set; } = new HashImage();
    }
}