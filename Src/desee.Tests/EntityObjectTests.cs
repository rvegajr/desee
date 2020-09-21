using System;
using Xunit;
using desee.EntityObjects;
using desee.EntityObjects.Extentions;
using System.Diagnostics;

namespace desee.Tests
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class EntityObjectTests
    {
        [Fact]
        public void IsInTests()
        {
            Assert.True("KeyFieldName".IsIn("Key*"), "Key* should be in KeyFieldName");
            Assert.False("DataFieldName".IsIn("Key*"), "Key* should not be in DataFieldName");
            Assert.False("DataFieldName".IsIn("-Key*,-Data*"), "-Key*,-Data* on DataFieldName should return False");
            Assert.True("DataFieldName".IsIn("-Key*"), "-Key* on DataFieldName should return True");
            Assert.False("DataFieldName".IsIn("-Data*"), "-Data* on DataFieldName should return False");
        }

        [Fact]
        public void HashStringValueChangeTest()
        {
            var testObj = new MyTestClass();
            var hashKeyImage = testObj.AsHashKeyImage("Key*", "-Key*");
            var oldIntField = testObj.IntField;
            testObj.IntField = 9;
            var newHashKeyImage = testObj.AsHashKeyImage("Key*", "-Key*");
            Assert.False(newHashKeyImage.Image.Hash.Equals(hashKeyImage.Image.Hash), "Changed an item value, hash should be different");
            testObj.IntField = oldIntField;
            var newHashKeyImage2 = testObj.AsHashKeyImage("Key*", "-Key*");
            Assert.True(newHashKeyImage2.Image.Hash.Equals(hashKeyImage.Image.Hash), "Restored old value, hash should equal");
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }

    public class MyTestClass {
        public string StringField { get; set; } = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        public Int16 KeyInt16FieldA { get; set; }= 1;
        public float KeyFloatFieldB { get; set; } = float.Parse("1.5");
        public int IntField { get; set; }= int.MaxValue;
        public decimal DecimalField { get; set; }= Decimal.MaxValue;
        public DateTime DateTimeField { get; set; }= DateTime.Now;
    }
}
