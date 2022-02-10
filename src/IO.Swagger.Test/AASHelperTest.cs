using IO.Swagger.Helpers;
using NUnit.Framework;
using static AdminShellNS.AdminShellV20;

namespace IO.Swagger.Test
{
    public class AASHelperTest
    {



        private AASHelper? _helper;

        [SetUp]
        public void Setup()
        {
            _helper = new AASHelper();
        }

        [Test]
        public void TestFindByPath()
        {
            var sm = new Submodel();
            var l1 = new SubmodelElementCollection { idShort = "level1" };
            var l2 = new SubmodelElementCollection { idShort = "level2" };
            var l3 = new SubmodelElementCollection { idShort = "level3" };
            var l4 = new SubmodelElementCollection { idShort = "level4" };
            l3.Add(l4);
            l2.Add(l3);
            l1.Add(l2);
            sm.Add(l1);
            var found = _helper!.FindSubmodelElementByPath(sm, "level1", out _);
            Assert.AreEqual(found.idShort, "level1");
            found = _helper!.FindSubmodelElementByPath(sm, "level1.level2", out _);
            Assert.AreEqual(found.idShort, "level2");
            found = _helper!.FindSubmodelElementByPath(sm, "level1.level2.level3", out _);
            Assert.AreEqual(found.idShort, "level3");
            found = _helper!.FindSubmodelElementByPath(sm, "level1.level2.level3.level4", out _);
            Assert.AreEqual(found.idShort, "level4");
        }


        [Test]
        public void TestOutputModifier()
        {
            var sm = new Submodel();
            var l1 = new SubmodelElementCollection { idShort = "level1" };
            var blob = new Blob { idShort = "blob", value = "ABCD" };
            l1.Add(blob);
            sm.Add(l1);

            var jsonString = _helper!.HandleOutputModifiers(blob, "deep", "normal", "withoutBlobValue");
            Assert.IsFalse(jsonString.ToString().Contains("value"));
        }
    }
}