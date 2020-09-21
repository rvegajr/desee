using System;
using System.Runtime.CompilerServices;
using desee.EntityObjects.Extentions;
[assembly: InternalsVisibleTo("desee.EntityObjects")]

namespace desee.EntityObjects
{
    public class HashImage {
        public string Json { get; set; } = "";
        public string Hash 
        {
            get { 
                return this.Json.AsSHA256Hash(); 
            }  
        }        
    }
}
