using CargoWiseEAdaptorInterface.Helpers;
using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface.Tests.Helpers
{
	[TestClass]
	public class ProcessLogHelpersTests
	{
		[TestMethod]
		[DataRow("TestFiles/example1.xml", "SOME.LOG.TEXT.")]
		public void Can_ExtractLog(string inputFile, string expectedLog)
		{
			// ARRANGE
			var fileText = XElement.Parse(File.ReadAllText(inputFile));

			// ACT
			var log = ProcessLogHelpers.TryParseProcessLog(fileText);

			// ASSERT
			Assert.AreEqual(expectedLog, log);
		}

		[TestMethod]
		[DataRow("TestFiles/example2.xml")]
		public void Cant_ExtractLogIfEmpty(string inputFile)
		{
			// ARRANGE
			var fileText = XElement.Parse(File.ReadAllText(inputFile));

			// ACT
			var log = ProcessLogHelpers.TryParseProcessLog(fileText);

			// ASSERT
			Assert.AreEqual("", log);
		}
	}
}
