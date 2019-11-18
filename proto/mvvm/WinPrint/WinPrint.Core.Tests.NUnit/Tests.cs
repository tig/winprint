using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using WinPrint.Core.Services;

namespace WinPrint.Core.Tests.NUnit
{
    // TODO WTS: Add appropriate unit tests.
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestHeader()
        {
            Assert.Pass();
        }

        // TODO WTS: Remove or update this once your app is using real data and not the SampleDataService.
        // This test serves only as a demonstration of testing functionality in the Core library.
        [Test]
        public async Task EnsureSampleDataServiceReturnsMasterDetailDataAsync()
        {
            var actual = await SampleDataService.GetMasterDetailDataAsync();

            Assert.AreNotEqual(0, actual.Count());
        }
    }
}
